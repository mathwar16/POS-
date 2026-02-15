using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBilling.DTOs;
using RestaurantBilling.Services;
using System.Security.Claims;

namespace RestaurantBilling.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpensesController : BaseController
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<ExpenseCategoryDto>>> GetCategories()
        {
            var categories = await _expenseService.GetCategoriesAsync(CurrentUserId);
            return Ok(categories);
        }

        [HttpPost("categories")]
        public async Task<ActionResult<ExpenseCategoryDto>> CreateCategory(CreateExpenseCategoryDto dto)
        {
            var category = await _expenseService.CreateCategoryAsync(CurrentUserId, dto);
            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> CreateExpense(CreateExpenseDto dto)
        {
            try
            {
                var expense = await _expenseService.CreateExpenseAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetExpenses), new { id = expense.Id }, expense);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetExpenses(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, totalCount) = await _expenseService.GetExpensesAsync(CurrentUserId, startDate, endDate, categoryId, page, pageSize);
            
            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ExpenseSummaryDto>> GetSummary(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var summary = await _expenseService.GetExpenseSummaryAsync(CurrentUserId, startDate, endDate);
            return Ok(summary);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                await _expenseService.DeleteExpenseAsync(CurrentUserId, id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
