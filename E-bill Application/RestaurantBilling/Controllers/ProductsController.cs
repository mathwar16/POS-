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
    public class ProductsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.UserId == CurrentUserId && p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Category = p.Category,
                    IsFavorite = p.IsFavorite
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId && p.IsActive);

            if (product == null)
                return NotFound(new { error = "Product not found" });

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Category = product.Category,
                IsFavorite = product.IsFavorite
            });
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Category = dto.Category,
                IsFavorite = dto.IsFavorite,
                UserId = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Category = product.Category,
                    IsFavorite = product.IsFavorite
                });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductDto>> UpdateProduct(int id, UpdateProductDto dto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId);

            if (product == null)
                return NotFound(new { error = "Product not found" });

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Category = dto.Category;
            product.IsFavorite = dto.IsFavorite;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Category = product.Category,
                IsFavorite = product.IsFavorite
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId);

            if (product == null)
                return NotFound(new { error = "Product not found" });

            product.IsActive = false; // Soft delete
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
