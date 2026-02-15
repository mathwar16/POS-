namespace RestaurantBilling.DTOs
{
    public class ProductCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateProductCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
