
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TinyTickets.Api.Data;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //builder.Services.AddCors(o =>
            //    o.AddPolicy("AllowClient", 
            //        p => p
            //        .AllowAnyOrigin()
            //        .AllowAnyHeader()
            //        .AllowAnyMethod()));

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

            // Connection string
            //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            //                      ?? "Server=(localdb)\\MSSQLLocalDB;Database=TinyTicketsDb;Trusted_Connection=True;";

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                    ?? "Server=(localdb)\\MSSQLLocalDB;Database=TinyTicketsDb;Trusted_Connection=True;";


            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddSingleton<ServiceBusClient>(provider =>
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


            var app = builder.Build();
            //app.UseCors();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            var tickets = new List<object>();
            var id = 1;

            //app.MapGet("/tickets", () => tickets);
            //app.MapPost("/tickets", (JsonElement body) =>
            //{
            //    var title = body.GetProperty("title").GetString() ?? "Untitled";
            //    var item = new { id = id++, title };
            //    tickets.Add(item);
            //    return Results.Ok(item);
            //});


            // Map endpoints
            app.UseCors("AllowTinyTicketsUi");
            app.MapGet("/tickets", async (AppDbContext db) =>
                await db.Tickets.ToListAsync());

            app.MapPost("/tickets", async (AppDbContext db, Ticket body, ServiceBusSender sender) =>
            {
                // 1. Save to SQL
                db.Tickets.Add(body);
                await db.SaveChangesAsync();

                // 2. Publish to Service Bus
                var payload = JsonSerializer.Serialize(body);
                var msg = new ServiceBusMessage(payload)
                {
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(msg);

                // 3. Return result
                return Results.Ok(body);
            });

            app.MapPost("/tickets/{id}/publish", async (int id, AppDbContext db, ServiceBusSender sender) =>
            {
                var ticket = await db.Tickets.FindAsync(id);
                if (ticket == null)
                    return Results.NotFound();

                var json = JsonSerializer.Serialize(ticket);
                var message = new ServiceBusMessage(json)
                {
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(message);

                return Results.Ok(new { status = "published" });
            });

            app.Run();

        }
    }
}
