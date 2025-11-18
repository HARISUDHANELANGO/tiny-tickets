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
            // SQL
            // --------------------------------------------------------
            var sql = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlServer(sql));

            // --------------------------------------------------------
            // Service Bus
            // --------------------------------------------------------
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

            // --------------------------------------------------------
            // SAS Services
            // --------------------------------------------------------
            builder.Services.AddSingleton<SasTokenService>();
            builder.Services.AddSingleton<SasReadService>();

            var app = builder.Build();

            // Auto-migrations
            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<AppDbContext>()
                    .Database.Migrate();
            }

            app.UseCors("AllowTinyTicketsUi");

            // --------------------------------------------------------
            // Ticket APIs
            // --------------------------------------------------------
            app.MapGet("/tickets", async (AppDbContext db) =>
                await db.Tickets.ToListAsync());

            app.MapPost("/tickets", async (AppDbContext db, Ticket body, ServiceBusSender sender) =>
            {
                body.Title = body.Title.Trim();
                db.Tickets.Add(body);
                await db.SaveChangesAsync();

                var msg = new ServiceBusMessage(JsonSerializer.Serialize(body))
                {
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(msg);
                return Results.Ok(body);
            });

            // --------------------------------------------------------
            // Storage APIs
            // --------------------------------------------------------

            // SAS Upload URL
            app.MapPost("/storage/sas-upload", (SasRequest req, SasTokenService sas) =>
            {
                return Results.Ok(new { uploadUrl = sas.GenerateUploadSas(req.Container, req.FileName) });
            });

            // Save Metadata
            app.MapPost("/storage/save-metadata", async (UploadedFile file, AppDbContext db) =>
            {
                file.UploadedOn = DateTime.UtcNow;
                db.UploadedFiles.Add(file);
                await db.SaveChangesAsync();
                return Results.Ok(file);
            });

            // List
            app.MapGet("/storage/files", async (AppDbContext db, SasReadService sas) =>
            {
                var rows = await db.UploadedFiles.OrderByDescending(f => f.UploadedOn).ToListAsync();

                var result = rows.Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.BlobName,
                    f.ContentType,
                    f.Size,
                    f.UploadedOn,
                    url = sas.GetReadUrl(f.Container, f.BlobName)
                });

                return Results.Ok(result);
            });

            // Delete
            app.MapDelete("/storage/files/{id}", async (int id, AppDbContext db, IConfiguration config) =>
            {
                var file = await db.UploadedFiles.FindAsync(id);
                if (file == null) return Results.NotFound();

                var blobService = new BlobServiceClient(
                    config.GetConnectionString("StorageAccount")
                    ?? Environment.GetEnvironmentVariable("StorageAccount__ConnectionString")
                );

                var blob = blobService.GetBlobContainerClient(file.Container).GetBlobClient(file.BlobName);
                await blob.DeleteIfExistsAsync();

                db.UploadedFiles.Remove(file);
                await db.SaveChangesAsync();

                return Results.Ok(new { deleted = true });
            });

            app.MapDelete("/storage/files/{id}", async (int id, AppDbContext db, IConfiguration config) =>
            {
                var entry = await db.UploadedFiles.FindAsync(id);
                if (entry == null)
                    return Results.NotFound(new { message = "File not found" });

                // Blob client
                var blobService = new BlobServiceClient(
                    config.GetConnectionString("StorageAccount")
                    ?? Environment.GetEnvironmentVariable("StorageAccount__ConnectionString")
                );

                var container = blobService.GetBlobContainerClient(entry.Container);
                var blob = container.GetBlobClient(entry.BlobName);

                // Delete from Blob
                await blob.DeleteIfExistsAsync();

                // Delete from SQL
                db.UploadedFiles.Remove(entry);
                await db.SaveChangesAsync();

                return Results.Ok(new { deleted = true, id = entry.Id });
            });


            app.Run();
        }
    }
}
