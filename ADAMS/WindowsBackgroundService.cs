using Microsoft.Extensions.Configuration;

namespace ADAMS
{
    public sealed class WindowsBackgroundService (
        AccountMaintenanceWorker amw,
        ILogger<WindowsBackgroundService> logger,
        IConfiguration configuration) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                await Wait(stoppingToken);

                try
                {
                    amw.PerformMaintenance();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }
        }

        private async Task Wait(CancellationToken ct)
        {
            await Task.Delay(TimeUntilJob(), ct);
        }

        private TimeSpan TimeUntilJob()
        {
            string? unparsedStartTime = configuration.GetValue<string>("TimeToStart");
            const int DefaultStartHours = 0;
            const int DefaultStartMinutes = 0;
            TimeOnly startTime = new(DefaultStartHours, DefaultStartMinutes);
            DateTime currTime = DateTime.Now;
            DateTime scheduledTime;

            if (unparsedStartTime == null || unparsedStartTime.Length != 4)
            {
                logger.LogWarning($"Invalid TimeToStart in appsettings.json; defaulting to {startTime}");
            }
            else
            {
                startTime = ParseTimeToStart(unparsedStartTime);
                logger.LogInformation($"Configured time to start: {startTime}");
            }

            if (currTime.TimeOfDay < startTime.ToTimeSpan())
            {
                scheduledTime = DateTime.Today.Add(startTime.ToTimeSpan());
            }
            else
            {
                scheduledTime = DateTime.Today.AddDays(1.0).Add(startTime.ToTimeSpan());
            }

            logger.LogInformation($"Next scheduled job at: {scheduledTime}");
            return scheduledTime - currTime;

            static TimeOnly ParseTimeToStart(string unparsedTime)
            {
                int hours = int.Parse(unparsedTime.Substring(0, 2));
                int minutes = int.Parse(unparsedTime.Substring(2, 2));
                TimeOnly time = new(hours, minutes);
                return time;
            }
        }
    }
}
