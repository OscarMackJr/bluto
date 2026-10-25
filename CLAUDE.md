<!-- GSD:project-start source:PROJECT.md -->

## Project

**Bluto — WP-014 Closure and Runnable POC**

Bluto is the Fisk Road enterprise identity spine. It resolves entity (Party) identity across divisional systems ahead of time and records it, so cross-domain context has an auditable join. Identity is never inferred at query time. This GSD project takes the existing `implementation/bootstrap` codebase (WP-001 through WP-013 delivered) through WP-014 review closure and POC readiness certification. A new governed work package, WP-015, then turns the result into a POC that runs end to end on a developer machine.

**Core Value:** A developer can run `docker compose up` and watch a synthetic batch resolve into Parties in PostgreSQL. An authenticated, tenant-scoped API returns those mappings, and the resulting events are delivered to a consumer. None of the existing security, tenant-isolation, or governance controls are weakened on the way.

### Constraints

- **Governance**: Work packages execute in order. WP-014 changes are limited to its `allowed_files`, and WP-015 must be defined before its code. This is the repository's constitutional process, and the RVEC evidence depends on it.
- **Tech stack**: .NET 10 (SDK 10.0.204), PostgreSQL 16, Flyway, Dapper/Npgsql, xUnit v3 with Testcontainers. These are preserved decisions and the existing toolchain.
- **Security**: Fail-closed token-shape validation. No raw strong identifiers at rest or in logs. Forced RLS. Tenant-scoped idempotency. No key material or secrets in repo or config. These are WP-014 security controls and preserved decisions.
- **Migrations**: Forward-only, with no mutation of existing identity history. Rollback strategy is forward recovery.
- **Delivery**: Changes go through PRs into `implementation/bootstrap` (the branch has a PR rule). One PR per work package keeps evidence traceable to the commit SHA and WP ID.
- **Environment**: Developer machine is Windows. CRLF conversion affects checksum validation, so line endings must be handled deliberately.

<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->

## Technology Stack

## Repository Shape (read first)

- Remote: `https://github.com/OscarMackJr/bluto.git`
- Checked-out branch: `implementation/bootstrap` (remote also has `master`, `feature/wp-006-identity-resolution-implementation`, and a Dependabot branch)
- Code root: `implementation/`
- `publish-bluto/` also carries its own copy of `00-baseline/`, `docs/`, `contracts/`, `repository/`. That copy is newer than the top-level one: it contains the ADRs in `docs/01-foundation-governance/adrs/ADR-0001..0017`, and the top-level `docs/` does not.

## Languages

- C# (`LangVersion` = `latest`, targeting .NET 10) - all application, worker, persistence, validation, and test code in `implementation/src/` and `implementation/tests/`
- SQL (PostgreSQL dialect) - Flyway migrations in `implementation/db/migrations/` (`V001__identity_resolution_minimal_slice.sql`, `V002__wp013_privacy_tenant_lifecycle.sql`) and callback `implementation/db/callbacks/afterConnect.sql`
- JavaScript (ES modules, Node) - contract validation shim `implementation/tools/contracts/bin/ajv-validate.mjs`
- HCL (Terraform) - `implementation/infra/terraform/*.tf`
- PowerShell - `scripts/windows/Invoke-BlutoFlyway.ps1`
- YAML / JSON - OpenAPI (`docs/04-contracts/schemas/bluto-v1.openapi.yaml`, OpenAPI 3.0.3), JSON Schema Draft 2020-12 (`implementation/contracts/schemas/*.schema.json`, `docs/04-contracts/schemas/*.schema.json`), governance registries (`repository/**/*.yaml|json`)
- Markdown - controlled documents are normative (`docs/`, `00-baseline/`, `specs/`)

## Runtime

- .NET 10 (`net10.0`), set once in `Directory.Build.props`
- SDK pinned in `global.json`: `10.0.204`, `rollForward: latestFeature`
- CI uses `actions/setup-dotnet@v6` with `dotnet-version: 10.0.x`
- Node.js (version not pinned, no `.nvmrc`). Used only for contract tooling in `implementation/tools/contracts/`
- Python (version not pinned). Used in CI only, through `python -m json.tool` for JSON well-formedness checks
- NuGet with **Central Package Management** (`ManagePackageVersionsCentrally=true` in `Directory.Packages.props`). Put package versions only in `Directory.Packages.props`; `<PackageReference>` entries in `.csproj` files must have no `Version` attribute.
- No `packages.lock.json` files. NuGet restore is not lockfile-pinned.
- npm for `implementation/tools/contracts/`. Lockfile `package-lock.json` is present (lockfileVersion 3, about 245 packages). `node_modules/` is git-ignored.

