using DbmsComparison.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController(IConfiguration configuration) : ControllerBase
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
}
