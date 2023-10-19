using MessageBus.Common;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(ProcessMessageAfunc.Startup))]


namespace ProcessMessageAfunc
{
    using AutoMapper;
    using Domain.Entitites.Products;
    using MassTransit;
    using MessageBus.Messages.Products;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Startup :
         FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }


        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            EventBusSettings? settings = configuration.GetSection(nameof(EventBusSettings)).Get<EventBusSettings>();

            builder.Services.AddMassTransit(config =>
            {
                config.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(settings.RabbitmqSettings.Host ?? throw new NullReferenceException("The host has not been specififed for RabbitMQ"), x =>
                    {
                        x.Username(settings.RabbitmqSettings.Username ?? throw new NullReferenceException("The username has not been specififed for RabbitMQ"));
                        x.Password(settings.RabbitmqSettings.Password ?? throw new NullReferenceException("The password has not been specififed for RabbitMQ"));
                    });

                    // Set up receiver endpoint for the ProductCreated event
                    // using the contancts from the messagebus library
                    cfg.ReceiveEndpoint(EventBusConstants.ProductCreatedQueue, c =>
                    {
                        c.ConfigureConsumer<ProductCreatedConsumer>(ctx);
                    });
                });
            });
        }
    }


    /// <summary>
    /// Settings file for the EventBus
    /// </summary>
    internal class EventBusSettings
    {
        public string EventbusProvider { get; set; } = "rabbitmq";
        public AzureServiceBusSettings? AzureServiceBusSettings { get; set; } = new AzureServiceBusSettings();
        public RabbitmqSettings? RabbitmqSettings { get; set; } = new RabbitmqSettings();
    }

    internal class AzureServiceBusSettings
    {
        /// <summary>
        /// Primary Connection String for Azure Service Bus.<br />
        /// This can be achieved from the service bus IAM in the Azure Portal.
        /// </summary>
        public string? ConnectionString { get; set; }
    }

    internal class RabbitmqSettings
    {
        /// <summary>
        /// Event Bus Host Address. By default set to "localhost".
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Event Bus Port. By default set to 5672.
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Event Bus Username. By default set to "guest".
        /// </summary>
        public string Username { get; set; } = "guest";

        /// <summary>
        /// Event Bus Password. By default set to "guest".
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Event Bus Virtual Host. By default set to "/".
        /// </summary>
        public string VirtualHost { get; set; } = "/";
    }
    internal class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
    {
        private readonly IMapper _mapper;
        private readonly ILogger<ProductCreatedConsumer> _logger;

        public ProductCreatedConsumer(IMapper mapper, ILogger<ProductCreatedConsumer> logger)
        {
            _mapper = mapper;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            // Map the Product Created Event to a Product by using data from the message
            Product product = _mapper.Map<Product>(context.Message);

            // Log the details of the product
            // You could do anything here with the consumed message such as saving it to a database,
            // calling the mediator to dispatch a command, etc.
            _logger.LogInformation($"Consumed Product Created Message. Details: Product {product.Name} has a cost of {product.Price} and we got {product.Stock} in stock.");

            // Return a completed task, job done!
            await Task.CompletedTask;
        }
    }
}