using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Data;

namespace RestaurantBilling.Services
{
    public class ReportGenerationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportGenerationService> _logger;

        public ReportGenerationService(IServiceProvider serviceProvider, ILogger<ReportGenerationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dynamic Report Generation Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndRunReports();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during report generation check.");
                }

                // Check more frequently than 1 minute to ensure we don't miss the exact minute due to drift
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task CheckAndRunReports()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
            
            var now = Helpers.DateTimeHelper.GetIndianTime();
            var currentTime = new TimeSpan(now.Hour, now.Minute, 0);

            // Detailed logging for debugging
            _logger.LogInformation($"[BackgroundService] Heartbeat IST: {now:yyyy-MM-dd HH:mm:ss}. Checking for schedules at {currentTime:hh\\:mm}");

            var schedules = await context.ReportSchedules
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync();

            if (!schedules.Any())
            {
                _logger.LogDebug("No active report schedules found.");
            }

            var emailsSetting = await context.GlobalSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == "report_emails");
            var recipients = emailsSetting?.Value ?? "admin@restaurant.com";

            foreach (var schedule in schedules)
            {
                var targetTime = new TimeSpan(schedule.ScheduledTime.Hours, schedule.ScheduledTime.Minutes, 0);
                
                // _logger.LogInformation($"[BackgroundService] Checking Schedule {schedule.Id} ({schedule.ReportType}): Target {targetTime:hh\\:mm}, LastRun: {schedule.LastRun:yyyy-MM-dd HH:mm}");

                if (ShouldRun(schedule, now, currentTime))
                {
                    _logger.LogInformation($"[BackgroundService] TRIGGERING Schedule {schedule.Id}: {schedule.ReportType} for recipients: {recipients}");
                    
                    var parts = schedule.ReportType.Split('_');
                    var baseType = parts[0];
                    var category = parts.Length > 1 ? parts[1] : "All";

                    DateTime start, end;
                    GetDateRangeForType(baseType, now, out start, out end);

                    try 
                    {
                        await reportService.GenerateAndSendReportAsync(baseType, category, start, end, recipients);
                        
                        using var saveScope = _serviceProvider.CreateScope();
                        var saveContext = saveScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var dbSchedule = await saveContext.ReportSchedules.FindAsync(schedule.Id);
                        if (dbSchedule != null)
                        {
                            dbSchedule.LastRun = now;
                            await saveContext.SaveChangesAsync();
                            _logger.LogInformation($"[BackgroundService] Successfully updated LastRun for Schedule {schedule.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[BackgroundService] ERROR generating/sending scheduled report {schedule.ReportType}");
                    }
                }
            }
        }

        private bool ShouldRun(Models.ReportSchedule schedule, DateTime now, TimeSpan currentTime)
        {
            // Normalize times to ignore seconds
            var scheduled = new TimeSpan(schedule.ScheduledTime.Hours, schedule.ScheduledTime.Minutes, 0);
            
            if (scheduled != currentTime)
                return false;

            if (schedule.LastRun.HasValue)
            {
                // If it ran in the last 60 minutes, don't run again (prevents double triggers within the same minute)
                if ((now - schedule.LastRun.Value).TotalMinutes < 60)
                {
                    // _logger.LogDebug($"[BackgroundService] Schedule {schedule.Id} already ran recently ({schedule.LastRun})");
                    return false;
                }
            }

            var baseType = schedule.ReportType.Split('_')[0];

            if (baseType == "Daily") return true;

            if (baseType == "Weekly")
                return now.DayOfWeek == schedule.DayOfWeek;

            if (baseType == "Monthly")
                return now.Day == (schedule.DayOfMonth ?? 1);

            return false;
        }

        private void GetDateRangeForType(string type, DateTime now, out DateTime start, out DateTime end)
        {
            // Update: To match the user's expectation and the "Run Now" behavior, 
            // "Daily" will now cover "Today" (from 00:00 AM IST to now/end of day).
            if (type.StartsWith("Daily"))
            {
                start = now.Date;
                end = now.Date.AddDays(1);
            }
            else if (type.StartsWith("Weekly"))
            {
                // Current week starting from Sunday
                start = now.Date.AddDays(-(int)now.DayOfWeek);
                end = now.Date.AddDays(1);
            }
            else // Monthly
            {
                // Current month so far
                start = new DateTime(now.Year, now.Month, 1);
                end = now.Date.AddDays(1);
            }
            
            _logger.LogInformation($"[BackgroundService] Calculated Date Range for {type}: {start:yyyy-MM-dd HH:mm} to {end:yyyy-MM-dd HH:mm}");
        }
    }
}
