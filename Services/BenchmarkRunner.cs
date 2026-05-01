using System.Diagnostics;
using System.Globalization;
using DbmsComparison.Api.Data;
using DbmsComparison.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Services;

public class BenchmarkRunner
{
    public async Task<BenchmarkResult> RunAsync(AppDbContext context, BenchmarkScenario scenario, int? overrideCount, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        var totalCount = GetScenarioCount(scenario, overrideCount);
        var process = Process.GetCurrentProcess();
        var cpuStart = process.TotalProcessorTime;
        var memStart = process.WorkingSet64;
        var stopwatch = Stopwatch.StartNew();

        var users = Enumerable.Range(1, totalCount)
            .Select(index => new User
            {
                Name = $"User {index}",
                Email = $"user-{runId:N}-{index}@example.com",
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        var products = Enumerable.Range(1, totalCount)
            .Select(index => new Product
            {
                Name = $"Product {index}",
                Price = 10m + index,
                Metadata = "{\"category\":\"benchmark\",\"source\":\"runner\"}"
            })
            .ToList();

        var orderItemsPerOrder = Math.Clamp(totalCount / 10, 1, 5);
        var ordersToCreate = Math.Clamp(totalCount / 10, 1, totalCount);

        context.Users.AddRange(users);
        context.Products.AddRange(products);
        await context.SaveChangesAsync(cancellationToken);

        var orders = new List<Order>(ordersToCreate);
        for (var i = 0; i < ordersToCreate; i++)
        {
            var user = users[i % users.Count];
            var orderItems = new List<OrderItem>(orderItemsPerOrder);
            decimal totalAmount = 0m;

            for (var j = 0; j < orderItemsPerOrder; j++)
            {
                var product = products[(i + j) % products.Count];
                var quantity = (j % 3) + 1;
                totalAmount += product.Price * quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }

            orders.Add(new Order
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(5 + (i % 20)),
                TotalAmount = totalAmount,
                OrderItems = orderItems
            });
        }

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync(cancellationToken);

        var readUsers = await context.Users
            .AsNoTracking()
            .Where(x => x.Email.StartsWith($"user-{runId:N}"))
            .OrderBy(x => x.Id)
            .Take(totalCount)
            .ToListAsync(cancellationToken);

        var readProducts = await context.Products
            .AsNoTracking()
            .Where(x => x.Metadata.Contains("\"benchmark\""))
            .OrderBy(x => x.Id)
            .Take(totalCount)
            .ToListAsync(cancellationToken);

        var readOrders = await context.Orders
            .AsNoTracking()
            .Include(x => x.OrderItems)
            .Where(x => x.CreatedAt >= orders.Min(o => o.CreatedAt))
            .OrderBy(x => x.Id)
            .Take(ordersToCreate)
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.Name = $"{user.Name} Updated";
        }

        foreach (var product in products)
        {
            product.Price += 1m;
        }

        foreach (var order in orders)
        {
            order.Duration = order.Duration.Add(TimeSpan.FromMinutes(1));
            order.TotalAmount = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
        }

        await context.SaveChangesAsync(cancellationToken);

        context.OrderItems.RemoveRange(orders.SelectMany(o => o.OrderItems));
        context.Orders.RemoveRange(orders);
        context.Users.RemoveRange(users);
        context.Products.RemoveRange(products);
        await context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        process.Refresh();

        var cpuMs = (process.TotalProcessorTime - cpuStart).TotalMilliseconds;
        var ramMb = Math.Max(0, process.WorkingSet64 - memStart) / 1024d / 1024d;
        var timeMs = stopwatch.Elapsed.TotalMilliseconds;
        var totalOps = (totalCount * 8) + (ordersToCreate * 4);
        var tps = timeMs > 0 ? totalOps / (timeMs / 1000d) : 0d;

        return new BenchmarkResult(
            runId,
            scenario,
            totalCount,
            timeMs,
            cpuMs,
            ramMb,
            tps,
            readUsers.Count + readProducts.Count + readOrders.Count);
    }

    public async Task WriteResultAsync(BenchmarkResult result, string db, string providerName, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "results");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, "benchmark-results.csv");
        var isNewFile = !File.Exists(filePath);

        await using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);

        if (isNewFile)
        {
            await writer.WriteLineAsync("run_id,dbms,provider,scenario,rows,time_ms,cpu_ms,ram_mb,tps");
        }

        var line = string.Join(",",
            result.RunId,
            db,
            providerName,
            result.Scenario,
            result.RowCount.ToString(CultureInfo.InvariantCulture),
            result.TimeMs.ToString("F2", CultureInfo.InvariantCulture),
            result.CpuMs.ToString("F2", CultureInfo.InvariantCulture),
            result.RamMb.ToString("F2", CultureInfo.InvariantCulture),
            result.Tps.ToString("F2", CultureInfo.InvariantCulture));

        await writer.WriteLineAsync(line);
    }

    private static int GetScenarioCount(BenchmarkScenario scenario, int? overrideCount)
    {
        if (overrideCount.HasValue && overrideCount.Value > 0)
        {
            return overrideCount.Value;
        }

        return scenario switch
        {
            BenchmarkScenario.S1 => 1000,
            BenchmarkScenario.S2 => 10000,
            BenchmarkScenario.S3 => 2000,
            _ => 1000
        };
    }
}

public enum BenchmarkScenario
{
    S1,
    S2,
    S3
}

public sealed record BenchmarkResult(
    Guid RunId,
    BenchmarkScenario Scenario,
    int RowCount,
    double TimeMs,
    double CpuMs,
    double RamMb,
    double Tps,
    int ReadCount);
