using DbmsComparison.Api.Data;
using DbmsComparison.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var orders = await context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.CreatedAt,
                    x.Duration,
                    x.TotalAmount,
                    Items = x.OrderItems.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        ProductName = i.Product.Name,
                        i.Quantity,
                        i.UnitPrice
                    })
                })
                .ToListAsync(cancellationToken);

            return Ok(orders);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var order = await context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.CreatedAt,
                    x.Duration,
                    x.TotalAmount,
                    Items = x.OrderItems.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        ProductName = i.Product.Name,
                        i.Quantity,
                        i.UnitPrice
                    })
                })
                .FirstOrDefaultAsync(cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var userExists = await context.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                return BadRequest(new { message = "UserId does not exist." });
            }

            var requestedProductIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
            var products = await context.Products
                .Where(x => requestedProductIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            if (products.Count != requestedProductIds.Count)
            {
                return BadRequest(new { message = "One or more ProductId values do not exist." });
            }

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0m;

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                totalAmount += product.Price * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            var order = new Order
            {
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(request.DurationMinutes),
                TotalAmount = totalAmount,
                OrderItems = orderItems
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync(cancellationToken);

            var response = new
            {
                order.Id,
                order.UserId,
                order.CreatedAt,
                order.Duration,
                order.TotalAmount,
                Items = order.OrderItems.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice
                })
            };

            return CreatedAtAction(nameof(GetById), new { id = order.Id, db }, response);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var order = await context.Orders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return NotFound();
            }

            context.Orders.Remove(order);
            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }

    private bool TryCreateContext(string db, out AppDbContext? context, out IActionResult? errorResult)
    {
        context = null;
        errorResult = null;

        if (!DbContextOptionsFactory.TryParse(db, out var provider))
        {
            errorResult = BadRequest(new { message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite." });
            return false;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, provider);
        context = new AppDbContext(optionsBuilder.Options);

        return true;
    }

    private static string? ValidateRequest(CreateOrderRequest request)
    {
        if (request.UserId <= 0)
        {
            return "UserId is required.";
        }

        if (request.DurationMinutes <= 0)
        {
            return "DurationMinutes must be greater than 0.";
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return "At least one order item is required.";
        }

        if (request.Items.Any(x => x.ProductId <= 0))
        {
            return "Each item must contain valid ProductId.";
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            return "Each item must have Quantity greater than 0.";
        }

        return null;
    }

    public sealed record CreateOrderItemRequest(int ProductId, int Quantity);

    public sealed record CreateOrderRequest(int UserId, int DurationMinutes, List<CreateOrderItemRequest> Items);
}
