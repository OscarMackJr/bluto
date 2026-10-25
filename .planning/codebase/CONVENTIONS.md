# Coding Conventions

**Analysis Date:** 2026-10-24

**Scope note:** The only executable code is the .NET solution under `implementation/` (solution file `implementation/Bluto.Validation.sln`). The root `C:\Users\OMack\bluto` tree (`docs/`, `00-baseline/`, `repository/`, `specs/`, `contracts/`) and `docs/` hold governed Markdown, JSON registries, and evidence, not code. All paths below are relative to `publish-bluto/` unless stated otherwise.

Normative engineering rules live in controlled docs. Read them before writing code:
- `docs/06-engineering-standards/CODING-STANDARDS.md` - small single-purpose functions, side effects behind ports, no mutable global state, domain must not touch HTTP/SQL/clock/randomness/filesystem/env directly
- `docs/06-engineering-standards/ERROR-HANDLING.md` - typed error categories, translate adapter errors once, no catch-all suppression
- `docs/06-engineering-standards/NAMING-DOCUMENTATION.md` - Ubiquitous Language names are mandatory; source-specific terms stay in adapters
- `docs/06-engineering-standards/LOGGING-OBSERVABILITY.md` - stable event names; raw strong identifiers, HMAC tokens, source payloads, secrets prohibited in logs; source keys never metric labels

No project skills directory (`.claude/skills/`, `.agents/skills/`) exists.

## Naming Patterns

**Files:**
- PascalCase `.cs` file named after the primary type or feature area: `src/Bluto.Application/Identity/IdentityResolutionService.cs`, `src/Bluto.Worker/Outbox/OutboxPublisher.cs`
- A file may group a feature's records, interfaces, and classes together (e.g. `src/Bluto.Application/Identity/IdentityResolutionService.cs` contains `ResolveSourceCandidateCommand`, `IPartyRepository`, `IdentityResolutionService`, `InMemoryPartyRepository`, `OutboxFact`, payload records). `src/Bluto.Domain/Identity/IdentityModel.cs` holds the whole Party aggregate. Follow this feature-file grouping rather than one-type-per-file.
- Projects: `Bluto.<Layer>` (`Bluto.Domain`, `Bluto.Application`, `Bluto.Infrastructure.Postgres`, `Bluto.Api`, `Bluto.Worker`, `Bluto.Validation.Cli`)
- Feature folders inside projects: `Identity/`, `Outbox/`, `Resolution/`
- SQL migrations: Flyway versioned `V###__snake_case_description.sql` in `implementation/db/migrations/` (e.g. `V002__wp013_privacy_tenant_lifecycle.sql`)
- JSON contracts: kebab-case with version suffix: `party-created.v1.schema.json`, `resolve-current-party.stale.v1.example.json`
- Test files: `<Subject>Tests.cs`; work-package specification tests use `Wp<NNN><Area>SpecificationTests.cs` (e.g. `tests/Bluto.Identity.Tests/Wp006IdentityResolutionDomainSpecificationTests.cs`)

**Namespaces:**
- File-scoped namespaces matching `Project.Folder`: `namespace Bluto.Application.Identity;`, `namespace Bluto.Worker.Outbox;`
- Exception: `src/Bluto.Api/MappingApi.cs` uses `namespace Bluto.Api.Mapping;` while sitting at the project root. `src/Bluto.Validation.Cli` uses `RootNamespace` `Bluto.Validation`.

**Functions:**
- PascalCase methods; async methods end in `Async` and take `CancellationToken cancellationToken` as the last parameter: `Task<Party?> FindActivePartyAsync(Guid tenantId, string sourceSystem, string sourceKey, CancellationToken cancellationToken)`
- Static factory methods for domain construction: `PartyId.New()`, `EffectiveInterval.HalfOpen(...)`, `Party.Rehydrate(...)`, `OutboxFact.PartyCreated(...)`, `ProvenancePayload.From(command)`
- Domain transitions are verbs: `MergeInto`, `Retire`, `Split`, `Reject`, `Supersede`, `EstablishSourceLink`
- Test-facing synchronous mirrors on in-memory adapters drop the suffix: `FindActiveParty`, `RecordIdempotency` in `InMemoryPartyRepository`

**Variables / fields:**
- Private fields are camelCase with no underscore prefix, assigned via `this.`:
  ```csharp
  private readonly IPartyRepository repository;
  private readonly Action<string> log;

  public IdentityResolutionService(IPartyRepository repository, Action<string> log)
  {
      this.repository = repository;
      this.log = log;
  }
  ```
