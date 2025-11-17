using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TinyTickets.Api.Data;
using TinyTickets.Api.Models;
using TinyTickets.Api.Services;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CORS
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

            // SQL Connection
            var sql = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlServer(sql));

            // Service Bus
            builder.Services.AddSingleton<ServiceBusClient>(_ =>
            {
                var cs = builder.Configuration["ServiceBus:ConnectionString"]
                         ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString")
                         ?? throw new Exception("Service Bus connection string missing");

                return new ServiceBusClient(cs);
            });

            builder.Services.AddSingleton<ServiceBusSender>(provider =>
            {
                var client = provider.GetRequiredService<ServiceBusClient>();
                return client.CreateSender("ticket-events");
            });

            // Blob SAS Services
            builder.Services.AddSingleton<SasTokenService>();
            builder.Services.AddSingleton<SasReadService>();

            var app = builder.Build();

            // DB Migration
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            app.UseCors("AllowTinyTicketsUi");

            // --------------------- TICKETS ---------------------

            app.MapGet("/tickets", async (AppDbContext db) =>
                await db.Tickets.ToListAsync());

            app.MapPost("/tickets", async (AppDbContext db, Ticket body, ServiceBusSender sender) =>
            {
                db.Tickets.Add(body);
                await db.SaveChangesAsync();

                await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(body))
                {
                    ContentType = "application/json"
                });

                return Results.Ok(body);
            });

            // --------------------- STORAGE ---------------------

            // SAS Upload URL API
            app.MapPost("/storage/sas-upload", (SasRequest req, SasTokenService sas) =>
            {
                string sasUrl = sas.GenerateUploadSas(req.Container, req.FileName);
                return Results.Ok(new { uploadUrl = sasUrl });
            });

            // SAS Read URL API
            app.MapPost("/storage/sas-read", (StorageSasReadRequest req, IConfiguration config) =>
            {
                string conn = config.GetConnectionString("StorageAccount")
                             ?? Environment.GetEnvironmentVariable("StorageAccount__ConnectionString");

                var blobService = new BlobServiceClient(conn);
                var container = blobService.GetBlobContainerClient(req.Container);
                var blob = container.GetBlobClient(req.FileName);

                var sas = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(10));
                return Results.Ok(new { url = sas.ToString() });
            });

            // Save Metadata in SQL
            app.MapPost("/storage/save-metadata", async (UploadedFile file, AppDbContext db) =>
            {
                db.UploadedFiles.Add(file);
                await db.SaveChangesAsync();

                return Results.Ok(file);
            });

            // List Files
            app.MapGet("/storage/files", async (AppDbContext db, SasReadService sas) =>
            {
                var items = await db.Files
                    .OrderByDescending(x => x.UploadedOn)
                    .ToListAsync();

                var results = items.Select(x => new
                {
                    x.Id,
                    x.FileName,
                    x.BlobName,
                    x.ContentType,
                    x.UploadedOn,
                    Url = sas.GetReadUrl("ticket-images", x.BlobName) // FIXED
                });

                return Results.Ok(results);
            });

            app.Run();
        }
    }
}
