using DbmsComparison.Api.Data;
using DbmsComparison.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController(IConfiguration configuration, DataSeeder dataSeeder) : ControllerBase
{
    [HttpGet("providers")]
    public IActionResult Providers()
    {
        var defaultProvider = configuration["Database:DefaultProvider"] ?? "sqlserver";

        return Ok(new
        {
            defaultProvider,
            supported = new[] { "sqlserver", "postgres", "mysql", "sqlite" }
        });
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test([FromQuery] string db = "sqlserver")
    {
        if (!DbContextOptionsFactory.TryParse(db, out var provider))
        {
            return BadRequest(new
            {
                message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite."
            });
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, provider);

        await using var context = new AppDbContext(optionsBuilder.Options);
        var canConnect = await context.Database.CanConnectAsync();

        return Ok(new
        {
            db,
            provider = context.Database.ProviderName,
            canConnect
        });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!DbContextOptionsFactory.TryParse(db, out var parsedProvider))
        {
            return BadRequest(new
            {
                message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite."
            });
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, parsedProvider);

        await using var context = new AppDbContext(optionsBuilder.Options);

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return BadRequest(new
            {
                db,
                message = "Cannot connect to database. Ensure migrations are applied before seeding."
            });
        }

        var seedResult = await dataSeeder.SeedAsync(context, cancellationToken);

        return Ok(new
        {
            db,
            provider = context.Database.ProviderName,
            seedResult.AddedTotal,
            seedResult.Users,
            seedResult.Categories,
            seedResult.Products,
            seedResult.ProductCategories,
            seedResult.Orders,
            seedResult.OrderItems,
            seedResult.Locations,
            seedResult.Skipped,
            seedResult.Message
        });
    }

    [HttpGet("seed/status")]
    public async Task<IActionResult> SeedStatus([FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!DbContextOptionsFactory.TryParse(db, out var parsedProvider))
        {
            return BadRequest(new
            {
                message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite."
            });
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, parsedProvider);

        await using var context = new AppDbContext(optionsBuilder.Options);

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return BadRequest(new
            {
                db,
                message = "Cannot connect to database. Ensure database is available."
            });
        }

        var users = await context.Users.CountAsync(cancellationToken);
        var categories = await context.Categories.CountAsync(cancellationToken);
        var products = await context.Products.CountAsync(cancellationToken);
        var productCategories = await context.ProductCategories.CountAsync(cancellationToken);
        var orders = await context.Orders.CountAsync(cancellationToken);
        var orderItems = await context.OrderItems.CountAsync(cancellationToken);
        var locations = await context.Locations.CountAsync(cancellationToken);

        return Ok(new
        {
            db,
            provider = context.Database.ProviderName,
            Users = users,
            Categories = categories,
            Products = products,
            ProductCategories = productCategories,
            Orders = orders,
            OrderItems = orderItems,
            Locations = locations,
            Total = users + categories + products + productCategories + orders + orderItems + locations
        });
    }
}
