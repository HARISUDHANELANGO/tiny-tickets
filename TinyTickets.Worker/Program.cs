using Azure.Messaging.ServiceBus;

var builder = Host.CreateApplicationBuilder(args);

// 1. Read connection string
var cs = builder.Configuration["ServiceBus:ConnectionString"]
         ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString");

// 2. Create ServiceBusClient
var client = new ServiceBusClient(cs);

// 3. Create Processor for the queue
builder.Services.AddSingleton((_) =>
{
    return client.CreateProcessor("ticket-events", new ServiceBusProcessorOptions
    {
        MaxConcurrentCalls = 1,
        AutoCompleteMessages = false
    });
});

// 4. Register our background worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
