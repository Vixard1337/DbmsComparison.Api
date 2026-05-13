namespace DbmsComparison.Api.Services;

public class DiagramService
{
    public IReadOnlyList<string> GenerateDiagrams()
    {
        var resultsDirectory = Path.Combine(AppContext.BaseDirectory, "results");
        var diagramsDirectory = Path.Combine(resultsDirectory, "diagrams");
        Directory.CreateDirectory(diagramsDirectory);

        var architecturePath = Path.Combine(diagramsDirectory, "architecture.mmd");
        var erdPath = Path.Combine(diagramsDirectory, "erd.mmd");

        File.WriteAllText(architecturePath, GetArchitectureDiagram());
        File.WriteAllText(erdPath, GetErdDiagram());

        return [architecturePath, erdPath];
    }

    private static string GetArchitectureDiagram()
    {
        return """
flowchart LR
    client[Client / Swagger] --> api[ASP.NET Core Web API]
    api --> ef[EF Core]
    ef --> sqlserver[(SQL Server)]
    ef --> postgres[(PostgreSQL)]
    ef --> mysql[(MySQL)]
    ef --> sqlite[(SQLite)]
    api --> bench[Benchmark Runner]
    bench --> results[(results/*.csv)]
    api --> reports[Report Service]
    reports --> results
""".Trim();
    }

    private static string GetErdDiagram()
    {
        return """
erDiagram
    USER ||--o{ ORDER : places
    ORDER ||--o{ ORDER_ITEM : contains
    PRODUCT ||--o{ ORDER_ITEM : includes
    CATEGORY ||--o{ CATEGORY : parent
    PRODUCT ||--o{ PRODUCT_CATEGORY : tags
    CATEGORY ||--o{ PRODUCT_CATEGORY : tags
    LOCATION
""".Trim();
    }
}
