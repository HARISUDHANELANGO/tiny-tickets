using Azure.Messaging.ServiceBus;

public class Worker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;

    public Worker(ServiceBusProcessor processor)
    {
        _processor = processor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register message + error handlers
        _processor.ProcessMessageAsync += OnMessageReceived;
        _processor.ProcessErrorAsync += OnError;

        Console.WriteLine("Worker: Listening to ticket-events queue...");

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        Console.WriteLine($"[Worker] Message received: {body}");

        // Manual completion
        await args.CompleteMessageAsync(args.Message);
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"[Worker Error] {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
