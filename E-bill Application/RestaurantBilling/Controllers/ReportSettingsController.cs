using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Data;
using RestaurantBilling.DTOs;
using RestaurantBilling.Models;
using RestaurantBilling.Services;

namespace RestaurantBilling.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportSettingsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IReportService _reportService;

        public ReportSettingsController(ApplicationDbContext context, IEmailService emailService, IReportService reportService)
        {
            _context = context;
            _emailService = emailService;
            _reportService = reportService;
        }

        [HttpGet("schedules")]
        public async Task<ActionResult<IEnumerable<ReportScheduleDto>>> GetSchedules()
        {
            var existingTypes = await _context.ReportSchedules.Select(s => s.ReportType).ToListAsync();
            var requiredTypes = new List<string> { "Daily_Sales", "Daily_Expenses", "Weekly_Sales", "Weekly_Expenses", "Monthly_Sales", "Monthly_Expenses" };
            
            var missingTypes = requiredTypes.Except(existingTypes).ToList();
            if (missingTypes.Any())
            {
                foreach (var type in missingTypes)
                {
                    var schedule = new ReportSchedule
                    {
                        ReportType = type,
                        IsActive = type.StartsWith("Daily"), // Default active for daily
                        ScheduledTime = new TimeSpan(22, 0, 0),
                        DayOfWeek = type.StartsWith("Weekly") ? DayOfWeek.Sunday : null,
                        DayOfMonth = type.StartsWith("Monthly") ? 1 : null
                    };
                    _context.ReportSchedules.Add(schedule);
                }
                await _context.SaveChangesAsync();
            }

            var schedules = await _context.ReportSchedules
                .OrderBy(s => s.Id)
                .Select(s => new ReportScheduleDto
                {
                    Id = s.Id,
                    ReportType = s.ReportType,
                    IsActive = s.IsActive,
                    ScheduledTime = s.ScheduledTime.ToString(@"hh\:mm"),
                    DayOfWeek = (int?)s.DayOfWeek,
                    DayOfMonth = s.DayOfMonth,
                    LastRun = s.LastRun
                })
                .ToListAsync();

            return Ok(schedules);
        }

        [HttpPut("schedules/{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, UpdateReportScheduleDto dto)
        {
            var schedule = await _context.ReportSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            if (TimeSpan.TryParse(dto.ScheduledTime, out var time))
            {
                schedule.IsActive = dto.IsActive;
                schedule.ScheduledTime = time;
                schedule.DayOfWeek = dto.DayOfWeek.HasValue ? (DayOfWeek)dto.DayOfWeek.Value : null;
                schedule.DayOfMonth = dto.DayOfMonth;
                schedule.LastRun = null; // Reset last run if schedule changes to allow immediate re-run if needed

                await _context.SaveChangesAsync();
                return NoContent();
            }

            return BadRequest(new { error = "Invalid time format. Use HH:mm" });
        }

        [HttpGet("emails")]
        public async Task<ActionResult<EmailSettingsDto>> GetEmails()
        {
            var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "report_emails");
            return Ok(new EmailSettingsDto { Emails = setting?.Value ?? "" });
        }

        [HttpPut("emails")]
        public async Task<IActionResult> UpdateEmails(EmailSettingsDto dto)
        {
            var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "report_emails");
            if (setting == null)
            {
                setting = new GlobalSetting { Key = "report_emails" };
                _context.GlobalSettings.Add(setting);
            }

            setting.Value = dto.Emails;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("run/{type}")]
        public async Task<IActionResult> RunReport(string type)
        {
            var now = Helpers.DateTimeHelper.GetIndianTime();
            DateTime start, end;

            var parts = type.Split('_');
            var baseType = parts[0];
            var category = parts.Length > 1 ? parts[1] : "All";

            if (baseType.Equals("Daily", StringComparison.OrdinalIgnoreCase))
            {
                // Manual run for "Daily" usually means "the current day so far"
                start = now.Date;
                end = now.Date.AddDays(1);
            }
            else if (baseType.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
            {
                start = now.Date.AddDays(-(int)now.DayOfWeek);
                end = now.Date.AddDays(1);
            }
            else if (baseType.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
            {
                start = new DateTime(now.Year, now.Month, 1);
                end = now.Date.AddDays(1);
            }
            else
            {
                return BadRequest("Invalid report type");
            }

            var emailsSetting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "report_emails");
            var recipients = emailsSetting?.Value ?? "admin@restaurant.com";

            try
            {
                await _reportService.GenerateAndSendReportAsync(baseType, category, start, end, recipients);
                return Ok(new { message = $"{type} report triggered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to generate/send report: {ex.Message}" });
            }
        }

        [HttpGet("general")]
        public async Task<ActionResult<GlobalSettingsDto>> GetGeneralSettings()
        {
            var settings = await _context.GlobalSettings.ToListAsync();
            
            return Ok(new GlobalSettingsDto
            {
                GstEnabled = bool.Parse(settings.FirstOrDefault(s => s.Key == "gst_enabled")?.Value ?? "true"),
                GstPercentage = decimal.Parse(settings.FirstOrDefault(s => s.Key == "gst_percentage")?.Value ?? "5"),
                ServiceChargeEnabled = bool.Parse(settings.FirstOrDefault(s => s.Key == "service_charge_enabled")?.Value ?? "true"),
                ServiceChargePercentage = decimal.Parse(settings.FirstOrDefault(s => s.Key == "service_charge_percentage")?.Value ?? "5"),
                RestaurantName = settings.FirstOrDefault(s => s.Key == "restaurant_name")?.Value ?? "My Restaurant",
                RestaurantAddress = settings.FirstOrDefault(s => s.Key == "restaurant_address")?.Value ?? "123, Main Street",
                RestaurantPhone = settings.FirstOrDefault(s => s.Key == "restaurant_phone")?.Value ?? "+91-00000 00000"
            });
        }

        [HttpPut("general")]
        public async Task<IActionResult> UpdateGeneralSettings(GlobalSettingsDto dto)
        {
            await UpdateSetting("gst_enabled", dto.GstEnabled.ToString().ToLower());
            await UpdateSetting("gst_percentage", dto.GstPercentage.ToString());
            await UpdateSetting("service_charge_enabled", dto.ServiceChargeEnabled.ToString().ToLower());
            await UpdateSetting("service_charge_percentage", dto.ServiceChargePercentage.ToString());
            await UpdateSetting("restaurant_name", dto.RestaurantName ?? string.Empty);
            await UpdateSetting("restaurant_address", dto.RestaurantAddress ?? string.Empty);
            await UpdateSetting("restaurant_phone", dto.RestaurantPhone ?? string.Empty);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task UpdateSetting(string key, string value)
        {
            var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new GlobalSetting { Key = key };
                _context.GlobalSettings.Add(setting);
            }
            setting.Value = value;
        }
    }
}
