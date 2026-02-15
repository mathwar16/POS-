namespace RestaurantBilling.DTOs
{
    public class DashboardDto
    {
        public decimal TotalRevenue { get; set; } // Net
        public decimal GrossRevenue { get; set; } // Subtotal
        public int TotalOrders { get; set; }
        public decimal AvgOrderValue { get; set; }
        public int TotalCustomers { get; set; } // Based on unique IDs if possible, or just total orders proxy
        public string PeakOrderTime { get; set; } = string.Empty;

        // Trends (percentage change)
        public double RevenueTrend { get; set; }
        public double OrdersTrend { get; set; }
        public double AovTrend { get; set; }

        public List<SummaryItemDto> PaymentMethods { get; set; } = new();
        public List<SummaryItemDto> PlatformBreakdown { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();

        // Chart Data
        public List<ChartDataPointDto> RevenueChart { get; set; } = new();
        public List<ChartDataPointDto> OrderVolumeChart { get; set; } = new();
        public List<SummaryItemDto> BestSellingProducts { get; set; } = new();
    }

    public class ChartDataPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int Count { get; set; }
    }

    public class SummaryItemDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Subtotal { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
