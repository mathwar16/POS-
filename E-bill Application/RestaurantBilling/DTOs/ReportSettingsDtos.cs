using System.ComponentModel.DataAnnotations;

namespace RestaurantBilling.DTOs
{
    public class ReportScheduleDto
    {
        public int Id { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string ScheduledTime { get; set; } = string.Empty;
        public int? DayOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
        public DateTime? LastRun { get; set; }
    }

    public class UpdateReportScheduleDto
    {
        [Required]
        public bool IsActive { get; set; }

        [Required]
        public string ScheduledTime { get; set; } = string.Empty; // HH:mm

        public int? DayOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
    }

    public class EmailSettingsDto
    {
        [Required]
        public string Emails { get; set; } = string.Empty;
    }

    public class GlobalSettingsDto
    {
        public bool GstEnabled { get; set; }
        public decimal GstPercentage { get; set; }
        public bool ServiceChargeEnabled { get; set; }
        public decimal ServiceChargePercentage { get; set; }

        // Restaurant header configuration
        public string RestaurantName { get; set; } = string.Empty;
        public string RestaurantAddress { get; set; } = string.Empty;
        public string RestaurantPhone { get; set; } = string.Empty;
    }
}
