# DbmsComparison.Api

Comparative analysis platform for relational DBMS performance and implementation cost using a unified `ASP.NET Core (.NET 10)` Web API and `Entity Framework Core` model.

## Thesis Context

This project benchmarks four relational databases under controlled conditions:

- `Microsoft SQL Server`
- `PostgreSQL`
- `MySQL` (Pomelo provider)
- `SQLite`

The objective is to compare:

- **Usage cost**: latency, throughput (TPS), CPU, RAM
- **Implementation cost**: integration effort, complexity, and provider-specific issues

## Tech Stack

- `ASP.NET Core Web API` (`.NET 10`)
- `Entity Framework Core`
- Optional comparison layer: `Dapper`
- `Docker Compose` for DB environments

## Planned Data Model (Unified Across DBMS)

Main entities:

- `Users`
- `Orders`
- `OrderItems`
- `Products`
- `Categories` (self-reference hierarchy)
- `ProductCategories` (many-to-many)
- `Locations`

Model requirements include:

- Temporal types (`DateTime`, `TimeSpan`)
- Binary data (`byte[]`)
- JSON metadata
- Geospatial data (or fallback where unsupported)
- Multi-level relationships (`1:N`, `N:N`, hierarchical)

## Benchmark Scenarios

- **S1 (Low load)**: 10,000 records, 1 user
- **S2 (Medium/High load)**: 100,000 records, multiple users
- **S3 (Stability)**: repeated runs for variance analysis

## Metrics

- `time_ms`
- `tps`
- `cpu`
- `ram`

Results format (CSV):

`run_id,dbms,scenario,time_ms,cpu,ram,tps`

## Repository Status

Initial project setup. Full multi-database implementation, migrations, benchmarking, and analysis modules are in progress.

## License

For academic use.