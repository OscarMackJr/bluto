# Technology Stack

**Analysis Date:** 2026-10-24

## Repository Shape (read first)

The project root `C:\Users\OMack\bluto` is **not** a git repository. It holds a governed documentation baseline ("Bluto Enterprise Starter Kit v2.0"): `00-baseline/`, `docs/`, `specs/`, `prompts/`, `repository/`, `releases/`, `contracts/`, `README.md`, `RELEASE-NOTES.md`, `SHA256SUMS.txt`, `release-manifest.json`. None of it is executable code.

All executable code lives in the nested git checkout **`publish-bluto/`**:
- Remote: `https://github.com/OscarMackJr/bluto.git`
- Checked-out branch: `implementation/bootstrap` (remote also has `master`, `feature/wp-006-identity-resolution-implementation`, and a Dependabot branch)
- Code root: `implementation/`
- `publish-bluto/` also carries its own copy of `00-baseline/`, `docs/`, `contracts/`, `repository/`. That copy is newer than the top-level one: it contains the ADRs in `docs/01-foundation-governance/adrs/ADR-0001..0017`, and the top-level `docs/` does not.

All paths below are relative to the repository root (`publish-bluto/`).

## Languages

**Primary:**
- C# (`LangVersion` = `latest`, targeting .NET 10) - all application, worker, persistence, validation, and test code in `implementation/src/` and `implementation/tests/`

**Secondary:**
- SQL (PostgreSQL dialect) - Flyway migrations in `implementation/db/migrations/` (`V001__identity_resolution_minimal_slice.sql`, `V002__wp013_privacy_tenant_lifecycle.sql`) and callback `implementation/db/callbacks/afterConnect.sql`
- JavaScript (ES modules, Node) - contract validation shim `implementation/tools/contracts/bin/ajv-validate.mjs`
- HCL (Terraform) - `implementation/infra/terraform/*.tf`
- PowerShell - `scripts/windows/Invoke-BlutoFlyway.ps1`
- YAML / JSON - OpenAPI (`docs/04-contracts/schemas/bluto-v1.openapi.yaml`, OpenAPI 3.0.3), JSON Schema Draft 2020-12 (`implementation/contracts/schemas/*.schema.json`, `docs/04-contracts/schemas/*.schema.json`), governance registries (`repository/**/*.yaml|json`)
- Markdown - controlled documents are normative (`docs/`, `00-baseline/`, `specs/`)

## Runtime

**Environment:**
- .NET 10 (`net10.0`), set once in `Directory.Build.props`
- SDK pinned in `global.json`: `10.0.204`, `rollForward: latestFeature`
- CI uses `actions/setup-dotnet@v6` with `dotnet-version: 10.0.x`
- Node.js (version not pinned, no `.nvmrc`). Used only for contract tooling in `implementation/tools/contracts/`
- Python (version not pinned). Used in CI only, through `python -m json.tool` for JSON well-formedness checks

**Package Manager:**
- NuGet with **Central Package Management** (`ManagePackageVersionsCentrally=true` in `Directory.Packages.props`). Put package versions only in `Directory.Packages.props`; `<PackageReference>` entries in `.csproj` files must have no `Version` attribute.
- No `packages.lock.json` files. NuGet restore is not lockfile-pinned.
- npm for `implementation/tools/contracts/`. Lockfile `package-lock.json` is present (lockfileVersion 3, about 245 packages). `node_modules/` is git-ignored.

## Frameworks

**Core:**
- ASP.NET Core 10 Minimal APIs (`Microsoft.NET.Sdk.Web`) - HTTP API in `implementation/src/Bluto.Api/` (`Program.cs`, `MappingApi.cs`). Endpoints are registered through extension methods (`AddBlutoMappingApi`, `MapBlutoMappingApi`).
- Microsoft.Extensions.DependencyInjection (built in) - no third-party container (ADR-0003)
- ASP.NET Core Authorization - policy `resolve-current-party` (`RequireAuthenticatedUser`) in `implementation/src/Bluto.Api/MappingApi.cs`
- `System.Diagnostics.ActivitySource` / `System.Diagnostics.Metrics.Meter` - tracing and metrics primitives in `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs` and `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs`
- `System.Text.Json` - all serialization. JSON field names are snake_case, set with `[JsonPropertyName]`.

**Testing:**
- xUnit.net v3 `3.2.0` (`xunit.v3`) with `xunit.runner.visualstudio` `3.1.5` and `Microsoft.NET.Test.Sdk` `18.0.0`
- Testcontainers.PostgreSql `4.8.0` - live PostgreSQL tests using image `postgres:16-alpine` (`implementation/tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs`)
- Microsoft.AspNetCore.Mvc.Testing `10.0.0` - version declared centrally, but no project references it. `Bluto.Api.Tests` uses `FrameworkReference Microsoft.AspNetCore.App` instead.
- JsonSchema.Net `7.4.0` - schema validation in `tests/Contracts` and `tests/Smoke`
- FsCheck (ADR-0009, property-based testing) - **not installed**

**Build/Dev:**
- .NET SDK OCI container publishing (`EnableSdkContainerSupport`) - no Dockerfiles. Configured in the `.csproj` files:
  - `Bluto.Api` -> repository `bluto-api`, base `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`, user `64198`
  - `Bluto.Worker` -> repository `bluto-worker`, base `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra`, user `64198`
