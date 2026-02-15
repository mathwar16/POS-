using System.ComponentModel.DataAnnotations;

namespace RestaurantBilling.Models
{
    public class GlobalSetting
    {
        [Key]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
