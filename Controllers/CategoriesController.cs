using DbmsComparison.Api.Data;
using DbmsComparison.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(IConfiguration configuration) : ControllerBase
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
            var categories = await context.Categories
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.ParentCategoryId
                })
                .ToListAsync(cancellationToken);

            return Ok(categories);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await context.Categories.AnyAsync(x => x.Id == request.ParentCategoryId.Value, cancellationToken);
                if (!parentExists)
                {
                    return BadRequest(new { message = "ParentCategoryId does not exist." });
                }
            }

            var category = new Category
            {
                Name = request.Name.Trim(),
                ParentCategoryId = request.ParentCategoryId
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetAll), new { db }, new
            {
                category.Id,
                category.Name,
                category.ParentCategoryId
            });
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

    public sealed record CreateCategoryRequest(string Name, int? ParentCategoryId);
}
