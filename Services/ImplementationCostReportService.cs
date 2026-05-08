using DbmsComparison.Api.Data;

namespace DbmsComparison.Api.Services;

public class ImplementationCostReportService(IConfiguration configuration, IHostEnvironment environment)
{
    public ImplementationCostReport BuildReport()
    {
        var providers = new[]
        {
            BuildProvider("sqlserver", DbmsProvider.SqlServer, "SqlServer", "nvarchar(max)", "geography", true),
            BuildProvider("postgres", DbmsProvider.PostgreSql, "PostgreSql", "jsonb", "geography (point)", true),
            BuildProvider("mysql", DbmsProvider.MySql, "MySql", "json", "point", true),
            BuildProvider("sqlite", DbmsProvider.Sqlite, "Sqlite", "TEXT", "WKT", false)
        };

        return new ImplementationCostReport(
            configuration["Database:DefaultProvider"] ?? "sqlserver",
            providers);
    }

    private ImplementationCostProviderReport BuildProvider(
        string key,
        DbmsProvider provider,
        string migrationsFolder,
        string jsonType,
        string spatialType,
        bool supportsSpatial)
    {
        var connectionName = provider switch
        {
            DbmsProvider.SqlServer => "SqlServer",
            DbmsProvider.PostgreSql => "PostgreSql",
            DbmsProvider.MySql => "MySql",
            DbmsProvider.Sqlite => "Sqlite",
            _ => "SqlServer"
        };

        var connectionString = configuration.GetConnectionString(connectionName);
        var migrationsPath = Path.Combine(environment.ContentRootPath, "Migrations", migrationsFolder);
        var migrationCount = Directory.Exists(migrationsPath)
            ? Directory.EnumerateFiles(migrationsPath, "*.cs")
                .Count(file => !file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            : 0;

        return new ImplementationCostProviderReport(
            key,
            connectionName,
            !string.IsNullOrWhiteSpace(connectionString),
            jsonType,
            spatialType,
            supportsSpatial ? "native" : "wkt",
            migrationCount);
    }
}

public sealed record ImplementationCostReport(
    string DefaultProvider,
    IReadOnlyList<ImplementationCostProviderReport> Providers);

public sealed record ImplementationCostProviderReport(
    string Key,
    string ConnectionStringName,
    bool ConnectionStringConfigured,
    string JsonColumnType,
    string SpatialColumnType,
    string SpatialSupportMode,
    int MigrationCount);
