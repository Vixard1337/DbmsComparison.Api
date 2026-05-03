using DbmsComparison.Api.Data;

namespace DbmsComparison.Api.Services;

public class BenchmarkBatchRunner(BenchmarkRunner runner)
{
    public async Task<IReadOnlyList<BenchmarkBatchResult>> RunAsync(
        AppDbContext context,
        string db,
        string providerName,
        int repetitions,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BenchmarkBatchResult>();

        foreach (BenchmarkScenario scenario in Enum.GetValues(typeof(BenchmarkScenario)))
        {
            for (var i = 1; i <= repetitions; i++)
            {
                var result = await runner.RunAsync(context, scenario, null, cancellationToken);
                await runner.WriteResultAsync(result, db, providerName, cancellationToken);
                results.Add(new BenchmarkBatchResult(scenario, i, result.RunId, result.RowCount, result.TimeMs, result.Tps));
            }
        }

        return results;
    }
}

public sealed record BenchmarkBatchResult(
    BenchmarkScenario Scenario,
    int Iteration,
    Guid RunId,
    int RowCount,
    double TimeMs,
    double Tps);
