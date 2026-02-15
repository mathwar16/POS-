using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantBilling.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual ExpenseCategory? Category { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, UP, Bank Transfer, Petty Cash

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? VendorName { get; set; }

        public string? ReceiptImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
