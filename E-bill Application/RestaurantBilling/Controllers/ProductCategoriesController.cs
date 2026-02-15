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
    public class ProductCategoriesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProductCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductCategoryDto>>> GetCategories()
        {
            var categories = await _context.ProductCategories
                .AsNoTracking()
                .Where(pc => pc.UserId == CurrentUserId && pc.IsActive)
                .OrderBy(pc => pc.Name)
                .Select(pc => new ProductCategoryDto
                {
                    Id = pc.Id,
                    Name = pc.Name,
                    IsActive = pc.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<ActionResult<ProductCategoryDto>> CreateCategory(CreateProductCategoryDto dto)
        {
            var category = new ProductCategory
            {
                Name = dto.Name,
                UserId = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductCategoryDto>> UpdateCategory(int id, CreateProductCategoryDto dto)
        {
             // Wait, the DTO name should be consistent. Let's use simple string or CreateProductCategoryDto
             // I'll use CreateProductCategoryDto for now as it just has Name.
             
            var category = await _context.ProductCategories
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.UserId == CurrentUserId);

            if (category == null)
                return NotFound(new { error = "Category not found" });

            var oldName = category.Name;
            var newName = dto.Name;

            if (oldName != newName)
            {
                category.Name = newName;

                // Cascade update products using this category name
                var productsToUpdate = await _context.Products
                    .Where(p => p.UserId == CurrentUserId && p.Category == oldName)
                    .ToListAsync();

                foreach (var product in productsToUpdate)
                {
                    product.Category = newName;
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.ProductCategories
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.UserId == CurrentUserId);

            if (category == null)
                return NotFound(new { error = "Category not found" });

            category.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