- Static readonly fields and constants are PascalCase: `ApprovedIdentityTokenPattern`, `CreatePartyOperation`, `ToolVersion`
- Unused parameters are discarded explicitly to satisfy analyzers: `_ = fact;`, `_ = publishedAt;`, `public void ReplaceId(PartyId _)`

**Types:**
- `sealed` on every concrete class and record (`public sealed class`, `public sealed record`). Do not leave classes unsealed.
- Strongly typed IDs are `readonly record struct` wrapping `Guid` with `ToString() => Value.ToString("D")`: `PartyId`, `SourceLinkId` in `src/Bluto.Domain/Identity/IdentityModel.cs`
- Commands/queries/results/options are positional `sealed record`s: `ResolveSourceCandidateCommand`, `ResolveCurrentPartyQuery`, `OutboxPublisherOptions`, `ScheduledResolutionBatchResult`
- Ports are `I`-prefixed interfaces declared in the consuming layer: `IPartyRepository`, `ICurrentMappingRepository` (Application), `IOutboxStore`, `IOutboxTransport`, `IOutboxContractValidator` (Worker)
- Telemetry exposed as a nested `public static class Telemetry` with `Name`, `ActivitySource`, `Meter`, `MetricNames` (see `OutboxPublisher.Telemetry`, `ScheduledResolutionWorker.Telemetry`)
- Lifecycle/status values are lowercase snake_case strings, not enums: `"active"`, `"merged"`, `"retired"`, `PartySourceLink.Active = "active"`. Only `GateResult` in `src/Bluto.Validation.Cli/RepositoryValidator.cs` is an enum (serialized with `JsonStringEnumConverter<GateResult>`).
- Outcome codes are snake_case strings: `"created"`, `"idempotent"`, `"active_source_key_match"`, `"cross_tenant_collision"`, `"linked_existing_party"`
- API error codes are SCREAMING_SNAKE: `"AUTHENTICATION_REQUIRED"`, `"SCOPE_FORBIDDEN_CONCEALED"`, `"MAPPING_NOT_FOUND"`, `"DEPENDENCY_UNAVAILABLE"`

**Wire format (JSON/SQL):**
- All external JSON property names are snake_case via `[property: JsonPropertyName("party_id")]` on positional records (see `src/Bluto.Api/MappingApi.cs`, payload records in `IdentityResolutionService.cs`)
- SQL is lowercase with schema-qualified snake_case tables: `identity_resolution.parties`, `integration_outbox.outbox_facts`; Dapper column aliases map to PascalCase (`p.party_id as PartyId`)
- Metric names: `bluto.<area>.<metric_snake>` (`bluto.outbox.publish_latency_ms`); ActivitySource/Meter names: `Bluto.Worker.Outbox`

## Code Style

**Formatting:**
- `.editorconfig` (repo root `.editorconfig`): UTF-8, CRLF line endings, final newline, trim trailing whitespace, 2-space indent default, 4-space indent for `*.cs`
- `.gitattributes`: `*.cs text eol=crlf` (asserted by `tests/Bluto.Validation.Tests/CiConfigurationTests.cs`)
- `dotnet format implementation/Bluto.Validation.sln --verify-no-changes` runs in CI (`.github/workflows/wp002-ci.yml`). Run it locally before finishing.
- Allman braces; braces always used on `if` bodies, even single statements
- Expression-bodied members for one-expression methods/properties (`public bool Contains(DateTimeOffset instant) => ...`)
- Long conditions break before `&&` / `||` with leading operator, indented:
  ```csharp
  if (sourceLinks.Any(link =>
          link.TenantId == tenantId
          && link.SourceSystem.Equals(sourceSystem, StringComparison.Ordinal)
          && link.Status.Equals(PartySourceLink.Active, StringComparison.Ordinal)))
  ```
- Raw string literals (`"""`) for multi-line SQL in `src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`
- Collection expressions `[]` for empty/initialized collections: `private readonly HashSet<PartyId> issued = [];`
- Named arguments for boolean and ambiguous parameters: `new IdentityResolutionResult(replay, Created: false, Outcome: "idempotent")`, `EffectiveInterval.HalfOpen(command.EffectiveFrom, effectiveTo: null)`

