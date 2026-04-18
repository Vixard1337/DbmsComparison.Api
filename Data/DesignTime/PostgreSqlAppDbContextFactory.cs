using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbmsComparison.Api.Data.DesignTime;

public class PostgreSqlAppDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlAppDbContext>
{
    public PostgreSqlAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlAppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=benchmarkdb;Username=postgres;Password=postgres");

        return new PostgreSqlAppDbContext(optionsBuilder.Options);
    }
}
