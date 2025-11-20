using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TinyTickets.Api.Data;
using TinyTickets.Api.Services;
using TinyTickets.Api.Models;
using Azure.Identity;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------
// QuestPDF
// --------------------------------------------------------
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

// --------------------------------------------------------
// Load Key Vault
// --------------------------------------------------------
var vaultUrl = builder.Configuration["KeyVaultUrl"];
if (!string.IsNullOrEmpty(vaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUrl),
        new DefaultAzureCredential());
}

// --------------------------------------------------------
// Load Secrets
// --------------------------------------------------------
var sqlConn = builder.Configuration["SqlConnectionString"];
var sbConn = builder.Configuration["ServiceBusConnectionString"];
var blobConn = builder.Configuration["StorageAccountConnectionString"];

var tenantId = builder.Configuration["AzureAd:TenantId"];
var instance = builder.Configuration["AzureAd:Instance"];
var audience = builder.Configuration["AzureAd:Audience"];
// Should be: api://f2cea967-6192-44ae-aedc-1e6b6a994e5e

// --------------------------------------------------------
// DB
// --------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(sqlConn));

// --------------------------------------------------------
// Service Bus
// --------------------------------------------------------
builder.Services.AddSingleton(new ServiceBusClient(sbConn));
builder.Services.AddSingleton(p =>
    p.GetRequiredService<ServiceBusClient>().CreateSender("ticket-events"));

// --------------------------------------------------------
// Blob
// --------------------------------------------------------
builder.Services.AddSingleton<SasTokenService>();
builder.Services.AddSingleton<SasReadService>();

// --------------------------------------------------------
// CORS
// --------------------------------------------------------
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowTinyTicketsUi", p =>
        p.WithOrigins("https://salmon-rock-08a479500.3.azurestaticapps.net")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials());
});

// --------------------------------------------------------
// Azure AD Auth
// --------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = $"{instance}{tenantId}/v2.0";

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[]
            {
                $"{instance}{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/"
            },

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

// --------------------------------------------------------
// Authorization
// --------------------------------------------------------
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("ApiScope", p =>
    {
        p.RequireAuthenticatedUser();
        p.RequireClaim("scp", "access_as_user");
    });
});

var app = builder.Build();

// --------------------------------------------------------
// Auto migrations
// --------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>()
        .Database.Migrate();
}

// --------------------------------------------------------
// Pipeline
// --------------------------------------------------------
app.UseHttpsRedirection();
app.UseCors("AllowTinyTicketsUi");
app.UseAuthentication();
app.UseAuthorization();

// --------------------------------------------------------
// Endpoints
// --------------------------------------------------------

// List tickets
app.MapGet("/tickets", async (AppDbContext db)
    => await db.Tickets.ToListAsync()).RequireAuthorization("ApiScope");

// Create ticket
app.MapPost("/tickets", async (AppDbContext db, Ticket body, ServiceBusSender sb) =>
{
    body.Title = body.Title.Trim();
    db.Tickets.Add(body);
    await db.SaveChangesAsync();

    await sb.SendMessageAsync(new ServiceBusMessage(
        System.Text.Json.JsonSerializer.Serialize(body)));

    return Results.Ok(body);
}).RequireAuthorization("ApiScope");

// Publish
app.MapPost("/tickets/{id}/publish", async (int id, AppDbContext db, ServiceBusSender sb) =>
{
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket == null) return Results.NotFound();

    await sb.SendMessageAsync(new ServiceBusMessage(
        System.Text.Json.JsonSerializer.Serialize(ticket)));

    return Results.Ok(new { published = true });
}).RequireAuthorization("ApiScope");

// SAS Upload
app.MapPost("/storage/sas-upload", (SasRequest req, SasTokenService sas) =>
    Results.Ok(new { uploadUrl = sas.GenerateUploadSas(req.Container, req.FileName) })
).RequireAuthorization("ApiScope");

// Save metadata
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
    var items = await db.UploadedFiles
        .OrderByDescending(f => f.UploadedOn)
        .ToListAsync();

    return Results.Ok(items.Select(f => new
    {
        f.Id,
        f.FileName,
        f.BlobName,
        f.ContentType,
        f.Size,
        f.UploadedOn,
        url = sas.GetReadUrl(f.Container, f.BlobName)
    }));
}).RequireAuthorization("ApiScope");

// Delete
app.MapDelete("/storage/files/{id}", async (int id, AppDbContext db) =>
{
    var entry = await db.UploadedFiles.FindAsync(id);
    if (entry == null) return Results.NotFound();

    var blob = new BlobServiceClient(blobConn);
    await blob.GetBlobContainerClient(entry.Container)
        .DeleteBlobIfExistsAsync(entry.BlobName);

    db.UploadedFiles.Remove(entry);
    await db.SaveChangesAsync();

    return Results.Ok(new { deleted = true });
}).RequireAuthorization("ApiScope");

app.Use(async (ctx, next) =>
{
    Console.WriteLine("AUTHENTICATED: " + ctx.User.Identity?.IsAuthenticated);
    foreach (var c in ctx.User.Claims)
        Console.WriteLine($"{c.Type} = {c.Value}");

    await next();
});
app.Run();