**Linting / compiler settings (`Directory.Build.props`):**
- `TargetFramework` `net10.0`, `Nullable` enable, `ImplicitUsings` enable, `LangVersion` latest
- `TreatWarningsAsErrors` true, `EnforceCodeStyleInBuild` true, `AnalysisLevel` latest - any analyzer warning breaks the build
- `GenerateDocumentationFile` true, but every csproj sets `<NoWarn>$(NoWarn);CS1591</NoWarn>`; add the same `NoWarn` to any new project
- `.editorconfig` rules: `dotnet_sort_system_directives_first = true`, `IDE0005` (unused usings) = warning (therefore error), `CA1303` and `CA1812` disabled
- Package versions are central: `Directory.Packages.props` (`ManagePackageVersionsCentrally`). Add `<PackageVersion>` there and reference without `Version` in csproj.
- String comparisons always pass explicit `StringComparison.Ordinal` (or `OrdinalIgnoreCase`); dictionaries keyed by string use `StringComparer.Ordinal`
- Regexes are `static readonly` with `RegexOptions.Compiled | RegexOptions.CultureInvariant`
- Parsing uses `CultureInfo.InvariantCulture` (`RepositoryValidator.TryAuthorityRank`)

## Import Organization

**Order:**
1. `System.*` namespaces, alphabetical (enforced by `dotnet_sort_system_directives_first`)
2. `Bluto.*` project namespaces
3. Third-party (`Dapper`, `Npgsql`, `Microsoft.AspNetCore.*`, `Json.Schema`, `Testcontainers.*`, `Xunit`) alphabetical with the rest

Example (`src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`):
```csharp
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bluto.Application.Identity;
using Bluto.Domain.Identity;
using Dapper;
using Npgsql;
```

**Path Aliases:**
- None. Implicit usings cover `System`, `System.Linq`, `System.Threading.Tasks`, etc. Unused usings fail the build (IDE0005).
- Some code uses fully-qualified names inline instead of a using (`System.Text.RegularExpressions.Regex` in `IdentityResolutionService.cs`); prefer a `using` directive in new code.

## Error Handling

**Patterns:**
- Map failure categories onto BCL exception types consistently; the API layer depends on this mapping:
  - `ArgumentException` - validation failure (missing tenant, malformed token, bad idempotency key, invalid interval)
  - `UnauthorizedAccessException` - tenant scope not authorized
  - `InvalidOperationException` - domain invariant violation / dependency failure (Party ID reuse, immutable ID, self-merge)
  - `DBConcurrencyException` - atomic-commit verification failure in `PostgresPartyRepository`
  - Custom `sealed` exceptions carrying a safe `Code` for worker paths: `OutboxTransportException`, `OutboxContractValidationException` in `src/Bluto.Worker/Outbox/OutboxPublisher.cs`
- Guard clauses at method entry; call `cancellationToken.ThrowIfCancellationRequested();` first in every async/port method
- Expected business outcomes return result records with outcome strings rather than throwing (`IdentityResolutionResult(null, Created: false, Outcome: "cross_tenant_collision")`). Throw only for invalid input, authorization, and invariant violations.
- Exception messages are fixed, generic sentences and never interpolate input values (tests assert raw identifiers never appear in `error.Message`):
  ```csharp
  throw new ArgumentException("raw strong identifier rejected; provide only approved canonical tokens.");
  ```
- Boundary translation happens once, in `MappingApi.ResolveCurrentPartyAsync` (`src/Bluto.Api/MappingApi.cs`): `UnauthorizedAccessException` -> 404 `SCOPE_FORBIDDEN_CONCEALED`, `ArgumentException` -> 400 `REQUEST_INVALID`, `InvalidOperationException` -> 503 `DEPENDENCY_UNAVAILABLE`. The endpoint returns a `MappingHttpResponse` record rather than throwing.
- Cross-tenant denials are concealed as 404, not 403, once a tenant claim exists.
- Worker catch blocks filter cancellation: `catch (Exception ex) when (ex is not OperationCanceledException)`, then map to a sanitized code via `SafeErrorCode` / `SanitizeErrorCode` (regex `^[a-z0-9_]{1,64}$` plus prohibited-term check). Never log `ex.Message`.
- Retry/dead-letter decision is owned by `OutboxPublisher.DrainAsync` (`attemptCount >= options.MaxAttempts` -> dead-letter, else back-off `30 * attemptCount` seconds).

## Logging

**Framework:** Injected `Action<string> log` delegate in application and worker services; ASP.NET Core `ILogger` only at the endpoint adapter (`logger.LogInformation("{BlutoMappingOutcome}", message)` in `BlutoMappingEndpoints`). OpenTelemetry `ActivitySource`/`Meter` (`System.Diagnostics`) for traces and metrics.

**Patterns:**
- Log line format is `<event_name> key=value key=value` with snake_case keys and GUIDs formatted `:D`:
  ```csharp
  log($"identity_resolution_rejected outcome=cross_tenant_collision tenant_id={command.TenantId:D} correlation_id={command.CorrelationId:D}");
  log($"outbox_publish outcome=dead_lettered tenant_id={batch.TenantId:D} event_id={record.Fact.EventId:D} error_code={errorCode}");
  ```
