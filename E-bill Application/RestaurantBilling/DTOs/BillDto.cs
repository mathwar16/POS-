namespace RestaurantBilling.DTOs
{
    public class BillDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TokenNumber { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Gst { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public DateTime CreatedAt { get; set; }
        public List<BillItemResponseDto> Items { get; set; } = new();
    }

    public class BillItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
