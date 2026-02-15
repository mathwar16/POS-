using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Data;
using RestaurantBilling.DTOs;
using RestaurantBilling.Models;

namespace RestaurantBilling.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentStart = startDate ?? Helpers.DateTimeHelper.GetIndianTime().Date;
            var currentEnd = endDate ?? Helpers.DateTimeHelper.GetIndianTime().Date;

            // Dates are treated as IST dates
            var startIst = currentStart.Date;
            var endIst = currentEnd.Date.AddDays(1);

            // Calculate previous period for trends
            var duration = endIst - startIst;
            var prevStartIst = startIst.Add(-duration);
            var prevEndIst = startIst;

            var currentBills = await _context.Bills
                .AsNoTracking()
                .Where(b => b.UserId == CurrentUserId && b.CreatedAt >= startIst && b.CreatedAt < endIst)
                .Include(b => b.BillItems)
                .ToListAsync();

            var prevBills = await _context.Bills
                .AsNoTracking()
                .Where(b => b.UserId == CurrentUserId && b.CreatedAt >= prevStartIst && b.CreatedAt < prevEndIst)
                .ToListAsync();

            // Current Metrics
            var totalRevenue = currentBills.Sum(b => b.Total);
            var grossRevenue = currentBills.Sum(b => b.Subtotal);
            var totalOrders = currentBills.Count;
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
            
            // Trends
            var prevRevenue = prevBills.Sum(b => b.Total);
            var prevOrders = prevBills.Count;
            var prevAov = prevOrders > 0 ? prevRevenue / prevOrders : 0;

            var revenueTrend = CalculateTrend(totalRevenue, prevRevenue);
            var ordersTrend = CalculateTrend(totalOrders, prevOrders);
            var aovTrend = CalculateTrend(avgOrderValue, prevAov);

            // Peak Order Time (Hours are already IST as CreatedAt is stored in IST)
            var peakHour = currentBills
                .GroupBy(b => b.CreatedAt.Hour)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
            var peakOrderTime = $"{peakHour:D2}:00 - {(peakHour + 1) % 24:D2}:00";

            // Payment Methods
            var paymentMethods = currentBills
                .GroupBy(b => b.PaymentMethod)
                .Select(g => new SummaryItemDto
                {
                    Name = g.Key,
                    Amount = g.Sum(b => b.Total),
                    Count = g.Count()
                })
                .ToList();

            // Platform Breakdown
            var platformBreakdown = currentBills
                .GroupBy(b => b.Platform)
                .Select(g => new SummaryItemDto
                {
                    Name = g.Key,
                    Amount = g.Sum(b => b.Total),
                    Count = g.Count()
                })
                .ToList();

            // Chart Data (Grouped by Date or Hour)
            var chartData = GetChartData(currentBills, startIst, endIst);

            // Recent Orders (Paginated)
            var recentOrders = currentBills
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new RecentOrderDto
                {
                    Id = b.Id,
                    BillNumber = b.BillNumber,
                    Total = b.Total,
                    Subtotal = b.Subtotal,
                    PaymentMethod = b.PaymentMethod,
                    Platform = b.Platform,
                    Date = b.CreatedAt
                })
                .ToList();

            return Ok(new
            {
                summary = new DashboardDto
                {
                    TotalRevenue = totalRevenue,
                    GrossRevenue = grossRevenue,
                    TotalOrders = totalOrders,
                    AvgOrderValue = avgOrderValue,
                    TotalCustomers = totalOrders, // Omit for now or use total orders as proxy
                    PeakOrderTime = peakOrderTime,
                    RevenueTrend = revenueTrend,
                    OrdersTrend = ordersTrend,
                    AovTrend = aovTrend,
                    PaymentMethods = paymentMethods,
                    PlatformBreakdown = platformBreakdown,
                    RecentOrders = recentOrders,
                    RevenueChart = chartData.RevenuePoints,
                    OrderVolumeChart = chartData.OrderPoints,
                    BestSellingProducts = currentBills
                        .SelectMany(b => b.BillItems)
                        .GroupBy(bi => bi.ProductName)
                        .Select(g => new SummaryItemDto
                        {
                            Name = g.Key,
                            Amount = g.Sum(bi => bi.Total),
                            Count = g.Sum(bi => bi.Quantity)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList()
                },
                pagination = new
                {
                    totalCount = totalOrders,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize)
                }
            });
        }

        private double CalculateTrend(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return (double)((current - previous) / previous * 100);
        }

        private (List<ChartDataPointDto> RevenuePoints, List<ChartDataPointDto> OrderPoints) GetChartData(List<Bill> bills, DateTime start, DateTime end)
        {
            var durationDays = (end - start).TotalDays;

            if (durationDays <= 2) // Hourly for small ranges
            {
                var hourly = bills.GroupBy(b => b.CreatedAt.Hour)
                    .Select(g => new { Hour = g.Key, Revenue = g.Sum(b => b.Total), Count = g.Count() })
                    .ToDictionary(x => x.Hour, x => x);

                var revenuePoints = new List<ChartDataPointDto>();
                var orderPoints = new List<ChartDataPointDto>();

                for (int i = 0; i < 24; i++)
                {
                    var label = $"{i:D2}:00";
                    var val = hourly.ContainsKey(i) ? hourly[i].Revenue : 0;
                    var count = hourly.ContainsKey(i) ? hourly[i].Count : 0;
                    revenuePoints.Add(new ChartDataPointDto { Label = label, Value = val });
                    orderPoints.Add(new ChartDataPointDto { Label = label, Value = count, Count = count });
                }
                return (revenuePoints, orderPoints);
            }
            else if (durationDays > 60) // Monthly for large ranges
            {
                var monthly = bills.GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(b => b.Total), Count = g.Count() })
                    .ToDictionary(x => new DateTime(x.Year, x.Month, 1), x => x);

                var revenuePoints = new List<ChartDataPointDto>();
                var orderPoints = new List<ChartDataPointDto>();

                for (var d = new DateTime(start.Year, start.Month, 1); d <= end; d = d.AddMonths(1))
                {
                    var label = d.ToString("MMM yyyy");
                    var val = monthly.ContainsKey(d) ? monthly[d].Revenue : 0;
                    var count = monthly.ContainsKey(d) ? monthly[d].Count : 0;
                    revenuePoints.Add(new ChartDataPointDto { Label = label, Value = val });
                    orderPoints.Add(new ChartDataPointDto { Label = label, Value = count, Count = count });
                }
                return (revenuePoints, orderPoints);
            }
            else // Daily for mid ranges
            {
                var daily = bills.GroupBy(b => b.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(b => b.Total), Count = g.Count() })
                    .ToDictionary(x => x.Date, x => x);

                var revenuePoints = new List<ChartDataPointDto>();
                var orderPoints = new List<ChartDataPointDto>();

                for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
                {
                    var label = d.ToString("MMM dd");
                    var val = daily.ContainsKey(d) ? daily[d].Revenue : 0;
                    var count = daily.ContainsKey(d) ? daily[d].Count : 0;
                    revenuePoints.Add(new ChartDataPointDto { Label = label, Value = val });
                    orderPoints.Add(new ChartDataPointDto { Label = label, Value = count, Count = count });
                }
                return (revenuePoints, orderPoints);
            }
        }

        [HttpGet("today")]
        public async Task<ActionResult<object>> GetTodayDashboard()
        {
            var today = Helpers.DateTimeHelper.GetIndianTime().Date;
            return await GetDashboardStats(today, today);
        }
    }
}