## Frameworks

- ASP.NET Core 10 Minimal APIs (`Microsoft.NET.Sdk.Web`) - HTTP API in `implementation/src/Bluto.Api/` (`Program.cs`, `MappingApi.cs`). Endpoints are registered through extension methods (`AddBlutoMappingApi`, `MapBlutoMappingApi`).
- Microsoft.Extensions.DependencyInjection (built in) - no third-party container (ADR-0003)
- ASP.NET Core Authorization - policy `resolve-current-party` (`RequireAuthenticatedUser`) in `implementation/src/Bluto.Api/MappingApi.cs`
- `System.Diagnostics.ActivitySource` / `System.Diagnostics.Metrics.Meter` - tracing and metrics primitives in `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs` and `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs`
- `System.Text.Json` - all serialization. JSON field names are snake_case, set with `[JsonPropertyName]`.
- xUnit.net v3 `3.2.0` (`xunit.v3`) with `xunit.runner.visualstudio` `3.1.5` and `Microsoft.NET.Test.Sdk` `18.0.0`
- Testcontainers.PostgreSql `4.8.0` - live PostgreSQL tests using image `postgres:16-alpine` (`implementation/tests/Bluto.Persistence.Tests/PostgresPartyRepositoryLiveTests.cs`)
- Microsoft.AspNetCore.Mvc.Testing `10.0.0` - version declared centrally, but no project references it. `Bluto.Api.Tests` uses `FrameworkReference Microsoft.AspNetCore.App` instead.
- JsonSchema.Net `7.4.0` - schema validation in `tests/Contracts` and `tests/Smoke`
- FsCheck (ADR-0009, property-based testing) - **not installed**
- .NET SDK OCI container publishing (`EnableSdkContainerSupport`) - no Dockerfiles. Configured in the `.csproj` files:
- Roslyn analyzers plus `dotnet format --verify-no-changes`. `Directory.Build.props` sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `GenerateDocumentationFile=true`. Each project adds `NoWarn CS1591`.
- `.editorconfig` - CRLF line endings, 2-space indent (4 for `*.cs`), `IDE0005` set to warning, `CA1303` and `CA1812` disabled
- Flyway `13.1.0` (local executable, default path `C:\tools\flyway-13.1.0\flyway.cmd`) - schema migrations (ADR-0005). Config in `implementation/tools/flyway/flyway.local.conf`.
- Terraform `1.14.6` in CI, `required_version >= 1.6.0` (`implementation/infra/terraform/versions.tf`)
- Spectral CLI `^6.15.0` - OpenAPI lint with the custom ruleset `implementation/tools/contracts/.spectral.yaml`
- Ajv `^8.17.1` + ajv-formats `^3.0.1` - independent JSON Schema 2020-12 validation (`npm run validate:*`)

## Key Dependencies

- Npgsql `10.0.0` - PostgreSQL driver (`NpgsqlDataSource`). Used in `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`.
- Dapper `2.1.66` - result mapping over explicit SQL. EF Core is not permitted (ADR-0004).
- OpenTelemetry.Api `1.15.3` - referenced by `Bluto.Api` and `Bluto.Worker`
- OpenTelemetry.Extensions.Hosting `1.15.3` - version declared centrally but **not referenced by any project**. No OTLP exporter is wired.
- Testcontainers.PostgreSql `4.8.0` - throwaway PostgreSQL for integration tests (Docker is required locally and in CI)
- JsonSchema.Net `7.4.0` - runtime and test schema validation
- `Bluto.Domain` (no references)
- `Bluto.Application` -> `Bluto.Domain`
- `Bluto.Infrastructure.Postgres` -> `Bluto.Application`, `Bluto.Domain`, Dapper, Npgsql
- `Bluto.Api` (Web SDK) -> `Bluto.Application`, OpenTelemetry.Api
- `Bluto.Worker` -> `Bluto.Application`, OpenTelemetry.Api. It is a **class library**: it has no `OutputType=Exe` and no `Main`/hosted service, even though it is configured for container publishing.
- `Bluto.Validation.Cli` (Exe) - standalone repository-governance validator, with no project references
- Tests: `tests/Api`, `tests/Bluto.Identity.Tests`, `tests/Bluto.Persistence.Tests`, `tests/Bluto.Validation.Tests`, `tests/Contracts`, `tests/Outbox`, `tests/Smoke`, `tests/Worker`

## Configuration

