using Azure.Monitor.OpenTelemetry.Exporter;
using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<KestrelServerOptions>(options =>
{
  options.Limits.MaxRequestBodySize = 60_000_000; // 60MB — above the app's own 50MB validation limit, so requests can actually reach ValidateFile
});

builder.Services.AddSingleton(sp =>
{
  var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
  var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<DocumentStorageService>();
  return new DocumentStorageService(connectionString, logger, "staff-docs");
});

builder.Services.AddSingleton(_ =>
{
  var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
  return new MenuService(connectionString);
});

builder.Services.AddSingleton(sp =>
{
  var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
  var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<OrderService>();
  return new OrderService(connectionString, logger);
});

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
  builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();
}

builder.Build().Run();
