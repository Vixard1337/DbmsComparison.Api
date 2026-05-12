using System.Globalization;
using ScottPlot;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace DbmsComparison.Api.Services;

public class ReportService(ImplementationCostReportService implementationCostReportService)
{
    public ReportSummary BuildSummary()
    {
        var resultsDirectory = GetResultsDirectory();
        var rows = LoadRows(resultsDirectory);
        var summaries = UsageSummaryRow.Build(rows);
        var usageComparisonPath = Path.Combine(resultsDirectory, "usage-comparison.csv");
        var summaryPath = Path.Combine(resultsDirectory, "usage-summary.csv");
        UsageSummaryRow.Write(summaryPath, summaries);
        UsageSummaryRow.WriteComparison(usageComparisonPath, summaries);

        var implementationReport = implementationCostReportService.BuildReport();
        var implementationPath = Path.Combine(resultsDirectory, "implementation-comparison.csv");
        ImplementationComparisonRow.Write(implementationPath, implementationReport);

        return new ReportSummary(resultsDirectory, summaryPath, usageComparisonPath, implementationPath, summaries);
    }

    public IReadOnlyList<string> GeneratePlots()
    {
        var resultsDirectory = GetResultsDirectory();
        var rows = LoadRows(resultsDirectory);
        var plotsDirectory = Path.Combine(resultsDirectory, "plots");
        var plotFiles = PlotWriter.Write(plotsDirectory, rows);
        return plotFiles;
    }

    public string GeneratePdfReport()
    {
        var summary = BuildSummary();
        var implementationReport = implementationCostReportService.BuildReport();
        var outputPath = Path.Combine(summary.ResultsDirectory, "report-summary.pdf");

        using var document = new PdfDocument();
        var titleFont = new XFont("Arial", 14, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 11, XFontStyle.Bold);
        var textFont = new XFont("Arial", 10, XFontStyle.Regular);
        const double margin = 30;
        const double lineHeight = 14;

        PdfPage page = document.AddPage();
        XGraphics graphics = XGraphics.FromPdfPage(page);
        double y = margin;

        void EnsureSpace()
        {
            if (y <= page.Height - margin - lineHeight)
            {
                return;
            }

            page = document.AddPage();
            graphics = XGraphics.FromPdfPage(page);
            y = margin;
        }

        void WriteLine(string text, XFont font)
        {
            EnsureSpace();
            graphics.DrawString(text, font, XBrushes.Black, new XRect(margin, y, page.Width - margin * 2, lineHeight), XStringFormats.TopLeft);
            y += lineHeight;
        }

        WriteLine("DbmsComparison report summary", titleFont);
        y += lineHeight / 2;
        WriteLine("Usage cost summary (mean values)", headerFont);

        foreach (var row in summary.Summaries)
        {
            WriteLine(
                $"{row.Dbms} {row.Scenario} | time {row.TimeMean:F2} ms | tps {row.TpsMean:F2} | cpu {row.CpuMean:F2} ms | ram {row.RamMean:F2} MB | peak {row.PeakRamMean:F2} MB",
                textFont);
        }

        y += lineHeight / 2;
        WriteLine("Implementation cost summary", headerFont);

        foreach (var provider in implementationReport.Providers)
        {
            WriteLine(
                $"{provider.Key} | conn {provider.ConnectionStringName} | json {provider.JsonColumnType} | spatial {provider.SpatialColumnType} ({provider.SpatialSupportMode}) | migrations {provider.MigrationCount}",
                textFont);
        }

        document.Save(outputPath);
        return outputPath;
    }

    private string GetResultsDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "results");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static List<ResultRow> LoadRows(string resultsDirectory)
    {
        if (!Directory.Exists(resultsDirectory))
        {
            return [];
        }

        var files = Directory.EnumerateFiles(resultsDirectory, "benchmark-results-*.csv").ToList();
        var rows = new List<ResultRow>();
        foreach (var file in files)
        {
            rows.AddRange(ResultRow.Load(file));
        }

        return rows;
    }
}

public sealed record ReportSummary(
    string ResultsDirectory,
    string SummaryPath,
    string UsageComparisonPath,
    string ImplementationComparisonPath,
    IReadOnlyList<UsageSummaryRow> Summaries);

