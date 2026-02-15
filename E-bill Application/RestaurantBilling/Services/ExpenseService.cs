using Microsoft.EntityFrameworkCore;
using RestaurantBilling.Data;
using RestaurantBilling.DTOs;
using RestaurantBilling.Models;

namespace RestaurantBilling.Services
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseCategoryDto>> GetCategoriesAsync(int userId);
        Task<ExpenseCategoryDto> CreateCategoryAsync(int userId, CreateExpenseCategoryDto dto);
        Task<ExpenseDto> CreateExpenseAsync(int userId, CreateExpenseDto dto);
        Task<(IEnumerable<ExpenseDto> Items, int TotalCount)> GetExpensesAsync(int userId, DateTime? startDate, DateTime? endDate, int? categoryId, int page, int pageSize);
        Task<ExpenseSummaryDto> GetExpenseSummaryAsync(int userId, DateTime? startDate, DateTime? endDate);
        Task DeleteExpenseAsync(int userId, int expenseId);
    }

    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExpenseCategoryDto>> GetCategoriesAsync(int userId)
        {
            return await _context.ExpenseCategories
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.IsActive)
                .Select(c => new ExpenseCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<ExpenseCategoryDto> CreateCategoryAsync(int userId, CreateExpenseCategoryDto dto)
        {
            var category = new ExpenseCategory
            {
                Name = dto.Name,
                UserId = userId,
                IsActive = true
            };

            _context.ExpenseCategories.Add(category);
            await _context.SaveChangesAsync();

            return new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            };
        }

        public async Task<ExpenseDto> CreateExpenseAsync(int userId, CreateExpenseDto dto)
        {
            // Verify category belongs to user
            var category = await _context.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.UserId == userId);
            
            if (category == null)
                throw new KeyNotFoundException("Category not found");

            var expenseDate = dto.Date ?? Helpers.DateTimeHelper.GetIndianTime();

            var expense = new Expense
            {
                Date = expenseDate,
                Amount = dto.Amount,
                CategoryId = dto.CategoryId,
                PaymentMethod = dto.PaymentMethod,
                Description = dto.Description,
                VendorName = dto.VendorName,
                ReceiptImagePath = dto.ReceiptImagePath,
                UserId = userId,
                CreatedAt = Helpers.DateTimeHelper.GetIndianTime()
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return new ExpenseDto
            {
                Id = expense.Id,
                Date = expense.Date,
                Amount = expense.Amount,
                CategoryId = expense.CategoryId,
                CategoryName = category.Name,
                PaymentMethod = expense.PaymentMethod,
                Description = expense.Description,
                VendorName = expense.VendorName,
                ReceiptImagePath = expense.ReceiptImagePath,
                CreatedAt = expense.CreatedAt
            };
        }

        public async Task<(IEnumerable<ExpenseDto> Items, int TotalCount)> GetExpensesAsync(int userId, DateTime? startDate, DateTime? endDate, int? categoryId, int page, int pageSize)
        {
            var query = _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value.Date);
            
            if (endDate.HasValue)
                query = query.Where(e => e.Date < endDate.Value.Date.AddDays(1));

            if (categoryId.HasValue)
                query = query.Where(e => e.CategoryId == categoryId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(e => e.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    Date = e.Date,
                    Amount = e.Amount,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category != null ? e.Category.Name : "Unknown",
                    PaymentMethod = e.PaymentMethod,
                    Description = e.Description,
                    VendorName = e.VendorName,
                    ReceiptImagePath = e.ReceiptImagePath,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<ExpenseSummaryDto> GetExpenseSummaryAsync(int userId, DateTime? startDate, DateTime? endDate)
        {
            var now = Helpers.DateTimeHelper.GetIndianTime();
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Filtered Query
            var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();
            if (startDate.HasValue) query = query.Where(e => e.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(e => e.Date < endDate.Value.Date.AddDays(1));
            
            var totalFiltered = await query.SumAsync(e => e.Amount);
            var todayFiltered = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= todayStart && e.Date < todayEnd)
                .SumAsync(e => e.Amount);
            var monthFiltered = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= monthStart && e.Date < monthEnd)
                .SumAsync(e => e.Amount);

            // Top Categories (current month IST)
            var topCategories = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= monthStart && e.Date < monthEnd)
                .GroupBy(e => e.Category != null ? e.Category.Name : "Unknown")
                .Select(g => new CategoryExpenseSummaryDto
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(e => e.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(5)
                .ToListAsync();

            return new ExpenseSummaryDto
            {
                TodayFiltered = todayFiltered,
                MonthFiltered = monthFiltered,
                TotalFiltered = totalFiltered,
                TodayTotal = todayFiltered, 
                MonthTotal = monthFiltered,
                TopCategories = topCategories
            };
        }

        public async Task DeleteExpenseAsync(int userId, int expenseId)
        {
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.UserId == userId);
            if (expense == null)
                throw new KeyNotFoundException("Expense not found");

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
        }
    }
}
