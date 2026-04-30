using System.Text.Json;
using DbmsComparison.Api.Data;
using DbmsComparison.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IConfiguration configuration) : ControllerBase
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
            var products = await context.Products
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Price,
                    x.Metadata
                })
                .ToListAsync(cancellationToken);

            return Ok(products);
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
            var product = await context.Products
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Price,
                    x.Metadata
                })
                .FirstOrDefaultAsync(cancellationToken);

            return product is null ? NotFound() : Ok(product);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertProductRequest request, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
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
            var product = new Product
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                Metadata = request.Metadata.Trim()
            };

            context.Products.Add(product);
            await context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = product.Id, db }, new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Metadata
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertProductRequest request, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
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
            var product = await context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (product is null)
            {
                return NotFound();
            }

            product.Name = request.Name.Trim();
            product.Price = request.Price;
            product.Metadata = request.Metadata.Trim();

            await context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Metadata
            });
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
            var product = await context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (product is null)
            {
                return NotFound();
            }

            context.Products.Remove(product);
            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }

    [HttpPost("{productId:int}/categories/{categoryId:int}")]
    public async Task<IActionResult> AddCategory(int productId, int categoryId, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var productExists = await context.Products.AnyAsync(x => x.Id == productId, cancellationToken);
            if (!productExists)
            {
                return NotFound(new { message = "Product not found." });
            }

            var categoryExists = await context.Categories.AnyAsync(x => x.Id == categoryId, cancellationToken);
            if (!categoryExists)
            {
                return NotFound(new { message = "Category not found." });
            }

            var alreadyLinked = await context.ProductCategories
                .AnyAsync(x => x.ProductId == productId && x.CategoryId == categoryId, cancellationToken);

            if (alreadyLinked)
            {
                return Conflict(new { message = "Product is already linked to the category." });
            }

            context.ProductCategories.Add(new ProductCategory
            {
                ProductId = productId,
                CategoryId = categoryId
            });

            await context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                ProductId = productId,
                CategoryId = categoryId
            });
        }
    }

    [HttpDelete("{productId:int}/categories/{categoryId:int}")]
    public async Task<IActionResult> RemoveCategory(int productId, int categoryId, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var link = await context.ProductCategories
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.CategoryId == categoryId, cancellationToken);

            if (link is null)
            {
                return NotFound(new { message = "Product-category link not found." });
            }

            context.ProductCategories.Remove(link);
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

    private static string? ValidateRequest(UpsertProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (request.Price < 0)
        {
            return "Price must be greater than or equal to 0.";
        }

        if (string.IsNullOrWhiteSpace(request.Metadata))
        {
            return "Metadata is required and must be valid JSON.";
        }

        try
        {
            using var _ = JsonDocument.Parse(request.Metadata);
        }
        catch
        {
            return "Metadata must be valid JSON.";
        }

        return null;
    }

    public sealed record UpsertProductRequest(string Name, decimal Price, string Metadata);
}