public sealed record ResultRow(
    string Dbms,
    string Provider,
    string Scenario,
    double TimeMs,
    double CpuMs,
    double RamMb,
    double PeakRamMb,
    double Tps)
{
    public static List<ResultRow> Load(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            return [];
        }

        var header = lines[0].Split(',');
        var index = header
            .Select((name, idx) => (name: name.Trim(), idx))
            .ToDictionary(x => x.name, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var rows = new List<ResultRow>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = line.Split(',');
            rows.Add(new ResultRow(
                GetText(cells, index, "dbms"),
                GetText(cells, index, "provider"),
                GetText(cells, index, "scenario"),
                GetNumber(cells, index, "time_ms"),
                GetNumber(cells, index, "cpu_ms"),
                GetNumber(cells, index, "ram_mb"),
                GetNumber(cells, index, "peak_ram_mb"),
                GetNumber(cells, index, "tps")));
        }

        return rows;
    }

    private static string GetText(string[] cells, Dictionary<string, int> index, string name)
    {
        return index.TryGetValue(name, out var idx) && idx < cells.Length
            ? cells[idx]
            : string.Empty;
    }

    private static double GetNumber(string[] cells, Dictionary<string, int> index, string name)
    {
        if (!index.TryGetValue(name, out var idx) || idx >= cells.Length)
        {
            return 0;
        }

        return double.TryParse(cells[idx], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }
}

public sealed record UsageSummaryRow(
    string Dbms,
    string Scenario,
    int Runs,
    double TimeMean,
    double TimeMedian,
    double TimeSd,
    double TimeIqr,
    double TpsMean,
    double TpsMedian,
    double TpsSd,
    double TpsIqr,
    double CpuMean,
    double CpuMedian,
    double CpuSd,
    double CpuIqr,
    double RamMean,
    double RamMedian,
    double RamSd,
    double RamIqr,
    double PeakRamMean,
    double PeakRamMedian,
    double PeakRamSd,
    double PeakRamIqr)
{
    public static List<UsageSummaryRow> Build(IEnumerable<ResultRow> rows)
    {
        return rows
            .GroupBy(row => new { row.Dbms, row.Scenario })
            .OrderBy(group => group.Key.Dbms)
            .ThenBy(group => group.Key.Scenario)
            .Select(group =>
            {
                var timeStats = Stats.From(group.Select(x => x.TimeMs));
                var tpsStats = Stats.From(group.Select(x => x.Tps));
                var cpuStats = Stats.From(group.Select(x => x.CpuMs));
                var ramStats = Stats.From(group.Select(x => x.RamMb));
                var peakStats = Stats.From(group.Select(x => x.PeakRamMb));

                return new UsageSummaryRow(
                    group.Key.Dbms,
                    group.Key.Scenario,
                    group.Count(),
                    timeStats.Mean,
                    timeStats.Median,
                    timeStats.Sd,
                    timeStats.Iqr,
                    tpsStats.Mean,
                    tpsStats.Median,
                    tpsStats.Sd,
                    tpsStats.Iqr,
                    cpuStats.Mean,
                    cpuStats.Median,
                    cpuStats.Sd,
                    cpuStats.Iqr,
                    ramStats.Mean,
                    ramStats.Median,
                    ramStats.Sd,
                    ramStats.Iqr,
                    peakStats.Mean,
                    peakStats.Median,
                    peakStats.Sd,
                    peakStats.Iqr);
            })
            .ToList();
    }

    public static void Write(string path, IReadOnlyCollection<UsageSummaryRow> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var writer = new StreamWriter(path, false);
        writer.WriteLine("dbms,scenario,runs,time_mean,time_median,time_sd,time_iqr,tps_mean,tps_median,tps_sd,tps_iqr,cpu_mean,cpu_median,cpu_sd,cpu_iqr,ram_mean,ram_median,ram_sd,ram_iqr,peak_ram_mean,peak_ram_median,peak_ram_sd,peak_ram_iqr");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
                row.Dbms,
                row.Scenario,
                row.Runs.ToString(CultureInfo.InvariantCulture),
                row.TimeMean.ToString("F2", CultureInfo.InvariantCulture),
                row.TimeMedian.ToString("F2", CultureInfo.InvariantCulture),
                row.TimeSd.ToString("F2", CultureInfo.InvariantCulture),
                row.TimeIqr.ToString("F2", CultureInfo.InvariantCulture),
                row.TpsMean.ToString("F2", CultureInfo.InvariantCulture),
                row.TpsMedian.ToString("F2", CultureInfo.InvariantCulture),
                row.TpsSd.ToString("F2", CultureInfo.InvariantCulture),
                row.TpsIqr.ToString("F2", CultureInfo.InvariantCulture),
                row.CpuMean.ToString("F2", CultureInfo.InvariantCulture),
                row.CpuMedian.ToString("F2", CultureInfo.InvariantCulture),
                row.CpuSd.ToString("F2", CultureInfo.InvariantCulture),
                row.CpuIqr.ToString("F2", CultureInfo.InvariantCulture),
                row.RamMean.ToString("F2", CultureInfo.InvariantCulture),
                row.RamMedian.ToString("F2", CultureInfo.InvariantCulture),
                row.RamSd.ToString("F2", CultureInfo.InvariantCulture),
                row.RamIqr.ToString("F2", CultureInfo.InvariantCulture),
                row.PeakRamMean.ToString("F2", CultureInfo.InvariantCulture),
                row.PeakRamMedian.ToString("F2", CultureInfo.InvariantCulture),
                row.PeakRamSd.ToString("F2", CultureInfo.InvariantCulture),
                row.PeakRamIqr.ToString("F2", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteComparison(string path, IReadOnlyCollection<UsageSummaryRow> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var writer = new StreamWriter(path, false);
        writer.WriteLine("dbms,scenario,time_mean,tps_mean,cpu_mean,ram_mean,peak_ram_mean");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
                row.Dbms,
                row.Scenario,
                row.TimeMean.ToString("F2", CultureInfo.InvariantCulture),
                row.TpsMean.ToString("F2", CultureInfo.InvariantCulture),
                row.CpuMean.ToString("F2", CultureInfo.InvariantCulture),
                row.RamMean.ToString("F2", CultureInfo.InvariantCulture),
                row.PeakRamMean.ToString("F2", CultureInfo.InvariantCulture)));
        }
    }
}