- Roslyn analyzers plus `dotnet format --verify-no-changes`. `Directory.Build.props` sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `GenerateDocumentationFile=true`. Each project adds `NoWarn CS1591`.
- `.editorconfig` - CRLF line endings, 2-space indent (4 for `*.cs`), `IDE0005` set to warning, `CA1303` and `CA1812` disabled
- Flyway `13.1.0` (local executable, default path `C:\tools\flyway-13.1.0\flyway.cmd`) - schema migrations (ADR-0005). Config in `implementation/tools/flyway/flyway.local.conf`.
- Terraform `1.14.6` in CI, `required_version >= 1.6.0` (`implementation/infra/terraform/versions.tf`)
- Spectral CLI `^6.15.0` - OpenAPI lint with the custom ruleset `implementation/tools/contracts/.spectral.yaml`
- Ajv `^8.17.1` + ajv-formats `^3.0.1` - independent JSON Schema 2020-12 validation (`npm run validate:*`)

## Key Dependencies

All NuGet versions are in `Directory.Packages.props`.

**Critical:**
- Npgsql `10.0.0` - PostgreSQL driver (`NpgsqlDataSource`). Used in `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`.
- Dapper `2.1.66` - result mapping over explicit SQL. EF Core is not permitted (ADR-0004).
- OpenTelemetry.Api `1.15.3` - referenced by `Bluto.Api` and `Bluto.Worker`

**Infrastructure:**
- OpenTelemetry.Extensions.Hosting `1.15.3` - version declared centrally but **not referenced by any project**. No OTLP exporter is wired.
- Testcontainers.PostgreSql `4.8.0` - throwaway PostgreSQL for integration tests (Docker is required locally and in CI)
- JsonSchema.Net `7.4.0` - runtime and test schema validation

**Project graph** (`implementation/Bluto.Validation.sln`):
- `Bluto.Domain` (no references)
- `Bluto.Application` -> `Bluto.Domain`
- `Bluto.Infrastructure.Postgres` -> `Bluto.Application`, `Bluto.Domain`, Dapper, Npgsql
- `Bluto.Api` (Web SDK) -> `Bluto.Application`, OpenTelemetry.Api
- `Bluto.Worker` -> `Bluto.Application`, OpenTelemetry.Api. It is a **class library**: it has no `OutputType=Exe` and no `Main`/hosted service, even though it is configured for container publishing.
- `Bluto.Validation.Cli` (Exe) - standalone repository-governance validator, with no project references
- Tests: `tests/Api`, `tests/Bluto.Identity.Tests`, `tests/Bluto.Persistence.Tests`, `tests/Bluto.Validation.Tests`, `tests/Contracts`, `tests/Outbox`, `tests/Smoke`, `tests/Worker`

## Configuration

**Environment:**
- The API uses only the default `WebApplication.CreateBuilder(args)` configuration. The repo has no `appsettings*.json` and binds no options classes.
- Runtime configuration is defined as a JSON contract, not as .NET config:
  - Schema: `implementation/deploy/configuration/bluto-runtime-config.schema.json` (Draft 2020-12, `environment` const `nonproduction`)
  - Sample: `implementation/deploy/configuration/nonproduction.sample.json`
  - Secrets are written only as `kvref://<vault>/<name>` Key Vault references (the schema enforces this with a pattern). Never put literal secrets or the HMAC tokenization key in config. `tests/Smoke/RuntimeConfigurationContractTests.cs` asserts this (REQ-ID-11).
- Flyway connection comes from environment variables `BLUTO_FLYWAY_URL`, `BLUTO_FLYWAY_USER`, `BLUTO_FLYWAY_PASSWORD` (or script parameters) in `scripts/windows/Invoke-BlutoFlyway.ps1`. `flyway.local.conf` intentionally contains no URL or credentials.
- No `.env` files present.

**Build:**
- `global.json` - SDK pin
- `Directory.Build.props` - shared compiler and analyzer settings
- `Directory.Packages.props` - central package versions
- `.editorconfig`, `.gitattributes` (`*.cs text eol=crlf`)
- `implementation/tools/contracts/package.json`, `.spectral.yaml`
- `implementation/tools/flyway/flyway.local.conf` - managed schemas `flyway_history`, `identity_resolution`, `integration_outbox`

## Platform Requirements

**Development:**
- .NET SDK 10.0.204 or a later 10.0 feature band
- Docker, for Testcontainers persistence tests
- Node.js and npm, for Spectral/Ajv contract tooling (`npm ci` in `implementation/tools/contracts/`)
- Flyway 13.1.0 CLI plus a reachable PostgreSQL (the script's example uses `jdbc:postgresql://localhost:15432/bluto`)
- Terraform >= 1.6.0 (CI uses 1.14.6)
- The primary workstation is Windows (PowerShell script, CRLF line endings). CI runs on `ubuntu-latest`.
- Build and test entry point: `dotnet test implementation/Bluto.Validation.sln`

**Production:**
- Target: Azure, nonproduction only at present (ADR-0015, `implementation/infra/terraform/variables.tf` allows only `environment = "nonproduction"`, default region `eastus2`)
  - Azure Container Apps (API app plus worker container job), Azure Container Registry, Azure Database for PostgreSQL Flexible Server (public access must be `false`), Azure Key Vault, Log Analytics workspace, private endpoints
- The Terraform has **no providers or resources**. It only emits a `deployment_contract` output (`outputs.tf`) listing the expected resources and naming conventions (`bluto-<env>-*`).
- Linux x64 chiseled .NET 10 container images, run as a non-root UID

---

*Stack analysis: 2026-10-24*
