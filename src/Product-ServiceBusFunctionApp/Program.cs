using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host
.CreateDefaultBuilder(args)
.ConfigureFunctionsWorkerDefaults(app =>
{
    app.AddOpenTelemetry();
})
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

    services.AddOpenTelemetry()
            .ConfigureResource(resBuilder => resBuilder.AddService("Isolated"))
            .WithTracing(tracerBuilder => tracerBuilder
                .AddSource(ActivityTrackingMiddleware.Source.Name)
                .SetSampler(new AlwaysOnSampler())
                .AddOtlpExporter(opts => { opts.Endpoint = new Uri("http://localhost:4317"); })
            );

});

await builder.Build().RunAsync();
