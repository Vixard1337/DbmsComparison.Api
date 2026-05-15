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
        var architectureImage = Path.Combine(diagramsDirectory, "architecture.png");
        var erdImage = Path.Combine(diagramsDirectory, "erd.png");

        File.WriteAllText(architecturePath, GetArchitectureDiagram());
        File.WriteAllText(erdPath, GetErdDiagram());

        TryRenderMermaid(diagramsDirectory, architecturePath, architectureImage);
        TryRenderMermaid(diagramsDirectory, erdPath, erdImage);

        return [architecturePath, erdPath, architectureImage, erdImage];
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

    private static void TryRenderMermaid(string workingDirectory, string inputPath, string outputPath)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "mmdc",
                Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            process.WaitForExit(10000);
        }
        catch
        {
        }
    }
}
