using System.Globalization;
using ScottPlot;

var arguments = Args.Parse(args);
if (!File.Exists(arguments.InputPath))
{
    Console.Error.WriteLine($"Input file not found: {arguments.InputPath}");
    return;
}

var rows = ResultRow.Load(arguments.InputPath);
if (rows.Count == 0)
{
    Console.Error.WriteLine("No rows found in input file.");
    return;
}

var summaries = SummaryRow.Build(rows);
SummaryRow.Write(arguments.OutputPath, summaries);

if (!string.IsNullOrWhiteSpace(arguments.PlotsDirectory))
{
    PlotWriter.Write(arguments.PlotsDirectory!, rows);
}

Console.WriteLine($"Summary written to {arguments.OutputPath}");
Console.WriteLine($"Generated {summaries.Count} summary rows at {DateTime.UtcNow:O}");

sealed record Args(string InputPath, string OutputPath, string? PlotsDirectory)
{
    public static Args Parse(string[] args)
    {
        var input = "results/benchmark-results.csv";
        var output = "analysis/summary.csv";
        string? plots = "analysis/plots";

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input" when i + 1 < args.Length:
                    input = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--plots" when i + 1 < args.Length:
                    plots = args[++i];
                    break;
                case "--no-plots":
                    plots = null;
                    break;
            }
        }

        return new Args(input, output, plots);
    }
}

sealed record ResultRow(
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

sealed record SummaryRow(
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
    public static List<SummaryRow> Build(IEnumerable<ResultRow> rows)
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

                return new SummaryRow(
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

    public static void Write(string path, IReadOnlyCollection<SummaryRow> rows)
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
}

sealed record Stats(double Mean, double Median, double Sd, double Iqr)
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
    public static void Write(string directory, IReadOnlyList<ResultRow> rows)
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
            }
        }
    }
}
