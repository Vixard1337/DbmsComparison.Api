import argparse
import csv
import os
import statistics
from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime

try:
    import matplotlib.pyplot as plt
except Exception:  # pragma: no cover - optional dependency
    plt = None


@dataclass
class ResultRow:
    dbms: str
    scenario: str
    time_ms: float
    cpu_ms: float
    ram_mb: float
    tps: float


def parse_results(path: str) -> list[ResultRow]:
    rows: list[ResultRow] = []
    with open(path, newline="", encoding="utf-8") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            rows.append(
                ResultRow(
                    dbms=row.get("dbms", "unknown"),
                    scenario=row.get("scenario", "unknown"),
                    time_ms=float(row.get("time_ms", 0) or 0),
                    cpu_ms=float(row.get("cpu_ms", 0) or 0),
                    ram_mb=float(row.get("ram_mb", 0) or 0),
                    tps=float(row.get("tps", 0) or 0),
                )
            )
    return rows


def summarize(values: list[float]) -> dict[str, float]:
    if not values:
        return {"mean": 0, "median": 0, "sd": 0, "iqr": 0}
    mean = statistics.fmean(values)
    median = statistics.median(values)
    sd = statistics.pstdev(values) if len(values) > 1 else 0.0
    sorted_vals = sorted(values)
    q1 = statistics.median(sorted_vals[: len(sorted_vals) // 2])
    q3 = statistics.median(sorted_vals[(len(sorted_vals) + 1) // 2 :])
    iqr = q3 - q1
    return {"mean": mean, "median": median, "sd": sd, "iqr": iqr}


def write_summary(path: str, summary_rows: list[dict[str, str]]) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=summary_rows[0].keys())
        writer.writeheader()
        writer.writerows(summary_rows)


def render_plots(rows: list[ResultRow], output_dir: str) -> None:
    if plt is None:
        print("matplotlib is not installed. Skipping plots.")
        return

    os.makedirs(output_dir, exist_ok=True)
    scenarios = sorted({r.scenario for r in rows})
    dbms_values = sorted({r.dbms for r in rows})

    for metric in ("time_ms", "tps"):
        for scenario in scenarios:
            grouped = []
            labels = []
            for dbms in dbms_values:
                values = [getattr(r, metric) for r in rows if r.dbms == dbms and r.scenario == scenario]
                if values:
                    grouped.append(values)
                    labels.append(dbms)

            if not grouped:
                continue

            plt.figure(figsize=(10, 6))
            plt.boxplot(grouped, labels=labels)
            plt.title(f"{metric} distribution - {scenario}")
            plt.ylabel(metric)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f"{metric}-{scenario}-boxplot.png"))
            plt.close()

            means = [statistics.fmean(values) for values in grouped]
            plt.figure(figsize=(10, 6))
            plt.bar(labels, means)
            plt.title(f"{metric} mean - {scenario}")
            plt.ylabel(metric)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f"{metric}-{scenario}-bar.png"))
            plt.close()


def main() -> None:
    parser = argparse.ArgumentParser(description="Analyze benchmark CSV results.")
    parser.add_argument("--input", default=os.path.join("results", "benchmark-results.csv"))
    parser.add_argument("--output", default=os.path.join("analysis", "summary.csv"))
    parser.add_argument("--plots", default=os.path.join("analysis", "plots"))
    args = parser.parse_args()

    if not os.path.exists(args.input):
        raise SystemExit(f"Input file not found: {args.input}")

    rows = parse_results(args.input)
    if not rows:
        raise SystemExit("No rows found in input file.")

    grouped: dict[tuple[str, str], list[ResultRow]] = defaultdict(list)
    for row in rows:
        grouped[(row.dbms, row.scenario)].append(row)

    summary_rows: list[dict[str, str]] = []
    for (dbms, scenario), bucket in sorted(grouped.items()):
        time_stats = summarize([r.time_ms for r in bucket])
        tps_stats = summarize([r.tps for r in bucket])
        cpu_stats = summarize([r.cpu_ms for r in bucket])
        ram_stats = summarize([r.ram_mb for r in bucket])

        summary_rows.append(
            {
                "dbms": dbms,
                "scenario": scenario,
                "runs": str(len(bucket)),
                "time_mean": f"{time_stats['mean']:.2f}",
                "time_median": f"{time_stats['median']:.2f}",
                "time_sd": f"{time_stats['sd']:.2f}",
                "time_iqr": f"{time_stats['iqr']:.2f}",
                "tps_mean": f"{tps_stats['mean']:.2f}",
                "tps_median": f"{tps_stats['median']:.2f}",
                "tps_sd": f"{tps_stats['sd']:.2f}",
                "tps_iqr": f"{tps_stats['iqr']:.2f}",
                "cpu_mean": f"{cpu_stats['mean']:.2f}",
                "cpu_median": f"{cpu_stats['median']:.2f}",
                "cpu_sd": f"{cpu_stats['sd']:.2f}",
                "cpu_iqr": f"{cpu_stats['iqr']:.2f}",
                "ram_mean": f"{ram_stats['mean']:.2f}",
                "ram_median": f"{ram_stats['median']:.2f}",
                "ram_sd": f"{ram_stats['sd']:.2f}",
                "ram_iqr": f"{ram_stats['iqr']:.2f}",
            }
        )

    write_summary(args.output, summary_rows)
    render_plots(rows, args.plots)

    print(f"Summary written to {args.output}")
    print(f"Generated {len(summary_rows)} summary rows at {datetime.utcnow().isoformat()}Z")


if __name__ == "__main__":
    main()
