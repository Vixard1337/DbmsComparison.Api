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

Seed sample test data:

`POST /api/database/seed?db=sqlserver`

Check seeded data counts:

`GET /api/database/seed/status?db=sqlserver`

Quick provider list endpoint:

`GET /api/database/providers`

Supported values for `db`:

- `sqlserver`
- `postgres`
- `mysql`
- `sqlite`

This endpoint returns provider info and whether the API can connect to the selected DBMS.

Implementation cost summary endpoint:

`GET /api/database/implementation-cost`

## Quick Start

1. Start databases:

`docker compose up -d`

2. Run the API:

`dotnet run`

3. Open Swagger UI:

`https://localhost:7289/swagger`

4. Seed sample data:

`POST /api/database/seed?db=sqlserver`

5. Run a benchmark:

`POST /api/benchmark/run?db=sqlserver&scenario=S1`

## Tests

Run integration tests:

`dotnet test`

Ensure database containers are running (see Docker section). Integration tests skip connectivity assertions when a provider is unavailable.

## Swagger UI

Swagger UI (NSwag) is available in development mode at:

`https://localhost:7289/swagger`

Benchmark runner endpoint:

`POST /api/benchmark/run?db=sqlserver&scenario=S1`

Optional override rows parameter:

`POST /api/benchmark/run?db=sqlserver&scenario=S1&rows=1000`

Batch runner for S1/S2/S3 with repetitions:

`POST /api/benchmark/run-all?db=sqlserver&repetitions=5`

Report summary endpoint:

`GET /api/report/summary`

Report plots endpoint:

`GET /api/report/plots`

Report PDF endpoint:

`GET /api/report/pdf`

Report archive (ZIP) endpoint:

`GET /api/report/archive`

Report file download endpoint:

`GET /api/report/files/{fileName}`

Combined benchmark results:

`results/benchmark-results-all.csv`

Generate diagrams (Mermaid):

`POST /api/report/diagrams`

If Mermaid CLI is installed (`mmdc` on PATH), PNG files are generated alongside `.mmd`.

Benchmark v1 runs CRUD for `Users`, `Products`, and `Orders` (with `OrderItems`).

Basic `Users` CRUD endpoints are available:

- `GET /api/users?db=sqlserver`
- `GET /api/users/{id}?db=sqlserver`
- `POST /api/users?db=sqlserver`
- `PUT /api/users/{id}?db=sqlserver`
- `DELETE /api/users/{id}?db=sqlserver`

Basic `Products` CRUD endpoints are available:

- `GET /api/products?db=sqlserver`
- `GET /api/products/{id}?db=sqlserver`
- `POST /api/products?db=sqlserver`
- `PUT /api/products/{id}?db=sqlserver`
- `DELETE /api/products/{id}?db=sqlserver`

Product-category assignment endpoints:

- `POST /api/products/{productId}/categories/{categoryId}?db=sqlserver`
- `DELETE /api/products/{productId}/categories/{categoryId}?db=sqlserver`

Basic `Orders` endpoints are available:

- `GET /api/orders?db=sqlserver`
- `GET /api/orders/{id}?db=sqlserver`
- `POST /api/orders?db=sqlserver`
- `PUT /api/orders/{id}?db=sqlserver`
- `DELETE /api/orders/{id}?db=sqlserver`

`POST /api/orders` validates:

- `UserId` exists
- every `ProductId` exists
- each `Quantity > 0`
- `TotalAmount` is calculated from `OrderItems` (server-side)

Basic `Categories` endpoints are available:

- `GET /api/categories?db=sqlserver`
- `POST /api/categories?db=sqlserver`

`POST /api/categories` supports hierarchy via optional `ParentCategoryId`.

## EF Core Tooling and Migration Strategy

This repository uses a local tool manifest with `dotnet-ef` and `Microsoft.EntityFrameworkCore.Design` for migration workflows.

Run EF Core tooling via:

`dotnet dotnet-ef`

Planned provider-specific migration layout:

- `Migrations/SqlServer`
- `Migrations/PostgreSql`
- `Migrations/MySql`
- `Migrations/Sqlite`

## Data Model (Unified Across DBMS)

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
- **S3 (Stability)**: 10,000 records, repeated runs for variance analysis

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

Benchmark metrics captured per run:

- `time_ms`
- `tps`
- `cpu_ms`
- `ram_mb`
- `peak_ram_mb`
- `read_ops`
- `create_ops`
- `update_ops`
- `delete_ops`
- `gc_gen0`, `gc_gen1`, `gc_gen2`

Results format (CSV):

`run_id,dbms,provider,scenario,rows,time_ms,cpu_ms,ram_mb,peak_ram_mb,tps,read_ops,create_ops,update_ops,delete_ops,gc_gen0,gc_gen1,gc_gen2`

Implementation cost metrics (per provider):

- configured connection string name
- JSON column type
- spatial column type / WKT fallback
- migration count

Each run is stored in:

`results/benchmark-results-{db}-{scenario}.csv`

## Analysis Script

Analyze benchmark results and generate summary statistics + plots:

`dotnet run --project DbmsComparison.Api.Reporter -- --input results/benchmark-results.csv`

Run analysis with plots:

`dotnet run --project DbmsComparison.Api.Reporter -- --input results/benchmark-results.csv --plots analysis/plots`

## Repository Status

Multi-database implementation, migrations, benchmarking, and analysis modules are included.

## License

For academic use.