public sealed record ImplementationComparisonRow(
    string Key,
    string ConnectionStringName,
    bool ConnectionStringConfigured,
    string JsonColumnType,
    string SpatialColumnType,
    string SpatialSupportMode,
    int MigrationCount)
{
    public static void Write(string path, ImplementationCostReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var writer = new StreamWriter(path, false);
        writer.WriteLine("dbms,connection_string_name,connection_configured,json_type,spatial_type,spatial_mode,migration_count");

        foreach (var provider in report.Providers)
        {
            writer.WriteLine(string.Join(",",
                provider.Key,
                provider.ConnectionStringName,
                provider.ConnectionStringConfigured.ToString(CultureInfo.InvariantCulture),
                provider.JsonColumnType,
                provider.SpatialColumnType,
                provider.SpatialSupportMode,
                provider.MigrationCount.ToString(CultureInfo.InvariantCulture)));
        }
    }
}

public sealed record Stats(double Mean, double Median, double Sd, double Iqr)
{
    public static Stats From(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count == 0)
        {
            return new Stats(0, 0, 0, 0);
        }

        list.Sort();
        var mean = list.Average();
        var median = CalculateMedian(list);
        var sd = list.Count > 1
            ? Math.Sqrt(list.Sum(v => Math.Pow(v - mean, 2)) / list.Count)
            : 0;
        var q1 = CalculateMedian(list.Take(list.Count / 2).ToList());
        var q3 = CalculateMedian(list.Skip((list.Count + 1) / 2).ToList());
        var iqr = q3 - q1;

        return new Stats(mean, median, sd, iqr);
    }

    private static double CalculateMedian(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var mid = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[mid - 1] + values[mid]) / 2
            : values[mid];
    }
}

static class PlotWriter
{
    public static IReadOnlyList<string> Write(string directory, IReadOnlyList<ResultRow> rows)
    {
        Directory.CreateDirectory(directory);

        var scenarios = rows.Select(r => r.Scenario).Distinct().OrderBy(s => s).ToList();
        var dbmsValues = rows.Select(r => r.Dbms).Distinct().OrderBy(s => s).ToList();

        var metrics = new (string Name, Func<ResultRow, double> Selector)[]
        {
            ("time_ms", row => row.TimeMs),
            ("tps", row => row.Tps),
            ("cpu_ms", row => row.CpuMs),
            ("ram_mb", row => row.RamMb),
            ("peak_ram_mb", row => row.PeakRamMb)
        };

        var files = new List<string>();

        foreach (var scenario in scenarios)
        {
            foreach (var metric in metrics)
            {
                var means = dbmsValues
                    .Select(dbms => rows
                        .Where(r => r.Scenario == scenario && r.Dbms == dbms)
                        .Select(metric.Selector)
                        .DefaultIfEmpty()
                        .Average())
                    .ToArray();

                var plot = new Plot();
                var positions = Enumerable.Range(0, means.Length).Select(x => (double)x).ToArray();
                plot.Add.Bars(positions, means);

                plot.Title($"{metric.Name} mean - {scenario}");
                plot.YLabel(metric.Name);
                plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    positions,
                    dbmsValues.ToArray());
                plot.Axes.Bottom.MajorTickStyle.Length = 0;

                var filePath = Path.Combine(directory, $"{metric.Name}-{scenario}-mean.png");
                plot.SavePng(filePath, 1000, 600);
                files.Add(filePath);
            }
        }

        return files;
    }
}
