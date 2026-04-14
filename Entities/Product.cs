namespace DbmsComparison.Api.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Metadata { get; set; } = "{}";

    public ICollection<OrderItem> OrderItems { get; set; } = [];

    public ICollection<ProductCategory> ProductCategories { get; set; } = [];
}
