using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TinyTickets.Functions.Data;
using TinyTickets.Functions.Services;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();

        var settings = config.Build();
        var vaultUrl = settings["KeyVaultUrl"];

        if (!string.IsNullOrWhiteSpace(vaultUrl))
        {
            var credential = new DefaultAzureCredential();

            config.AddAzureKeyVault(
                new Uri(vaultUrl),
                credential,
                new Azure.Extensions.AspNetCore.Configuration.Secrets.AzureKeyVaultConfigurationOptions()
            );
        }

    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var cfg = context.Configuration;

        // EF Core for Audit Tables
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(cfg["SqlConnectionString"]));

        // Core Processing Services
        services.AddScoped<ITicketProcessor, TicketProcessor>();
        services.AddScoped<IBlobProcessor, BlobProcessor>();
    })
    .Build();

host.Run();
