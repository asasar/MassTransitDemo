using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
namespace ServiceBusTriggerFunctionApp;

public class FunctionProductCreated
{
    private readonly ILogger<FunctionProductCreated> _logger;

    public FunctionProductCreated(ILogger<FunctionProductCreated> logger)
    {
        _logger = logger;
    }

    [Function(nameof(FunctionProductCreated))]
    public void Run([ServiceBusTrigger("product-created-queue", "functionsubscription", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
    }
}