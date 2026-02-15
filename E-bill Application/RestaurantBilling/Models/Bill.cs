using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantBilling.Models
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Daily running token number, resets every day.
        /// </summary>
        public int TokenNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Gst { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharge { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty; // CASH, UPI, CARD

        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = "Direct"; // Direct, Zomato, Swiggy

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public virtual User? User { get; set; }

        // Navigation property
        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }
}
