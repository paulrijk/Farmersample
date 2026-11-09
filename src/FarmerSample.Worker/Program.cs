using FarmerSample.Core;
using FarmerSample.Worker;
using OpenTelemetry;
using OpenTelemetry.Resources;

// Content root = the build output, so settings are found whichever folder the command is started from.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });

builder.Services.AddFarmerSampleCore(builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured."));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<OutboxDispatcher>();
builder.Services.AddHostedService<OutboxProcessor>();

if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    builder.Services.AddOpenTelemetry().ConfigureResource(r => r.AddService("farmersample-worker")).UseOtlpExporter();
    builder.Logging.AddOpenTelemetry(l => l.IncludeFormattedMessage = true);
}

builder.Build().Run();