- Allowed fields: `outcome`, `tenant_id`, `correlation_id`, `event_id`, `error_code`. Never log `source_key`, identity tokens, raw identifiers, or payloads.
- Metric tags use `tenant_id` only: `new KeyValuePair<string, object?>("tenant_id", batch.TenantId)`. Never tag with source keys.
- Constructors take `Action<string> log`; tests pass `logs.Add` or `_ => { }`.

## Comments

**When to Comment:**
- Code is almost comment-free. Add a comment only to cite a governing requirement or explain an invariant, e.g. in `tests/Smoke/RuntimeConfigurationContractTests.cs`:
  ```csharp
  // REQ-ID-11: Bluto configuration must not contain the tokenization key; see BLUTO_IDENTITY_SPINE_v0.1 section 14, Tokenization Key Ownership and Rotation.
  ```
- Cite controlled document IDs (`BLUTO-CONTRACT-API-001`, `BLUTO-ADR-0006`) when referencing standards, as `implementation/tools/contracts/.spectral.yaml` does.

**XML doc comments:**
- Not used. CS1591 is suppressed in every project. Do not add `///` blocks unless a phase explicitly requires generated reference docs.

## Function Design

**Size:** Most methods are under 40 lines. Long orchestrators exist (`IdentityResolutionService.ResolveAsync` ~80 lines, `OutboxPublisher.DrainAsync` ~75 lines); keep new methods smaller and extract private static helpers (`EstablishLink`, `Validate`, `ValidateIdempotencyKey`, `Success`, `Error`).

**Parameters:**
- Pass a single command/query record into application services (`ResolveAsync(ResolveSourceCandidateCommand command, CancellationToken cancellationToken)`)
- Tenant scope is always explicit: `Guid tenantId` is the first parameter of every repository port method, and services check `AuthorizedTenantIds.Contains(TenantId)`
- Dependencies injected via constructor; no service locator, no static mutable state (telemetry instruments are the only statics)
- Optional test-oriented constructor flags with defaults: `InMemoryPartyRepository(bool failBeforeOutboxCommit = false)`

**Return Values:**
- Nullable reference returns for "not found" (`Task<Party?>`, `Task<CurrentMapping?>`)
- Result records with counts and a `Metrics` dictionary for batch operations (`OutboxPublishResult`, `ScheduledResolutionBatchResult`)
- Read-only collection interfaces in signatures: `IReadOnlyList<T>`, `IReadOnlySet<Guid>`, `IReadOnlyDictionary<string, int>`; materialize with `.ToArray()`
- Deterministic ordering whenever output order matters: `.OrderBy(r => r.CreatedAt).ThenBy(r => r.Fact.EventId)`, `.Order(StringComparer.Ordinal)`
- Deterministic IDs derived by hashing (`OutboxFact.DeterministicEventId` uses SHA-256 of `eventType|idempotencyKey`)

## Module Design

**Exports:**
- Types are `public` by default; use `internal` for helpers not consumed elsewhere (`ControlledDocument` in `RepositoryValidator.cs`, telemetry counters). Tests do not use `InternalsVisibleTo`.
- Layer dependencies (csproj references): Domain <- Application <- {Infrastructure.Postgres, Api, Worker}. Domain has no package references. Keep it that way.
- DI registration via extension methods on `IServiceCollection` / `IEndpointRouteBuilder`: `AddBlutoMappingApi()`, `MapBlutoMappingApi()` in `src/Bluto.Api/MappingApi.cs`
- Stateless strategy singletons exposed as static fields: `OutboxContractValidator.Governed`, `OutboxContractValidator.Permissive`, `OutboxTransport.Disabled`
- `src/Bluto.Api/Program.cs` ends with `public partial class Program;` to allow host-level testing

**Barrel Files:** Not applicable (C#). Group related types in one feature file instead.

**Governed Markdown artifacts:**
- Every controlled `.md` under `docs/`, `00-baseline/`, `specs/` starts with a metadata table (`Document ID`, `Artifact ID`, `Version`, `Status`, `Owner`, `Baseline`, `Stream`, `Authority Level`, `Depends On`, `Supersedes`, `Approval Date`, `Effective Date`, `Review Date`, `Classification`) and ends with `## Change control` / `## Document control`. `RepositoryValidator` (`src/Bluto.Validation.Cli/RepositoryValidator.cs`) enforces ID formats `^BLUTO(?:-[A-Z0-9]+)+-[0-9]{3,4}$` and `^ART-BLUTO(?:-[A-Z0-9]+)+-[0-9]{3,4}-v\d+\.\d+\.\d+$`. Use document IDs, not filenames, for cross-references.

---

*Convention analysis: 2026-10-24*
