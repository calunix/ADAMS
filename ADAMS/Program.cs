using ADAMS;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ADAMS";
});

LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddSingleton<AccountMaintenanceWorker>();
builder.Services.AddHostedService<WindowsBackgroundService>();

var host = builder.Build();
host.Run();
