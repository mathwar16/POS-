using System.ComponentModel.DataAnnotations;

namespace RestaurantBilling.Models
{
    public class ReportSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string ReportType { get; set; } = string.Empty; // Daily, Weekly, Monthly

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public TimeSpan ScheduledTime { get; set; }

        public DayOfWeek? DayOfWeek { get; set; } // For Weekly

        public int? DayOfMonth { get; set; } // For Monthly

        public DateTime? LastRun { get; set; }
    }
}
