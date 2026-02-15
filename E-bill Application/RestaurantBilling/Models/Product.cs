using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantBilling.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }


        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Veg, Non-Veg, Beverage, Dessert

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
        public bool IsFavorite { get; set; } = false;


        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }
}
