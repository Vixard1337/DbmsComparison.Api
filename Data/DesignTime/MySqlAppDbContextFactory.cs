using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbmsComparison.Api.Data.DesignTime;

public class MySqlAppDbContextFactory : IDesignTimeDbContextFactory<MySqlAppDbContext>
{
    public MySqlAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MySqlAppDbContext>();
        const string connectionString = "Server=localhost;Port=3306;Database=benchmarkdb;User=root;Password=root";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new MySqlAppDbContext(optionsBuilder.Options);
    }
}
