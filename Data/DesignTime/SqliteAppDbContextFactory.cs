using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbmsComparison.Api.Data.DesignTime;

public class SqliteAppDbContextFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteAppDbContext>();
        optionsBuilder.UseSqlite("Data Source=benchmark.db");

        return new SqliteAppDbContext(optionsBuilder.Options);
    }
}
