using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace API
{
    public static class AddCustomOpenTelemetry
    {

        public static void AddOpenTelemetryOptionA(this WebApplicationBuilder appBuilder)
        {
            // Note: Switch between Zipkin/OTLP/Console by setting UseTracingExporter in appsettings.json.
            var tracingExporter = appBuilder.Configuration.GetValue("UseTracingExporter", defaultValue: "console")!.ToLowerInvariant();

            // Note: Switch between Prometheus/OTLP/Console by setting UseMetricsExporter in appsettings.json.
            var metricsExporter = appBuilder.Configuration.GetValue("UseMetricsExporter", defaultValue: "console")!.ToLowerInvariant();

            // Note: Switch between Console/OTLP by setting UseLogExporter in appsettings.json.
            var logExporter = appBuilder.Configuration.GetValue("UseLogExporter", defaultValue: "console")!.ToLowerInvariant();

            // Note: Switch between Explicit/Exponential by setting HistogramAggregation in appsettings.json
            var histogramAggregation = appBuilder.Configuration.GetValue("HistogramAggregation", defaultValue: "explicit")!.ToLowerInvariant();

            // Build a resource configuration action to set service information.
            Action<ResourceBuilder> configureResource = r => r.AddService(
                serviceName: appBuilder.Configuration.GetValue("ServiceName", defaultValue: "otel-test")!,
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: Environment.MachineName);

            // Create a service to expose ActivitySource, and Metric Instruments
            // for manual instrumentation
            appBuilder.Services.AddSingleton<Instrumentation>();

            // Configure OpenTelemetry tracing & metrics with auto-start using the
            // AddOpenTelemetry extension from OpenTelemetry.Extensions.Hosting.
            appBuilder.Services.AddOpenTelemetry()
                .ConfigureResource(configureResource)
                .UseAzureMonitor()
                .WithTracing(builder =>
                {
                    // Tracing

                    // Ensure the TracerProvider subscribes to any custom ActivitySources.
                    builder
                        .AddSource(Instrumentation.ActivitySourceName)
                        .SetSampler(new AlwaysOnSampler())
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation();

                    // Use IConfiguration binding for AspNetCore instrumentation options.
                    appBuilder.Services.Configure<AspNetCoreInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));

                    switch (tracingExporter)
                    {
                        case "zipkin":
                            builder.AddZipkinExporter();

                            builder.ConfigureServices(services =>
                            {
                                // Use IConfiguration binding for Zipkin exporter options.
                                services.Configure<ZipkinExporterOptions>(appBuilder.Configuration.GetSection("Zipkin"));
                            });
                            break;

                        case "otlp":
                            builder.AddOtlpExporter(otlpOptions =>
                            {
                                // Use IConfiguration directly for Otlp exporter endpoint option.
                                otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
                            });
                            break;

                        default:
                            builder.AddConsoleExporter();
                            break;
                    }
                })
                .WithMetrics(builder =>
                {
                    // Metrics

                    // Ensure the MeterProvider subscribes to any custom Meters.
                    builder
                        .AddMeter(Instrumentation.MeterName)
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation();

                    switch (histogramAggregation)
                    {
                        case "exponential":
                            builder.AddView(instrument =>
                            {
                                return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                                    ? new Base2ExponentialBucketHistogramConfiguration()
                                    : null;
                            });
                            break;
                        default:
                            // Explicit bounds histogram is the default.
                            // No additional configuration necessary.
                            break;
                    }

                    switch (metricsExporter)
                    {
                        case "prometheus":
                            builder.AddPrometheusExporter();
                            break;
                        case "otlp":
                            builder.AddOtlpExporter(otlpOptions =>
                            {
                                // Use IConfiguration directly for Otlp exporter endpoint option.
                                otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
                            });
                            break;
                        default:
                            builder.AddConsoleExporter();
                            break;
                    }
                });

            // Clear default logging providers used by WebApplication host.
            appBuilder.Logging.ClearProviders();

            // Configure OpenTelemetry Logging.
            appBuilder.Logging.AddOpenTelemetry(options =>
            {
                // Note: See appsettings.json Logging:OpenTelemetry section for configuration.

                var resourceBuilder = ResourceBuilder.CreateDefault();
                configureResource(resourceBuilder);
                options.SetResourceBuilder(resourceBuilder);

                switch (logExporter)
                {
                    case "otlp":
                        options.AddOtlpExporter(otlpOptions =>
                        {
                            // Use IConfiguration directly for Otlp exporter endpoint option.
                            otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
                        });
                        break;
                    default:
                        options.AddConsoleExporter();
                        break;
                }
            });
        }

        public static void AddOpenTelemetryOptionB(this WebApplicationBuilder appBuilder)
        {
            // OpenTelemetry
            var serviceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().ToString();
            var serviceVersion = "1.0.0";
            var connectionString = appBuilder.Configuration["ApplicationInsights:ConnectionString"];

            appBuilder.Services.AddOpenTelemetry()
                .WithTracing(config =>
                {
                    config.AddSource(serviceName);
                    config.SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                        );
                       config.AddAspNetCoreInstrumentation();
                       config.AddHttpClientInstrumentation();
                       config.AddAzureMonitorTraceExporter(o =>
                       {
                           o.ConnectionString = connectionString;
                       });
                   });
        }
    }
}
