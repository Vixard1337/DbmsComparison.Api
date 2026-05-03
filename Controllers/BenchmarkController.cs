using DbmsComparison.Api.Data;
using DbmsComparison.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenchmarkController(IConfiguration configuration, BenchmarkRunner runner, BenchmarkBatchRunner batchRunner) : ControllerBase
{
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromQuery] string db = "sqlserver", [FromQuery] string scenario = "S1", [FromQuery] int? rows = null, CancellationToken cancellationToken = default)
    {
        if (!DbContextOptionsFactory.TryParse(db, out var provider))
        {
            return BadRequest(new { message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite." });
        }

        if (!Enum.TryParse<BenchmarkScenario>(scenario, true, out var parsedScenario))
        {
            return BadRequest(new { message = "Unsupported scenario. Use: S1, S2, S3." });
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, provider);

        await using var context = new AppDbContext(optionsBuilder.Options);

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return BadRequest(new { db, message = "Cannot connect to database. Ensure migrations are applied." });
        }

        var result = await runner.RunAsync(context, parsedScenario, rows, cancellationToken);
        await runner.WriteResultAsync(result, db, context.Database.ProviderName ?? "unknown", cancellationToken);

        return Ok(new
        {
            db,
            provider = context.Database.ProviderName,
            result.RunId,
            Scenario = result.Scenario.ToString(),
            result.RowCount,
            result.TimeMs,
            result.CpuMs,
            result.RamMb,
            result.Tps,
            result.ReadCount
        });
    }

    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll([FromQuery] string db = "sqlserver", [FromQuery] int repetitions = 5, CancellationToken cancellationToken = default)
    {
        if (!DbContextOptionsFactory.TryParse(db, out var provider))
        {
            return BadRequest(new { message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite." });
        }

        if (repetitions <= 0)
        {
            return BadRequest(new { message = "Repetitions must be greater than 0." });
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, provider);

        await using var context = new AppDbContext(optionsBuilder.Options);

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return BadRequest(new { db, message = "Cannot connect to database. Ensure migrations are applied." });
        }

        var results = await batchRunner.RunAsync(context, db, context.Database.ProviderName ?? "unknown", repetitions, cancellationToken);

        return Ok(new
        {
            db,
            provider = context.Database.ProviderName,
            repetitions,
            scenarios = results
                .GroupBy(x => x.Scenario)
                .OrderBy(x => x.Key)
                .Select(x => new
                {
                    Scenario = x.Key.ToString(),
                    Runs = x.Select(r => new
                    {
                        r.Iteration,
                        r.RunId,
                        r.RowCount,
                        r.TimeMs,
                        r.Tps
                    })
                })
        });
    }
}
