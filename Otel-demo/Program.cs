using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using Scalar.AspNetCore;
using System.Reflection.PortableExecutable;

var builder = WebApplication.CreateBuilder(args);

Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: builder.Environment.ApplicationName,
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
    serviceInstanceId: Environment.MachineName);

builder.Services.AddOpenApi();

//builder.Services.AddOpenTelemetry()
//    .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
//    .WithLogging(logging => logging
//        .AddOtlpExporter(options=> options.Endpoint = new Uri("http://localhost:4318")))
//    .WithMetrics()
//    .WithTracing();

// Configure OpenTelemetry Logging.
builder.Logging.AddOpenTelemetry(options =>
{
    var resourceBuilder = ResourceBuilder.CreateDefault();
    configureResource(resourceBuilder);
    options.SetResourceBuilder(resourceBuilder);

    options.AddOtlpExporter(otlpOptions =>
    {
        // Use IConfiguration directly for Otlp exporter endpoint option.
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!);
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/", () => $"Hello from OpenTelemetry Metrics With Traceid: " + Activity.Current?.Id);

app.MapGet("/log", (ILogger<Program> logger) =>
{
    logger.HelloWorld("Hello from OpenTelemetry Logs!");

    return "Hello from OpenTelemetry Logs!";
});

//app.Logger.StartingApp();

app.Run();

internal static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Starting the app...")]
    public static partial void StartingApp(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Log: `{message}`.")]
    public static partial void HelloWorld(this ILogger logger, string message);
}