using System.ComponentModel.DataAnnotations;

namespace RestaurantBilling.DTOs
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public bool IsFavorite { get; set; } = false;
    }
}
