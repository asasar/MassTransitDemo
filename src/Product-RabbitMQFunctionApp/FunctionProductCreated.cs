using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class FunctionProductCreated
{
    private readonly ILogger _logger;

    public FunctionProductCreated(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FunctionProductCreated>();
    }

    [Function(nameof(FunctionProductCreated))]
    public void Run([RabbitMQTrigger("product-created-queue", ConnectionStringSetting = "connection-product-created-queue")] string myQueueItem)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
    }
}
