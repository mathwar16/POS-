using System.ComponentModel.DataAnnotations;

namespace RestaurantBilling.DTOs
{
    public class CreateBillDto
    {
        [Required]
        public List<BillItemDto> Items { get; set; } = new();

        [Required]
        public decimal Subtotal { get; set; }

        [Required]
        public decimal Gst { get; set; }

        [Required]
        public decimal Service { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = "Direct";

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }

    public class BillItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
