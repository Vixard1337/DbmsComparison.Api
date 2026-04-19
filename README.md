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

## Running Databases with Docker

Start database containers:

`docker compose up -d`

Stop database containers:

`docker compose down`

Check running containers:

`docker compose ps`

The default setup is defined in `docker-compose.yml` and includes `SQL Server`, `PostgreSQL`, `MySQL`, and `SQLite`.

## Database Connectivity Endpoint

The API includes a connectivity endpoint for containerized databases:

`GET /api/database/test?db=sqlserver`

Quick provider list endpoint:

`GET /api/database/providers`

Supported values for `db`:

- `sqlserver`
- `postgres`
- `mysql`
- `sqlite`

This endpoint returns provider info and whether the API can connect to the selected DBMS.

## EF Core Tooling and Migration Strategy

This repository uses a local tool manifest with `dotnet-ef` and `Microsoft.EntityFrameworkCore.Design` for migration workflows.

Run EF Core tooling via:

`dotnet dotnet-ef`

Planned provider-specific migration layout:

- `Migrations/SqlServer`
- `Migrations/PostgreSql`
- `Migrations/MySql`
- `Migrations/Sqlite`

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

## Experiment Protocol

- Use the same hardware and `.NET` runtime version for all test runs.
- Keep one unified data model across all DBMS providers.
- Warm-up: 2 preliminary runs before measurement.
- Measurement repetitions:
  - `S1`: `N = 5`
  - `S2`: `N = 5`
  - `S3`: `N = 10`
- Use identical input datasets for each DBMS.
- Keep containers running under the same Docker configuration.
- Acceptance criterion for repeatability: standard deviation `<= 10%`.

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
