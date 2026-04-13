using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Data;

public static class DbContextOptionsFactory
{
    public static void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration, DbmsProvider provider)
    {
        switch (provider)
        {
            case DbmsProvider.SqlServer:
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("SqlServer"));
                break;
            case DbmsProvider.PostgreSql:
                optionsBuilder.UseNpgsql(configuration.GetConnectionString("PostgreSql"));
                break;
            case DbmsProvider.MySql:
                var mySqlConnection = configuration.GetConnectionString("MySql");
                optionsBuilder.UseMySql(mySqlConnection, ServerVersion.AutoDetect(mySqlConnection));
                break;
            case DbmsProvider.Sqlite:
                optionsBuilder.UseSqlite(configuration.GetConnectionString("Sqlite"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider.");
        }
    }

    public static bool TryParse(string? value, out DbmsProvider provider)
    {
        provider = DbmsProvider.SqlServer;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "sqlserver" => Set(out provider, DbmsProvider.SqlServer),
            "postgres" or "postgresql" => Set(out provider, DbmsProvider.PostgreSql),
            "mysql" => Set(out provider, DbmsProvider.MySql),
            "sqlite" => Set(out provider, DbmsProvider.Sqlite),
            _ => false
        };
    }

    private static bool Set(out DbmsProvider current, DbmsProvider parsed)
    {
        current = parsed;
        return true;
    }
}
