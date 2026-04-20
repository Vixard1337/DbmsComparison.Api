using DbmsComparison.Api.Data;
using DbmsComparison.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Services;

public class DataSeeder
{
    public async Task<SeedResult> SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var hasData = await context.Users.AnyAsync(cancellationToken)
            || await context.Products.AnyAsync(cancellationToken)
            || await context.Categories.AnyAsync(cancellationToken)
            || await context.Orders.AnyAsync(cancellationToken)
            || await context.Locations.AnyAsync(cancellationToken);

        if (hasData)
        {
            return new SeedResult(
                AddedTotal: 0,
                Users: 0,
                Categories: 0,
                Products: 0,
                ProductCategories: 0,
                Orders: 0,
                OrderItems: 0,
                Locations: 0,
                Skipped: true,
                Message: "Seed skipped because data already exists.");
        }

        var rootCategory = new Category { Name = "Electronics" };
        var childCategory = new Category { Name = "Computers", ParentCategory = rootCategory };

        var laptop = new Product
        {
            Name = "Laptop",
            Price = 4999.99m,
            Metadata = "{\"brand\":\"Contoso\",\"ramGb\":16,\"storageGb\":512}"
        };

        var mouse = new Product
        {
            Name = "Mouse",
            Price = 129.99m,
            Metadata = "{\"brand\":\"Contoso\",\"wireless\":true}"
        };

        var userOne = new User
        {
            Name = "Alice Smith",
            Email = "alice@example.com",
            CreatedAt = DateTime.UtcNow,
            ProfileImage = [1, 2, 3, 4]
        };

        var userTwo = new User
        {
            Name = "Bob Johnson",
            Email = "bob@example.com",
            CreatedAt = DateTime.UtcNow,
            ProfileImage = [5, 6, 7, 8]
        };

        var orderOne = new Order
        {
            User = userOne,
            CreatedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(15),
            TotalAmount = 5129.98m
        };

        var orderTwo = new Order
        {
            User = userTwo,
            CreatedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(8),
            TotalAmount = 129.99m
        };

        var orderItemOne = new OrderItem
        {
            Order = orderOne,
            Product = laptop,
            Quantity = 1,
            UnitPrice = 4999.99m
        };

        var orderItemTwo = new OrderItem
        {
            Order = orderOne,
            Product = mouse,
            Quantity = 1,
            UnitPrice = 129.99m
        };

        var orderItemThree = new OrderItem
        {
            Order = orderTwo,
            Product = mouse,
            Quantity = 1,
            UnitPrice = 129.99m
        };

        var productCategoryOne = new ProductCategory { Product = laptop, Category = childCategory };
        var productCategoryTwo = new ProductCategory { Product = mouse, Category = rootCategory };

        var locationOne = new Location
        {
            Name = "HQ",
            Latitude = 52.2297,
            Longitude = 21.0122
        };

        var locationTwo = new Location
        {
            Name = "Branch Office",
            Latitude = 50.0647,
            Longitude = 19.9450
        };

        context.AddRange(rootCategory, childCategory);
        context.AddRange(laptop, mouse);
        context.AddRange(userOne, userTwo);
        context.AddRange(orderOne, orderTwo);
        context.AddRange(orderItemOne, orderItemTwo, orderItemThree);
        context.AddRange(productCategoryOne, productCategoryTwo);
        context.AddRange(locationOne, locationTwo);

        await context.SaveChangesAsync(cancellationToken);

        return new SeedResult(
            AddedTotal: 15,
            Users: 2,
            Categories: 2,
            Products: 2,
            ProductCategories: 2,
            Orders: 2,
            OrderItems: 3,
            Locations: 2,
            Skipped: false,
            Message: "Seed completed successfully.");
    }
}

public sealed record SeedResult(
    int AddedTotal,
    int Users,
    int Categories,
    int Products,
    int ProductCategories,
    int Orders,
    int OrderItems,
    int Locations,
    bool Skipped,
    string Message);
