using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TinyTickets.Api.Data;
using TinyTickets.Api.Models;
using TinyTickets.Api.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

            var builder = WebApplication.CreateBuilder(args);

            // --------------------------------------------------------
            // Load Key Vault � MUST be before reading secrets
            // --------------------------------------------------------
            var kvUrl = builder.Configuration["KeyVaultUrl"];

            if (!string.IsNullOrWhiteSpace(kvUrl))
            {
                builder.Configuration.AddAzureKeyVault(
                    new Uri(kvUrl),
                    new DefaultAzureCredential()
                );
            }

            // --------------------------------------------------------
            // CORS
            // --------------------------------------------------------
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowTinyTicketsUi", policy =>
                {
                    policy
                        .WithOrigins("https://salmon-rock-08a479500.3.azurestaticapps.net")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // --------------------------------------------------------
            // Retrieve ALL secrets strictly from KEY VAULT
            // --------------------------------------------------------
            var sqlConn = builder.Configuration["SqlConnectionString"]
                ?? throw new Exception("SqlConnection missing in Key Vault");

            var serviceBusConn = builder.Configuration["ServiceBusConnectionString"]
                ?? throw new Exception("ServiceBusConnection missing in Key Vault");

            var blobConn = builder.Configuration["StorageAccountConnectionString"]
                ?? throw new Exception("BlobStorageConnection missing in Key Vault");

            // --------------------------------------------------------
            // SQL
            // --------------------------------------------------------
            builder.Services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlServer(sqlConn));

            // --------------------------------------------------------
            // Service Bus
            // --------------------------------------------------------
            builder.Services.AddSingleton<ServiceBusClient>(_ =>
                new ServiceBusClient(serviceBusConn)
            );

            builder.Services.AddSingleton<ServiceBusSender>(provider =>
                provider.GetRequiredService<ServiceBusClient>()
                        .CreateSender("ticket-events")
            );

            // --------------------------------------------------------
            // Blob SAS Services
            // --------------------------------------------------------
            builder.Services.AddSingleton<SasTokenService>();
            builder.Services.AddSingleton<SasReadService>();

            // --------------------------------------------------------
            // Azure AD Authentication & Authorization
            // --------------------------------------------------------

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            // Configure JwtBearerOptions AFTER handler registration
            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters.ValidAudience =
                    builder.Configuration["AzureAd:ClientId"];

                options.TokenValidationParameters.ValidIssuer =
                    $"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/v2.0";
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scp", "access_as_user");
                });
            });

            var app = builder.Build();

            // Auto migrations
            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<AppDbContext>()
                    .Database.Migrate();
            }

            app.UseCors("AllowTinyTicketsUi");
            // --------------------------------------------------------
            // Authentication & Authorization Middleware
            // --------------------------------------------------------
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            // --------------------------------------------------------
            // Tickets
            // --------------------------------------------------------
            app.MapGet("/tickets", async (AppDbContext db) =>
                await db.Tickets.ToListAsync()).RequireAuthorization("ApiScope");

            app.MapPost("/tickets", async (AppDbContext db, Ticket body, ServiceBusSender sender) =>
            {
                body.Title = body.Title.Trim();

                db.Tickets.Add(body);
                await db.SaveChangesAsync();

                await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(body))
                {
                    ContentType = "application/json"
                });

                return Results.Ok(body);
            }).RequireAuthorization("ApiScope");

            // --------------------------------------------------------
            // Publish Ticket Event
            // --------------------------------------------------------
            app.MapPost("/tickets/{id}/publish", async (int id, AppDbContext db, ServiceBusSender sender) =>
            {
                var ticket = await db.Tickets.FindAsync(id);
                if (ticket == null)
                    return Results.NotFound(new { message = "Ticket not found" });

                // send event
                var evt = new ServiceBusMessage(JsonSerializer.Serialize(ticket))
                {
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(evt);

                return Results.Ok(new { published = true, id });
            }).RequireAuthorization("ApiScope");

            // --------------------------------------------------------
            // Storage APIs
            // --------------------------------------------------------

            // SAS upload
            app.MapPost("/storage/sas-upload", (SasRequest req, SasTokenService sas) =>
                Results.Ok(new { uploadUrl = sas.GenerateUploadSas(req.Container, req.FileName) })
            ).RequireAuthorization("ApiScope");

            // Save Metadata
            app.MapPost("/storage/save-metadata", async (UploadedFile file, AppDbContext db) =>
            {
                file.UploadedOn = DateTime.UtcNow;
                db.UploadedFiles.Add(file);
                await db.SaveChangesAsync();
                return Results.Ok(file);
            }).RequireAuthorization("ApiScope");

            // List files
            app.MapGet("/storage/files", async (AppDbContext db, SasReadService sas) =>
            {
                var rows = await db.UploadedFiles
                    .OrderByDescending(f => f.UploadedOn)
                    .ToListAsync();

                return Results.Ok(
                    rows.Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.BlobName,
                        f.ContentType,
                        f.Size,
                        f.UploadedOn,
                        url = sas.GetReadUrl(f.Container, f.BlobName)
                    })
                );
            }).RequireAuthorization("ApiScope");

            // Delete file
            app.MapDelete("/storage/files/{id}", async (int id, AppDbContext db) =>
            {
                var entry = await db.UploadedFiles.FindAsync(id);
                if (entry == null)
                    return Results.NotFound();

                var blobService = new BlobServiceClient(blobConn);
                var container = blobService.GetBlobContainerClient(entry.Container);
                await container.DeleteBlobIfExistsAsync(entry.BlobName);

                db.UploadedFiles.Remove(entry);
                await db.SaveChangesAsync();

                return Results.Ok(new { deleted = true });
            }).RequireAuthorization("ApiScope");

            app.Run();
        }
    }
}
