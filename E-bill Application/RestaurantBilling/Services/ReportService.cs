using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Data;
using System.Text;

namespace RestaurantBilling.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;
        private readonly IEmailService _emailService;
        private readonly string _reportPath;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
            if (!Directory.Exists(_reportPath))
            {
                Directory.CreateDirectory(_reportPath);
            }
        }

        public async Task GenerateAndSendReportAsync(string type, string category, DateTime start, DateTime end, string recipients)
        {
            // Ensure start/end (which are in IST) are converted to UTC for Postgres timestamptz comparisons
            var startUtc = Helpers.DateTimeHelper.ToUtcTime(start);
            var endUtc = Helpers.DateTimeHelper.ToUtcTime(end);

            _logger.LogInformation($"Generating {type} {category} report for IST Range: {start:yyyy-MM-dd HH:mm} to {end:yyyy-MM-dd HH:mm} (UTC: {startUtc:HH:mm} to {endUtc:HH:mm})");

            var bills = new List<Models.Bill>();
            var expenses = new List<Models.Expense>();

            if (category.Equals("Sales", StringComparison.OrdinalIgnoreCase) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                bills = await _context.Bills
                    .Include(b => b.BillItems)
                    .Where(b => b.CreatedAt >= startUtc && b.CreatedAt < endUtc)
                    .ToListAsync();
            }

            if (category.Equals("Expenses", StringComparison.OrdinalIgnoreCase) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.Date >= startUtc && e.Date < endUtc)
                    .ToListAsync();
            }

            if (!bills.Any() && !expenses.Any())
            {
                _logger.LogInformation($"No data found for {type} {category} report period.");
                return;
            }

            var csvContent = GenerateCsv(bills, expenses, category);
            var fileName = $"{type}_{category}_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
            var filePath = Path.Combine(_reportPath, fileName);

            await File.WriteAllTextAsync(filePath, csvContent);

            var totalSales = bills.Sum(b => b.Total);
            var totalExpenses = expenses.Sum(e => e.Amount);
            
            var totalCashSales = bills.Where(b => b.PaymentMethod == "CASH" || b.PaymentMethod == "Cash").Sum(b => b.Total);
            var totalCashExpenses = expenses.Where(e => e.PaymentMethod == "Cash" || e.PaymentMethod == "Petty Cash").Sum(e => e.Amount);
            var netCashInHand = totalCashSales - totalCashExpenses;

            var summary = new StringBuilder();
            summary.AppendLine($"Attached is the {type} {category} report for the period {start:yyyy-MM-dd} to {end:yyyy-MM-dd}.\n");
            
            if (category == "Sales" || category == "All")
            {
                summary.AppendLine($"Sales Summary:");
                summary.AppendLine($"Total Sales: {totalSales:C}");
                summary.AppendLine($"Cash Sales: {totalCashSales:C}");
                var platformStats = bills.GroupBy(b => b.Platform)
                    .Select(g => $"{g.Key}: {g.Count()} orders, Total: {g.Sum(b => b.Total):C}");
                summary.AppendLine("Platform Breakdown:");
                summary.AppendLine(string.Join("\n", platformStats));
                summary.AppendLine();
            }

            if (category == "Expenses" || category == "All")
            {
                summary.AppendLine($"Expense Summary:");
                summary.AppendLine($"Total Expenses: {totalExpenses:C}");
                summary.AppendLine($"Cash Expenses: {totalCashExpenses:C}");
                summary.AppendLine();
            }

            if (category == "All")
            {
                summary.AppendLine($"-----------------------------");
                summary.AppendLine($"Net Cash in Hand: {netCashInHand:C}");
            }

            var emailList = recipients.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var email in emailList)
            {
                await _emailService.SendEmailAsync(email.Trim(), $"{type} {category} Report", summary.ToString(), filePath);
            }
        }

        private string GenerateCsv(List<Models.Bill> bills, List<Models.Expense> expenses, string category)
        {
            var sb = new StringBuilder();
            
            if (category == "Sales" || category == "All")
            {
                // Section 1: Sales
                sb.AppendLine("SALES TRANSACTIONS");
                sb.AppendLine("BillNumber,Date,Platform,PaymentMethod,Subtotal,GST,ServiceCharge,Total");
                foreach (var bill in bills)
                {
                    sb.AppendLine($"{bill.BillNumber},{bill.CreatedAt:yyyy-MM-dd HH:mm:ss},{bill.Platform},{bill.PaymentMethod},{bill.Subtotal},{bill.Gst},{bill.ServiceCharge},{bill.Total}");
                }
                sb.AppendLine("");
                sb.AppendLine($"Total Sales,,,{bills.Sum(b => b.Total)}");
                sb.AppendLine("");
            }

            if (category == "Expenses" || category == "All")
            {
                // Section 2: Expenses
                sb.AppendLine("EXPENSES");
                sb.AppendLine("Date,Category,Description,Vendor,PaymentMethod,Amount");
                foreach (var exp in expenses)
                {
                    sb.AppendLine($"{exp.Date:yyyy-MM-dd HH:mm:ss},{exp.Category?.Name ?? "Unknown"},{exp.Description},{exp.VendorName},{exp.PaymentMethod},{exp.Amount}");
                }
                sb.AppendLine("");
                sb.AppendLine($"Total Expenses,,,,,{expenses.Sum(e => e.Amount)}");
            }

            return sb.ToString();
        }
    }
}
