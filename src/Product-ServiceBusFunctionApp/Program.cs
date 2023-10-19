using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host
.CreateDefaultBuilder(args)
.ConfigureFunctionsWorkerDefaults()
.ConfigureAppConfiguration((hostingContext, configBuilder) =>
{
    var env = hostingContext.HostingEnvironment;
    configBuilder
      .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true);
})
.ConfigureServices((appBuilder, services) =>
{
    var configuration = appBuilder.Configuration;
    var connectionString = configuration["ApplicationInsights:ConnectionString"];

    var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService(
                    serviceName: typeof(Program).Assembly.GetName().Name ?? "API",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                    serviceInstanceId: Environment.MachineName);

    services.AddOpenTelemetry()
        .WithTracing(config =>
        {
            config.SetResourceBuilder(openTelemetryResourceBuilder);
            config.AddAspNetCoreInstrumentation();
            config.AddHttpClientInstrumentation();
            config.AddConsoleExporter();
            config.AddAzureMonitorTraceExporter(o =>
            {
                o.ConnectionString = connectionString;
            });
        })
        .WithMetrics(config => {
            config.SetResourceBuilder(openTelemetryResourceBuilder);
            config.AddRuntimeInstrumentation();
            config.AddAspNetCoreInstrumentation();
            config.AddHttpClientInstrumentation();
            config.AddConsoleExporter();
            config.AddAzureMonitorMetricExporter(o =>
            {
                o.ConnectionString = connectionString;
            });
        })
        .UseAzureMonitor(options => {
            options.ConnectionString = connectionString;
        });
});

await builder.Build().RunAsync();
