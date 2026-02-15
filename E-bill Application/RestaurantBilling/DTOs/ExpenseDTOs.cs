using System.ComponentModel.DataAnnotations;

namespace RestaurantBilling.DTOs
{
    public class ExpenseCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateExpenseCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class ExpenseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? VendorName { get; set; }
        public string? ReceiptImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExpenseDto
    {
        public DateTime? Date { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? VendorName { get; set; }

        public string? ReceiptImagePath { get; set; }
    }

    public class ExpenseSummaryDto
    {
        public decimal TodayFiltered { get; set; } // Based on current filters
        public decimal MonthFiltered { get; set; }
        public decimal TotalFiltered { get; set; }
        
        // Overall stats (regardless of filters, useful for dashboard cards)
        public decimal TodayTotal { get; set; }
        public decimal MonthTotal { get; set; }
        
        public List<CategoryExpenseSummaryDto> TopCategories { get; set; } = new();
    }

    public class CategoryExpenseSummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
