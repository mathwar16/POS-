using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Data;
using RestaurantBilling.DTOs;
using RestaurantBilling.Models;
using Microsoft.AspNetCore.Authorization;

namespace RestaurantBilling.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public BillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> CreateBill(CreateBillDto dto)
        {
            // Use Indian Standard Time for all bill logic
            var billDate = dto.Date == default ? Helpers.DateTimeHelper.GetIndianTime() : Helpers.DateTimeHelper.ToIndianTime(dto.Date);

            var tokenNumber = GenerateTokenNumber(billDate);
            var billNumber = GenerateBillNumber(billDate, tokenNumber);

            var bill = new Bill
            {
                TokenNumber = tokenNumber,
                BillNumber = billNumber,
                UserId = CurrentUserId,
                Subtotal = dto.Subtotal,
                Gst = dto.Gst,
                ServiceCharge = dto.Service,
                Total = dto.Total,
                PaymentMethod = dto.PaymentMethod,
                Platform = dto.Platform,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CreatedAt = billDate,
                UpdatedAt = Helpers.DateTimeHelper.GetIndianTime()
            };

            foreach (var itemDto in dto.Items)
            {
                var billItem = new BillItem
                {
                    Bill = bill,
                    ProductId = itemDto.Id,
                    ProductName = itemDto.Name,
                    Price = itemDto.Price,
                    Quantity = itemDto.Quantity,
                    Total = itemDto.Total,
                    CreatedAt = Helpers.DateTimeHelper.GetIndianTime()
                };

                bill.BillItems.Add(billItem);
            }

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBill), new { id = bill.Id }, MapToDto(bill));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId);

            if (bill == null)
                return NotFound(new { error = "Bill not found" });

            return Ok(MapToDto(bill));
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAllBills(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.Bills
                .AsNoTracking()
                .Where(b => b.UserId == CurrentUserId)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date; // Date is already in local/IST from frontend usually, but we treat it as start of day
                query = query.Where(b => b.CreatedAt >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(b => b.CreatedAt < end);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bills = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillDto
                {
                    Id = b.Id,
                    TokenNumber = b.TokenNumber,
                    BillNumber = b.BillNumber,
                    Subtotal = b.Subtotal,
                    Gst = b.Gst,
                    ServiceCharge = b.ServiceCharge,
                    Total = b.Total,
                    PaymentMethod = b.PaymentMethod,
                    Platform = b.Platform,
                    CustomerName = b.CustomerName,
                    CustomerPhone = b.CustomerPhone,
                    CreatedAt = b.CreatedAt,
                    Items = b.BillItems.Select(i => new BillItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Total = i.Total
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                items = bills,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = totalPages
            });
        }

        /// <summary>
        /// Generate a daily token number that resets every day per user based on IST.
        /// </summary>
        private int GenerateTokenNumber(DateTime billDateIst)
        {
            var startOfDay = billDateIst.Date;
            var endOfDay = startOfDay.AddDays(1);

            var countToday = _context.Bills
                .Count(b =>
                    b.UserId == CurrentUserId &&
                    b.CreatedAt >= startOfDay &&
                    b.CreatedAt < endOfDay);

            // Next token is count + 1; this naturally resets when the date changes
            return countToday + 1;
        }

        /// <summary>
        /// Generate a bill number string based on the bill date (IST) and token number.
        /// </summary>
        private string GenerateBillNumber(DateTime billDateIst, int tokenNumber)
        {
            var datePart = billDateIst.ToString("yyyyMMdd");
            return $"BILL-{datePart}-{tokenNumber:D3}";
        }

        private static BillDto MapToDto(Bill bill)
        {
            return new BillDto
            {
                Id = bill.Id,
                TokenNumber = bill.TokenNumber,
                BillNumber = bill.BillNumber,
                Subtotal = bill.Subtotal,
                Gst = bill.Gst,
                ServiceCharge = bill.ServiceCharge,
                Total = bill.Total,
                PaymentMethod = bill.PaymentMethod,
                Platform = bill.Platform,
                CustomerName = bill.CustomerName,
                CustomerPhone = bill.CustomerPhone,
                CreatedAt = bill.CreatedAt,
                Items = bill.BillItems.Select(i => new BillItemResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Total = i.Total
                }).ToList()
            };
        }
    }
}
