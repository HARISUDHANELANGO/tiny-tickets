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
// QuestPDF Setup
// --------------------------------------------------------
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

// --------------------------------------------------------
// Load Key Vault
// --------------------------------------------------------
var vaultUrl = builder.Configuration["KeyVaultUrl"];
if (!string.IsNullOrWhiteSpace(vaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUrl),
        new DefaultAzureCredential()
    );
}

// --------------------------------------------------------
// Load secrets
// --------------------------------------------------------
var sqlConn = builder.Configuration["SqlConnectionString"];
var sbConn = builder.Configuration["ServiceBusConnectionString"];
var blobConn = builder.Configuration["StorageAccountConnectionString"];
var tenantId = builder.Configuration["AzureAd:TenantId"];
var instance = builder.Configuration["AzureAd:Instance"];
var apiAudience = builder.Configuration["AzureAd:ApiAudience"];

// --------------------------------------------------------
// SQL
// --------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(sqlConn));

// --------------------------------------------------------
// Service Bus
// --------------------------------------------------------
builder.Services.AddSingleton(new ServiceBusClient(sbConn));
builder.Services.AddSingleton(provider =>
    provider.GetService<ServiceBusClient>()!.CreateSender("ticket-events"));

// --------------------------------------------------------
// Blob Services
// --------------------------------------------------------
builder.Services.AddSingleton<SasTokenService>();
builder.Services.AddSingleton<SasReadService>();

// --------------------------------------------------------
// CORS
// --------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTinyTicketsUi", policy =>
        policy.WithOrigins("https://salmon-rock-08a479500.3.azurestaticapps.net")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// --------------------------------------------------------
// Azure AD Authentication
// --------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{instance}{tenantId}/v2.0";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = apiAudience,   // Required
            ValidateIssuer = true,
            ValidIssuer = $"{instance}{tenantId}/v2.0",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.RequireHttpsMetadata = true;
    });

// --------------------------------------------------------
// Authorization
// --------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scp", "access_as_user");
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
// Middleware
// --------------------------------------------------------
app.UseHttpsRedirection();
app.UseCors("AllowTinyTicketsUi");
app.UseAuthentication();
app.UseAuthorization();

// --------------------------------------------------------
// Endpoints
// --------------------------------------------------------
app.MapGet("/tickets", async (AppDbContext db) =>
    await db.Tickets.ToListAsync()
).RequireAuthorization("ApiScope");

app.MapPost("/tickets", async (AppDbContext db, Ticket body, ServiceBusSender sender) =>
{
    body.Title = body.Title.Trim();

    db.Tickets.Add(body);
    await db.SaveChangesAsync();

    await sender.SendMessageAsync(new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(body))
    {
        ContentType = "application/json"
    });

    return Results.Ok(body);
}).RequireAuthorization("ApiScope");

app.MapPost("/tickets/{id}/publish", async (int id, AppDbContext db, ServiceBusSender sender) =>
{
    var ticket = await db.Tickets.FindAsync(id);
    if (ticket == null)
        return Results.NotFound();

    await sender.SendMessageAsync(new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(ticket)));
    return Results.Ok(new { published = true });
}).RequireAuthorization("ApiScope");

// Storage SAS
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
    var list = await db.UploadedFiles.OrderByDescending(f => f.UploadedOn).ToListAsync();

    return Results.Ok(list.Select(f => new
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

// Delete file
app.MapDelete("/storage/files/{id}", async (int id, AppDbContext db) =>
{
    var entry = await db.UploadedFiles.FindAsync(id);
    if (entry == null) return Results.NotFound();

    var bc = new BlobServiceClient(blobConn);
    await bc.GetBlobContainerClient(entry.Container).DeleteBlobIfExistsAsync(entry.BlobName);

    db.UploadedFiles.Remove(entry);
    await db.SaveChangesAsync();

    return Results.Ok(new { deleted = true });
}).RequireAuthorization("ApiScope");

app.Run();
