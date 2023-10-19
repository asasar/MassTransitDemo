using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace ProcessMessageAfunc
{
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using MassTransit;
     using Microsoft.Azure.WebJobs;


    public class AuditOrderFunctions
    {
        const string AuditOrderEventHubName = "product-created-queue";
        readonly IEventReceiver _receiver;

        public AuditOrderFunctions(IEventReceiver receiver)
        {
            _receiver = receiver;
        }

        [FunctionName("AuditOrder")]
        public Task AuditOrderAsync([EventHubTrigger(AuditOrderEventHubName, Connection = "AzureWebJobsEventHub")]
            EventData message, CancellationToken cancellationToken)
        {
            return _receiver.HandleConsumer<ProductCreatedConsumer>(AuditOrderEventHubName, message, cancellationToken);
        }
    }
}
// https://github.com/MassTransit/MassTransit/blob/v6.3.2/src/Samples/Sample.AzureFunctions.ServiceBus/host.json