- The API uses only the default `WebApplication.CreateBuilder(args)` configuration. The repo has no `appsettings*.json` and binds no options classes.
- Runtime configuration is defined as a JSON contract, not as .NET config:
- Flyway connection comes from environment variables `BLUTO_FLYWAY_URL`, `BLUTO_FLYWAY_USER`, `BLUTO_FLYWAY_PASSWORD` (or script parameters) in `scripts/windows/Invoke-BlutoFlyway.ps1`. `flyway.local.conf` intentionally contains no URL or credentials.
- No `.env` files present.
- `global.json` - SDK pin
- `Directory.Build.props` - shared compiler and analyzer settings
- `Directory.Packages.props` - central package versions
- `.editorconfig`, `.gitattributes` (`*.cs text eol=crlf`)
- `implementation/tools/contracts/package.json`, `.spectral.yaml`
- `implementation/tools/flyway/flyway.local.conf` - managed schemas `flyway_history`, `identity_resolution`, `integration_outbox`

## Platform Requirements

- .NET SDK 10.0.204 or a later 10.0 feature band
- Docker, for Testcontainers persistence tests
- Node.js and npm, for Spectral/Ajv contract tooling (`npm ci` in `implementation/tools/contracts/`)
- Flyway 13.1.0 CLI plus a reachable PostgreSQL (the script's example uses `jdbc:postgresql://localhost:15432/bluto`)
- Terraform >= 1.6.0 (CI uses 1.14.6)
- The primary workstation is Windows (PowerShell script, CRLF line endings). CI runs on `ubuntu-latest`.
- Build and test entry point: `dotnet test implementation/Bluto.Validation.sln`
- Target: Azure, nonproduction only at present (ADR-0015, `implementation/infra/terraform/variables.tf` allows only `environment = "nonproduction"`, default region `eastus2`)
- The Terraform has **no providers or resources**. It only emits a `deployment_contract` output (`outputs.tf`) listing the expected resources and naming conventions (`bluto-<env>-*`).
- Linux x64 chiseled .NET 10 container images, run as a non-root UID

<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

- `docs/06-engineering-standards/CODING-STANDARDS.md` - small single-purpose functions, side effects behind ports, no mutable global state, domain must not touch HTTP/SQL/clock/randomness/filesystem/env directly
- `docs/06-engineering-standards/ERROR-HANDLING.md` - typed error categories, translate adapter errors once, no catch-all suppression
- `docs/06-engineering-standards/NAMING-DOCUMENTATION.md` - Ubiquitous Language names are mandatory; source-specific terms stay in adapters
- `docs/06-engineering-standards/LOGGING-OBSERVABILITY.md` - stable event names; raw strong identifiers, HMAC tokens, source payloads, secrets prohibited in logs; source keys never metric labels

## Naming Patterns

- PascalCase `.cs` file named after the primary type or feature area: `src/Bluto.Application/Identity/IdentityResolutionService.cs`, `src/Bluto.Worker/Outbox/OutboxPublisher.cs`
- A file may group a feature's records, interfaces, and classes together (e.g. `src/Bluto.Application/Identity/IdentityResolutionService.cs` contains `ResolveSourceCandidateCommand`, `IPartyRepository`, `IdentityResolutionService`, `InMemoryPartyRepository`, `OutboxFact`, payload records). `src/Bluto.Domain/Identity/IdentityModel.cs` holds the whole Party aggregate. Follow this feature-file grouping rather than one-type-per-file.
- Projects: `Bluto.<Layer>` (`Bluto.Domain`, `Bluto.Application`, `Bluto.Infrastructure.Postgres`, `Bluto.Api`, `Bluto.Worker`, `Bluto.Validation.Cli`)
- Feature folders inside projects: `Identity/`, `Outbox/`, `Resolution/`
- SQL migrations: Flyway versioned `V###__snake_case_description.sql` in `implementation/db/migrations/` (e.g. `V002__wp013_privacy_tenant_lifecycle.sql`)
- JSON contracts: kebab-case with version suffix: `party-created.v1.schema.json`, `resolve-current-party.stale.v1.example.json`
- Test files: `<Subject>Tests.cs`; work-package specification tests use `Wp<NNN><Area>SpecificationTests.cs` (e.g. `tests/Bluto.Identity.Tests/Wp006IdentityResolutionDomainSpecificationTests.cs`)
- File-scoped namespaces matching `Project.Folder`: `namespace Bluto.Application.Identity;`, `namespace Bluto.Worker.Outbox;`
- Exception: `src/Bluto.Api/MappingApi.cs` uses `namespace Bluto.Api.Mapping;` while sitting at the project root. `src/Bluto.Validation.Cli` uses `RootNamespace` `Bluto.Validation`.
- PascalCase methods; async methods end in `Async` and take `CancellationToken cancellationToken` as the last parameter: `Task<Party?> FindActivePartyAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken)`
- Static factory methods for domain construction: `PartyId.New()`, `EffectiveInterval.HalfOpen(...)`, `Party.Rehydrate(...)`, `OutboxFact.PartyCreated(...)`, `ProvenancePayload.From(command)`
- Domain transitions are verbs: `MergeInto`, `Retire`, `Split`, `Reject`, `Supersede`, `EstablishSourceLink`
- Test-facing synchronous mirrors on in-memory adapters drop the suffix: `FindActiveParty`, `RecordIdempotency` in `InMemoryPartyRepository`
- Private fields are camelCase with no underscore prefix, assigned via `this.`:
- Static readonly fields and constants are PascalCase: `ApprovedIdentityTokenPattern`, `CreatePartyOperation`, `ToolVersion`
- Unused parameters are discarded explicitly to satisfy analyzers: `_ = fact;`, `_ = publishedAt;`, `public void ReplaceId(PartyId _)`
- `sealed` on every concrete class and record (`public sealed class`, `public sealed record`). Do not leave classes unsealed.
- Strongly typed IDs are `readonly record struct` wrapping `Guid` with `ToString() => Value.ToString("D")`: `PartyId`, `SourceLinkId` in `src/Bluto.Domain/Identity/IdentityModel.cs`
- Commands/queries/results/options are positional `sealed record`s: `ResolveSourceCandidateCommand`, `ResolveCurrentPartyQuery`, `OutboxPublisherOptions`, `ScheduledResolutionBatchResult`
- Ports are `I`-prefixed interfaces declared in the consuming layer: `IPartyRepository`, `ICurrentMappingRepository` (Application), `IOutboxStore`, `IOutboxTransport`, `IOutboxContractValidator` (Worker)
- Telemetry exposed as a nested `public static class Telemetry` with `Name`, `ActivitySource`, `Meter`, `MetricNames` (see `OutboxPublisher.Telemetry`, `ScheduledResolutionWorker.Telemetry`)
- Lifecycle/status values are lowercase snake_case strings, not enums: `"active"`, `"merged"`, `"retired"`, `PartySourceLink.Active = "active"`. Only `GateResult` in `src/Bluto.Validation.Cli/RepositoryValidator.cs` is an enum (serialized with `JsonStringEnumConverter<GateResult>`).
- Outcome codes are snake_case strings: `"created"`, `"idempotent"`, `"active_source_key_match"`, `"cross_tenant_collision"`, `"linked_existing_party"`
- API error codes are SCREAMING_SNAKE: `"AUTHENTICATION_REQUIRED"`, `"SCOPE_FORBIDDEN_CONCEALED"`, `"MAPPING_NOT_FOUND"`, `"DEPENDENCY_UNAVAILABLE"`
- All external JSON property names are snake_case via `[property: JsonPropertyName("party_id")]` on positional records (see `src/Bluto.Api/MappingApi.cs`, payload records in `IdentityResolutionService.cs`)
- SQL is lowercase with schema-qualified snake_case tables: `identity_resolution.parties`, `integration_outbox.outbox_facts`; Dapper column aliases map to PascalCase (`p.party_id as PartyId`)
- Metric names: `bluto.<area>.<metric_snake>` (`bluto.outbox.publish_latency_ms`); ActivitySource/Meter names: `Bluto.Worker.Outbox`

## Code Style

- `.editorconfig` (repo root `.editorconfig`): UTF-8, CRLF line endings, final newline, trim trailing whitespace, 2-space indent default, 4-space indent for `*.cs`
- `.gitattributes`: `*.cs text eol=crlf` (asserted by `tests/Bluto.Validation.Tests/CiConfigurationTests.cs`)
- `dotnet format implementation/Bluto.Validation.sln --verify-no-changes` runs in CI (`.github/workflows/wp002-ci.yml`). Run it locally before finishing.
- Allman braces; braces always used on `if` bodies, even single statements
- Expression-bodied members for one-expression methods/properties (`public bool Contains(DateTimeOffset instant) => ...`)
- Long conditions break before `&&` / `||` with leading operator, indented:
- Raw string literals (`"""`) for multi-line SQL in `src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`
- Collection expressions `[]` for empty/initialized collections: `private readonly HashSet<PartyId> issued = [];`
- Named arguments for boolean and ambiguous parameters: `new IdentityResolutionResult(replay, Created: false, Outcome: "idempotent")`, `EffectiveInterval.HalfOpen(command.EffectiveFrom, effectiveTo: null)`
- `TargetFramework` `net10.0`, `Nullable` enable, `ImplicitUsings` enable, `LangVersion` latest
- `TreatWarningsAsErrors` true, `EnforceCodeStyleInBuild` true, `AnalysisLevel` latest - any analyzer warning breaks the build
- `GenerateDocumentationFile` true, but every csproj sets `<NoWarn>$(NoWarn);CS1591</NoWarn>`; add the same `NoWarn` to any new project
- `.editorconfig` rules: `dotnet_sort_system_directives_first = true`, `IDE0005` (unused usings) = warning (therefore error), `CA1303` and `CA1812` disabled
- Package versions are central: `Directory.Packages.props` (`ManagePackageVersionsCentrally`). Add `<PackageVersion>` there and reference without `Version` in csproj.
- String comparisons always pass explicit `StringComparison.Ordinal` (or `OrdinalIgnoreCase`); dictionaries keyed by string use `StringComparer.Ordinal`
- Regexes are `static readonly` with `RegexOptions.Compiled | RegexOptions.CultureInvariant`
- Parsing uses `CultureInfo.InvariantCulture` (`RepositoryValidator.TryAuthorityRank`)

## Import Organization

- None. Implicit usings cover `System`, `System.Linq`, `System.Threading.Tasks`, etc. Unused usings fail the build (IDE0005).
- Some code uses fully-qualified names inline instead of a using (`System.Text.RegularExpressions.Regex` in `IdentityResolutionService.cs`); prefer a `using` directive in new code.

## Error Handling

- Map failure categories onto BCL exception types consistently; the API layer depends on this mapping:
- Guard clauses at method entry; call `cancellationToken.ThrowIfCancellationRequested();` first in every async/port method
- Expected business outcomes return result records with outcome strings rather than throwing (`IdentityResolutionResult(null, Created: false, Outcome: "cross_tenant_collision")`). Throw only for invalid input, authorization, and invariant violations.
- Exception messages are fixed, generic sentences and never interpolate input values (tests assert raw identifiers never appear in `error.Message`):
- Boundary translation happens once, in `MappingApi.ResolveCurrentPartyAsync` (`src/Bluto.Api/MappingApi.cs`): `UnauthorizedAccessException` -> 404 `SCOPE_FORBIDDEN_CONCEALED`, `ArgumentException` -> 400 `REQUEST_INVALID`, `InvalidOperationException` -> 503 `DEPENDENCY_UNAVAILABLE`. The endpoint returns a `MappingHttpResponse` record rather than throwing.
- Cross-tenant denials are concealed as 404, not 403, once a tenant claim exists.
- Worker catch blocks filter cancellation: `catch (Exception ex) when (ex is not OperationCanceledException)`, then map to a sanitized code via `SafeErrorCode` / `SanitizeErrorCode` (regex `^[a-z0-9_]{1,64}$` plus prohibited-term check). Never log `ex.Message`.
- Retry/dead-letter decision is owned by `OutboxPublisher.DrainAsync` (`attemptCount >= options.MaxAttempts` -> dead-letter, else back-off `30 * attemptCount` seconds).

## Logging

- Log line format is `<event_name> key=value key=value` with snake_case keys and GUIDs formatted `:D`:
- Allowed fields: `outcome`, `tenant_id`, `correlation_id`, `event_id`, `error_code`. Never log `source_key`, identity tokens, raw identifiers, or payloads.
- Metric tags use `tenant_id` only: `new KeyValuePair<string, object?>("tenant_id", batch.TenantId)`. Never tag with source keys.
- Constructors take `Action<string> log`; tests pass `logs.Add` or `_ => { }`.

## Comments

- Code is almost comment-free. Add a comment only to cite a governing requirement or explain an invariant, e.g. in `tests/Smoke/RuntimeConfigurationContractTests.cs`:
- Cite controlled document IDs (`BLUTO-CONTRACT-API-001`, `BLUTO-ADR-0006`) when referencing standards, as `implementation/tools/contracts/.spectral.yaml` does.
- Not used. CS1591 is suppressed in every project. Do not add `///` blocks unless a phase explicitly requires generated reference docs.

## Function Design

- Pass a single command/query record into application services (`ResolveAsync(ResolveSourceCandidateCommand command, CancellationToken cancellationToken)`)
- Tenant scope is always explicit: `Guid tenantId` is the first parameter of every repository port method, and services check `AuthorizedTenantIds.Contains(TenantId)`
- Dependencies injected via constructor; no service locator, no static mutable state (telemetry instruments are the only statics)
- Optional test-oriented constructor flags with defaults: `InMemoryPartyRepository(bool failBeforeOutboxCommit = false)`
- Nullable reference returns for "not found" (`Task<Party?>`, `Task<CurrentMapping?>`)
- Result records with counts and a `Metrics` dictionary for batch operations (`OutboxPublishResult`, `ScheduledResolutionBatchResult`)
- Read-only collection interfaces in signatures: `IReadOnlyList<T>`, `IReadOnlySet<Guid>`, `IReadOnlyDictionary<string, int>`; materialize with `.ToArray()`
- Deterministic ordering whenever output order matters: `.OrderBy(r => r.CreatedAt).ThenBy(r => r.Fact.EventId)`, `.Order(StringComparer.Ordinal)`
- Deterministic IDs derived by hashing (`OutboxFact.DeterministicEventId` uses SHA-256 of `eventType|idempotencyKey`)

## Module Design

- Types are `public` by default; use `internal` for helpers not consumed elsewhere (`ControlledDocument` in `RepositoryValidator.cs`, telemetry counters). Tests do not use `InternalsVisibleTo`.
- Layer dependencies (csproj references): Domain <- Application <- {Infrastructure.Postgres, Api, Worker}. Domain has no package references. Keep it that way.
- DI registration via extension methods on `IServiceCollection` / `IEndpointRouteBuilder`: `AddBlutoMappingApi()`, `MapBlutoMappingApi()` in `src/Bluto.Api/MappingApi.cs`
- Stateless strategy singletons exposed as static fields: `OutboxContractValidator.Governed`, `OutboxContractValidator.Permissive`, `OutboxTransport.Disabled`
- `src/Bluto.Api/Program.cs` ends with `public partial class Program;` to allow host-level testing
- Every controlled `.md` under `docs/`, `00-baseline/`, `specs/` starts with a metadata table (`Document ID`, `Artifact ID`, `Version`, `Status`, `Owner`, `Baseline`, `Stream`, `Authority Level`, `Depends On`, `Supersedes`, `Approval Date`, `Effective Date`, `Review Date`, `Classification`) and ends with `## Change control` / `## Document control`. `RepositoryValidator` (`src/Bluto.Validation.Cli/RepositoryValidator.cs`) enforces ID formats `^BLUTO(?:-[A-Z0-9]+)+-[0-9]{3,4}$` and `^ART-BLUTO(?:-[A-Z0-9]+)+-[0-9]{3,4}-v\d+\.\d+\.\d+$`. Use document IDs, not filenames, for cross-references.

<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

## Workspace Shape

- **Root kit (not under git):** `00-baseline/`, `docs/`, `repository/`, `specs/`, `contracts/`, `releases/`, `prompts/`, `README.md`, `release-manifest.json`, `SHA256SUMS.txt`. This is the certified, docs-only release as unpacked (status "Certified", `release-manifest.json` `release_id: REL-ENTERPRISE-2.0.0`). It also holds a few files that `publish-bluto/` does not have: `prompts/BigBuild.md`, `prompts/getstarted.txt`, `specs/BLUTO-ARCH-PRINCIPLES-00.md`, `specs/BLUTO_IDENTITY_SPINE_v0.1.md`, `docs/01-Foundation/Vision.md`, `docs/Stream1/RELEASE_CERTIFICATION.md`.
- **`publish-bluto/` (git repository, the active codebase):** the same governed document kit, with diverged copies of most files, plus `implementation/` (the .NET 10 solution), `.github/` (CI), `scripts/`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`. All code changes go here. Its git history follows the work packages WP-001 through WP-014.

## System Overview

```text

```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| Domain model | Party aggregate, immutable Party IDs, source links, half-open effective intervals, merge/split/retire state | `implementation/src/Bluto.Domain/Identity/IdentityModel.cs` |
| Identity resolution use case | Deterministic token match, idempotency, cross-tenant collision, review-case deferral, outbox fact creation | `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs` |
| Current mapping query | Tenant-scoped `ResolveCurrentParty` read, `ICurrentMappingRepository` port | `implementation/src/Bluto.Application/Identity/CurrentMappingQuery.cs` |
| In-memory adapters | Test/slice repositories (`InMemoryPartyRepository`, `InMemoryCurrentMappingRepository`) | `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs`, `.../InMemoryCurrentMappingRepository.cs` |
| Postgres adapter | `PostgresPartyRepository : IPartyRepository`, atomic SQL scripts for Party+link+outbox writes | `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs` |
| Mapping API | Minimal API route, claims-based tenant context, error envelopes, header propagation | `implementation/src/Bluto.Api/MappingApi.cs`, `.../Program.cs` |
| Scheduled resolution worker | Processes `SyntheticCandidateBatch` through `IdentityResolutionService` with dedup and deferral | `implementation/src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs` |
| Outbox publisher | Drains pending outbox facts, validates contracts, retries, dead-letters, emits metrics | `implementation/src/Bluto.Worker/Outbox/OutboxPublisher.cs` |
| Repository validator | Parses controlled-document metadata tables, runs GATE-01..06, writes RVEC evidence and report | `implementation/src/Bluto.Validation.Cli/RepositoryValidator.cs`, `.../Program.cs` |
| Schema migrations | Tables, constraints, row-level security policies | `implementation/db/migrations/` |
| Contracts | OpenAPI baseline, JSON Schemas for events/outbox, examples, fixtures | `implementation/contracts/`, `docs/04-contracts/schemas/` |
| Deployment contract | Terraform locals/outputs describing the Azure non-production target (no resources declared) | `implementation/infra/terraform/main.tf` |

## Pattern Overview

- Dependencies point inward: Api/Worker → Application → Domain; Infrastructure.Postgres → Application + Domain. Domain references no packages or projects (`Bluto.Domain.csproj`).
- Ports (interfaces) live in the Application layer next to the use case that consumes them (`IPartyRepository`, `ICurrentMappingRepository`, `IOutboxStore`/`IOutboxTransport`/`IOutboxContractValidator` live in the Worker project).
- Transactional outbox: authoritative state and outbox facts are written in one serializable transaction (`PostgresPartyRepository.CommitNewPartyWithLinkAndOutboxAsync`).
- Tenant isolation everywhere: every command/query carries `TenantId` and `AuthorizedTenantIds`. Postgres sets `bluto.tenant_id` via `set_config` before each query, and RLS policies enforce it (`db/migrations/V002__wp013_privacy_tenant_lifecycle.sql`).
- No raw strong identifiers: matching uses only `v1.<43-char base64url>` HMAC tokens (`ApprovedIdentityTokenPattern` in `IdentityResolutionService.cs`). Commands that carry `RawStrongIdentifier` are rejected.
- Deterministic, non-probabilistic v1 matching (`MatchMethod = "deterministic_exact_token"`).
- Services take an `Action<string> log` delegate instead of `ILogger`, so tests can capture structured `key=value` log lines.

## Layers

- Purpose: Identity invariants (immutable Party IDs, no ID reuse, half-open intervals, single active link per source identity scope).
- Location: `implementation/src/Bluto.Domain/Identity/`
- Contains: `readonly record struct` IDs, `sealed record` value objects, `sealed class Party` aggregate with `Rehydrate` factory.
- Depends on: nothing.
- Used by: Application, Infrastructure.Postgres, tests.
- Purpose: Use cases, command/query records, ports, outbox fact construction, in-memory adapters.
- Location: `implementation/src/Bluto.Application/Identity/`
- Contains: `IdentityResolutionService`, `CurrentMappingQueryService`, `ResolveSourceCandidateCommand`, `ResolveCurrentPartyQuery`, `OutboxFact` plus payload records, `ReviewCase`.
- Depends on: Domain.
- Used by: Api, Worker, Infrastructure.Postgres, tests.
- Purpose: Persistence adapter for `IPartyRepository`.
- Location: `implementation/src/Bluto.Infrastructure.Postgres/Identity/`
- Contains: `PostgresPartyRepository`, `PostgresIdentityResolutionSql` (raw SQL constants), private row DTOs.
- Depends on: Application, Domain, Dapper, Npgsql.
- Used by: `tests/Bluto.Persistence.Tests` only. No host wires it.
- Purpose: Transport entry points.
- Location: `implementation/src/Bluto.Api/`, `implementation/src/Bluto.Worker/`
- Contains: Minimal API endpoint and DI extensions; worker/publisher classes with OpenTelemetry `ActivitySource`/`Meter`.
- Depends on: Application, OpenTelemetry.Api.
- Used by: container images `bluto-api`, `bluto-worker` (csproj `ContainerRepository`).
- Purpose: Enforce repository document gates.
- Location: `implementation/src/Bluto.Validation.Cli/`
- Depends on: nothing outside the BCL. Reads `repository/lir/`, `repository/rgtm/`, `repository/validation/`, and all controlled Markdown.

## Data Flow

### Identity Resolution (write path)

### Current Mapping Read (API path)

### Outbox Publishing

### Repository Validation

- Authoritative state lives only in PostgreSQL (`identity_resolution.*`, `integration_outbox.outbox_facts`). The in-memory repositories stand in for it in tests and slices.
- Domain aggregates are mutable classes with private setters and an optimistic `Version` counter that increments on each mutation.

## Key Abstractions

- Purpose: Stable identity with source links and lifecycle (active/merged/retired).
- Examples: `implementation/src/Bluto.Domain/Identity/IdentityModel.cs`
- Pattern: Aggregate root. Use `Party.Rehydrate(...)` for persisted state, `EstablishSourceLink` for new links, and `LoadSourceLink` for hydration.
- Purpose: Isolate persistence/transport from use cases.
- Examples: `IPartyRepository`, `ICurrentMappingRepository` (Application); `IOutboxStore`, `IOutboxTransport`, `IOutboxContractValidator`, `ISourceTokenCollisionRegistry` (Worker).
- Pattern: Async interfaces with a `CancellationToken` as the last parameter.
- Purpose: Integration event envelope persisted with state.
- Examples: `OutboxFact.PartyCreated`, `OutboxFact.SourceLinkEstablished` in `IdentityResolutionService.cs`. Schemas in `implementation/contracts/schemas/outbox-fact.v1.schema.json`.
- Pattern: Static factory methods on a sealed record.
- Purpose: Governed Markdown artifact identified by `Document ID` (`BLUTO-...-NNN`) and `Artifact ID` (`ART-BLUTO-...-vX.Y.Z`) in a metadata table.
- Examples: every file under `00-baseline/` and `docs/`.
- Pattern: Cross-references use Document IDs, not filenames (README "Document control").

## Entry Points

- Location: `implementation/src/Bluto.Api/Program.cs`
- Triggers: HTTP (ASP.NET Core minimal API). `public partial class Program` exposes it to `WebApplicationFactory` tests.
- Responsibilities: `AddBlutoMappingApi()` + `MapBlutoMappingApi()`.
- Location: `implementation/src/Bluto.Worker/` (class library SDK, no `Program.cs` or host)
- Triggers: Invoked by tests only. Container settings exist in the csproj.
- Responsibilities: `ScheduledResolutionWorker.ProcessAsync`, `OutboxPublisher.DrainAsync`.
- Location: `implementation/src/Bluto.Validation.Cli/Program.cs`
- Triggers: `dotnet run --project implementation/src/Bluto.Validation.Cli -- --root .`, run from the `publish-bluto/` root and in CI (`.github/workflows/wp001-validation.yml`, `wp002-ci.yml`).
- Responsibilities: Governance gates GATE-01..06 and evidence emission.
- Location: `scripts/windows/Invoke-BlutoFlyway.ps1`, config `implementation/tools/flyway/flyway.local.conf`
- Triggers: Manual (`-Command info|validate|migrate|repair`), with credentials from `BLUTO_FLYWAY_*` env vars.
- Location: `implementation/tools/contracts/package.json` (Spectral + AJV npm scripts)

## Architectural Constraints

- **Threading:** Async/await throughout. There is no background hosted service. Outbox publisher and worker run as one-shot batch calls.
- **Global state:** Static `Telemetry` classes with `ActivitySource`/`Meter` singletons in `OutboxPublisher.cs` and `ScheduledResolutionWorker.cs`. `OutboxPublisher` keeps an instance-level `publishedEventIds` HashSet, so duplicate suppression only covers the life of the instance.
- **Circular imports:** None. The project graph is acyclic (Domain ← Application ← {Api, Worker, Infrastructure.Postgres}).
- **Build strictness:** `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `Nullable=enable`, `AnalysisLevel=latest` (`Directory.Build.props`). Package versions are pinned centrally (`Directory.Packages.props`).
- **Authority:** Files under `specs/`, `contracts/`, `docs/00-authoritative/`, and baselined docs are immutable. Changes require ADR/change control (`docs/01-foundation-governance/CHANGE-CONTROL.md`).
- **Preserved decisions:** managed PostgreSQL, transactional outbox, HTTP JSON APIs, Key Vault-backed HMAC-SHA-256, no probabilistic matching in v1 (`WORK-PACKAGE-INDEX.yaml`).
- **Host wiring gap:** `Bluto.Api/Program.cs` registers `CurrentMappingQueryService` but no `ICurrentMappingRepository`. Only tests supply one (`tests/Api/MappingApiRouteContractTests.cs:73`). No Postgres `ICurrentMappingRepository` or `IOutboxStore` implementation exists yet.

## Anti-Patterns

### Tenant scoping outside the database session

### Writing state and outbox facts separately

### Leaking existence across tenants

### Editing the root kit instead of `publish-bluto/`

## Error Handling

- `ArgumentException` = invalid input (API → 400), `UnauthorizedAccessException` = tenant scope violation (API → concealed 404), `InvalidOperationException` = invariant violation or dependency issue (API → 503), `DBConcurrencyException` = atomic write mismatch.
- Domain-level business outcomes (cross-tenant collision, same-source conflict) come back as `IdentityResolutionResult.Outcome` strings, not as exceptions.
- The outbox publisher catches all non-cancellation exceptions per fact and converts them to retry or dead-letter state with `SafeErrorCode(ex)`.

## Cross-Cutting Concerns

<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:

- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
