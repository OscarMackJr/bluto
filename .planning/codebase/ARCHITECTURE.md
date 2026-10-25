<!-- refreshed: 2026-10-24 -->
# Architecture

**Analysis Date:** 2026-10-24

## Workspace Shape

The workspace root `C:\Users\OMack\bluto` holds two copies of the Bluto Enterprise Starter Kit v2.0. Know which one you are editing before you change anything:

- **Root kit (not under git):** `00-baseline/`, `docs/`, `repository/`, `specs/`, `contracts/`, `releases/`, `prompts/`, `README.md`, `release-manifest.json`, `SHA256SUMS.txt`. This is the certified, docs-only release as unpacked (status "Certified", `release-manifest.json` `release_id: REL-ENTERPRISE-2.0.0`). It also holds a few files that `publish-bluto/` does not have: `prompts/BigBuild.md`, `prompts/getstarted.txt`, `specs/BLUTO-ARCH-PRINCIPLES-00.md`, `specs/BLUTO_IDENTITY_SPINE_v0.1.md`, `docs/01-Foundation/Vision.md`, `docs/Stream1/RELEASE_CERTIFICATION.md`.
- **`publish-bluto/` (git repository, the active codebase):** the same governed document kit, with diverged copies of most files, plus `implementation/` (the .NET 10 solution), `.github/` (CI), `scripts/`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`. All code changes go here. Its git history follows the work packages WP-001 through WP-014.

The system follows a **documentation-first, contracts-first, tests-first, code-last** sequence (`implementation/plan/WORK-PACKAGE-INDEX.yaml` `sequence_policy`). Controlled Markdown is the normative authority. Code implements certified work packages only.

## System Overview

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                 Governance / Authority Layer (normative Markdown)          │
│  `specs/`  `docs/00-authoritative/`  `00-baseline/`  `docs/01..09-*/`      │
│  `releases/`  `contracts/vendored/`  `implementation/plan/`                │
└───────────────┬───────────────────────────────────────┬───────────────────┘
                │ projected into                         │ validated by
                ▼                                        ▼
┌──────────────────────────────────┐   ┌────────────────────────────────────┐
│ Machine Registry (`repository/`) │◄──│ Bluto.Validation.Cli (GATE-01..06) │
│ lir/ rgtm/ validation/ schemas/  │   │ `implementation/src/Bluto.         │
│ evidence/rvec/ constitution/     │   │   Validation.Cli/`                 │
└──────────────────────────────────┘   └────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────────────┐
│                  Runtime (modular monolith, two entry points)              │
├─────────────────────────────────┬─────────────────────────────────────────┤
│  Bluto.Api (HTTP JSON)          │  Bluto.Worker (batch + outbox)          │
│  `src/Bluto.Api/MappingApi.cs`  │  `src/Bluto.Worker/Resolution/`         │
│  GET /v1/mappings/{sys}/{key}   │  `src/Bluto.Worker/Outbox/`             │
└───────────────┬─────────────────┴──────────────────┬──────────────────────┘
                ▼                                    ▼
┌───────────────────────────────────────────────────────────────────────────┐
│ Bluto.Application (use cases + ports)  `src/Bluto.Application/Identity/`   │
│ IdentityResolutionService, CurrentMappingQueryService, OutboxFact,         │
│ IPartyRepository, ICurrentMappingRepository, InMemory* adapters            │
└───────────────┬───────────────────────────────────────────────────────────┘
                ▼
┌───────────────────────────────────────────────────────────────────────────┐
│ Bluto.Domain (pure model)  `src/Bluto.Domain/Identity/IdentityModel.cs`    │
│ Party, PartyId, PartySourceLink, EffectiveInterval, LinkProvenance         │
└───────────────────────────────────────────────────────────────────────────┘
                ▲ implements ports
┌───────────────────────────────────────────────────────────────────────────┐
│ Bluto.Infrastructure.Postgres  `src/Bluto.Infrastructure.Postgres/`        │
│ PostgresPartyRepository (Dapper + Npgsql, RLS tenant context)              │
└───────────────┬───────────────────────────────────────────────────────────┘
                ▼
┌───────────────────────────────────────────────────────────────────────────┐
│ PostgreSQL: schemas identity_resolution, integration_outbox                │
│ `implementation/db/migrations/V001__*.sql`, `V002__*.sql` (Flyway)         │
└───────────────────────────────────────────────────────────────────────────┘
```

All `src/` and `db/` paths above are relative to `implementation/`.

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

**Overall:** Clean / hexagonal layering inside a modular monolith, with separate API and worker entry points (preserved decisions `modular-monolith-first`, `separate-api-worker-stewardship-entry-points` in `implementation/plan/WORK-PACKAGE-INDEX.yaml`; logical layering in `docs/03-architecture/LOGICAL-ARCHITECTURE.md`).

**Key Characteristics:**
- Dependencies point inward: Api/Worker → Application → Domain; Infrastructure.Postgres → Application + Domain. Domain references no packages or projects (`Bluto.Domain.csproj`).
- Ports (interfaces) live in the Application layer next to the use case that consumes them (`IPartyRepository`, `ICurrentMappingRepository`, `IOutboxStore`/`IOutboxTransport`/`IOutboxContractValidator` live in the Worker project).
- Transactional outbox: authoritative state and outbox facts are written in one serializable transaction (`PostgresPartyRepository.CommitNewPartyWithLinkAndOutboxAsync`).
- Tenant isolation everywhere: every command/query carries `TenantId` and `AuthorizedTenantIds`. Postgres sets `bluto.tenant_id` via `set_config` before each query, and RLS policies enforce it (`db/migrations/V002__wp013_privacy_tenant_lifecycle.sql`).
- No raw strong identifiers: matching uses only `v1.<43-char base64url>` HMAC tokens (`ApprovedIdentityTokenPattern` in `IdentityResolutionService.cs`). Commands that carry `RawStrongIdentifier` are rejected.
- Deterministic, non-probabilistic v1 matching (`MatchMethod = "deterministic_exact_token"`).
- Services take an `Action<string> log` delegate instead of `ILogger`, so tests can capture structured `key=value` log lines.

## Layers

**Domain:**
- Purpose: Identity invariants (immutable Party IDs, no ID reuse, half-open intervals, single active link per source identity scope).
- Location: `implementation/src/Bluto.Domain/Identity/`
- Contains: `readonly record struct` IDs, `sealed record` value objects, `sealed class Party` aggregate with `Rehydrate` factory.
- Depends on: nothing.
- Used by: Application, Infrastructure.Postgres, tests.

**Application:**
- Purpose: Use cases, command/query records, ports, outbox fact construction, in-memory adapters.
- Location: `implementation/src/Bluto.Application/Identity/`
- Contains: `IdentityResolutionService`, `CurrentMappingQueryService`, `ResolveSourceCandidateCommand`, `ResolveCurrentPartyQuery`, `OutboxFact` plus payload records, `ReviewCase`.
- Depends on: Domain.
- Used by: Api, Worker, Infrastructure.Postgres, tests.

**Infrastructure (Postgres):**
- Purpose: Persistence adapter for `IPartyRepository`.
- Location: `implementation/src/Bluto.Infrastructure.Postgres/Identity/`
- Contains: `PostgresPartyRepository`, `PostgresIdentityResolutionSql` (raw SQL constants), private row DTOs.
- Depends on: Application, Domain, Dapper, Npgsql.
- Used by: `tests/Bluto.Persistence.Tests` only. No host wires it.

**Interfaces (Api / Worker):**
- Purpose: Transport entry points.
- Location: `implementation/src/Bluto.Api/`, `implementation/src/Bluto.Worker/`
- Contains: Minimal API endpoint and DI extensions; worker/publisher classes with OpenTelemetry `ActivitySource`/`Meter`.
- Depends on: Application, OpenTelemetry.Api.
- Used by: container images `bluto-api`, `bluto-worker` (csproj `ContainerRepository`).

**Governance tooling:**
- Purpose: Enforce repository document gates.
- Location: `implementation/src/Bluto.Validation.Cli/`
- Depends on: nothing outside the BCL. Reads `repository/lir/`, `repository/rgtm/`, `repository/validation/`, and all controlled Markdown.

## Data Flow

### Identity Resolution (write path)

1. The worker receives a `SyntheticCandidateBatch` and rejects unauthorized tenant scopes (`ScheduledResolutionWorker.ProcessAsync`, `src/Bluto.Worker/Resolution/ScheduledResolutionWorker.cs:102`).
2. Candidates are ordered by `SourceKey`, de-duplicated by idempotency key, and deferred when the ruleset or a token collision requires it. The rest map to `ResolveSourceCandidateCommand`.
3. `IdentityResolutionService.ResolveAsync` (`src/Bluto.Application/Identity/IdentityResolutionService.cs:71`) rejects raw identifiers, validates the token shape and the 6-part `|`-delimited idempotency key, checks tenant authorization, then evaluates in order: idempotent replay → active source-key match → cross-tenant token collision (review case) → same-source conflict (review case) → link to existing Party → create a new Party.
4. Writes go through `IPartyRepository.CommitNewPartyWithLinkAndOutboxAsync` / `CommitSourceLinkWithOutboxAsync` together with `OutboxFact.PartyCreated` / `OutboxFact.SourceLinkEstablished`.
5. The Postgres adapter runs the atomic SQL in a `Serializable` transaction and throws `DBConcurrencyException` when the inserted fact count is wrong (`src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs:153`).

### Current Mapping Read (API path)

1. `GET /v1/mappings/{source_system}/{source_key}` requires policy `resolve-current-party` (`src/Bluto.Api/MappingApi.cs:105`).
2. `ClaimsBlutoAuthorizationPolicy` builds `MappingRequestContext` from the `tenant_id` / `authorized_tenant_id` claims and the `x-correlation-id` / `x-request-id` headers.
3. `MappingApi.ResolveCurrentPartyAsync` maps outcomes to status codes: 401 unauthenticated, 403 missing tenant, 404 concealed (unauthorized tenant), 404 not found, 400 `ArgumentException`, 503 `InvalidOperationException`, 200 success. The response is a `MappingApiResponse` or `MappingApiErrorEnvelope` (snake_case JSON).
4. `CurrentMappingQueryService.ResolveAsync` re-validates scope and calls `ICurrentMappingRepository.ResolveCurrentAsync`.

### Outbox Publishing

1. `OutboxPublisher.DrainAsync(OutboxPublisherBatch)` claims pending facts per tenant (`IOutboxStore.ClaimPendingAsync`).
2. Facts are ordered by `CreatedAt` then `EventId`. Duplicate event IDs are suppressed, facts are validated (`IOutboxContractValidator`), then published through `IOutboxTransport`, or through `OutboxTransport.Disabled` when `BrokerEnabled` is false.
3. Failures move to `MarkPendingRetryAsync` or, at `MaxAttempts`, to `MarkDeadLetteredAsync`. Metrics are named `bluto.outbox.*`.

### Repository Validation

1. `Bluto.Validation.Cli` (`src/Bluto.Validation.Cli/Program.cs`) parses `--root`, `--evidence-dir`, `--report`.
2. `RepositoryValidator.Validate` loads controlled documents (metadata tables), gates, the LIR registry, and the RGTM dependency graph, then runs the metadata, identity, cross-reference, dependency, authority, and traceability validators.
3. It writes `repository/evidence/rvec/implementation-wp-001-gate-0N.json` and `implementation/validation/report.json`. The exit code is 1 when any mandatory finding exists.

**State Management:**
- Authoritative state lives only in PostgreSQL (`identity_resolution.*`, `integration_outbox.outbox_facts`). The in-memory repositories stand in for it in tests and slices.
- Domain aggregates are mutable classes with private setters and an optimistic `Version` counter that increments on each mutation.

## Key Abstractions

**Party aggregate:**
- Purpose: Stable identity with source links and lifecycle (active/merged/retired).
- Examples: `implementation/src/Bluto.Domain/Identity/IdentityModel.cs`
- Pattern: Aggregate root. Use `Party.Rehydrate(...)` for persisted state, `EstablishSourceLink` for new links, and `LoadSourceLink` for hydration.

**Ports:**
- Purpose: Isolate persistence/transport from use cases.
- Examples: `IPartyRepository`, `ICurrentMappingRepository` (Application); `IOutboxStore`, `IOutboxTransport`, `IOutboxContractValidator`, `ISourceTokenCollisionRegistry` (Worker).
- Pattern: Async interfaces with a `CancellationToken` as the last parameter.

**OutboxFact:**
- Purpose: Integration event envelope persisted with state.
- Examples: `OutboxFact.PartyCreated`, `OutboxFact.SourceLinkEstablished` in `IdentityResolutionService.cs`. Schemas in `implementation/contracts/schemas/outbox-fact.v1.schema.json`.
- Pattern: Static factory methods on a sealed record.

**Controlled Document:**
- Purpose: Governed Markdown artifact identified by `Document ID` (`BLUTO-...-NNN`) and `Artifact ID` (`ART-BLUTO-...-vX.Y.Z`) in a metadata table.
- Examples: every file under `00-baseline/` and `docs/`.
- Pattern: Cross-references use Document IDs, not filenames (README "Document control").

## Entry Points

**Bluto.Api:**
- Location: `implementation/src/Bluto.Api/Program.cs`
- Triggers: HTTP (ASP.NET Core minimal API). `public partial class Program` exposes it to `WebApplicationFactory` tests.
- Responsibilities: `AddBlutoMappingApi()` + `MapBlutoMappingApi()`.

**Bluto.Worker:**
- Location: `implementation/src/Bluto.Worker/` (class library SDK, no `Program.cs` or host)
- Triggers: Invoked by tests only. Container settings exist in the csproj.
- Responsibilities: `ScheduledResolutionWorker.ProcessAsync`, `OutboxPublisher.DrainAsync`.

**Bluto.Validation.Cli:**
- Location: `implementation/src/Bluto.Validation.Cli/Program.cs`
- Triggers: `dotnet run --project implementation/src/Bluto.Validation.Cli -- --root .`, run from the `publish-bluto/` root and in CI (`.github/workflows/wp001-validation.yml`, `wp002-ci.yml`).
- Responsibilities: Governance gates GATE-01..06 and evidence emission.

**Flyway migrations:**
- Location: `scripts/windows/Invoke-BlutoFlyway.ps1`, config `implementation/tools/flyway/flyway.local.conf`
- Triggers: Manual (`-Command info|validate|migrate|repair`), with credentials from `BLUTO_FLYWAY_*` env vars.

**Contract tooling:**
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

**What happens:** Code queries `identity_resolution.*` without calling `SetTenantContextAsync` first.
**Why it's wrong:** The RLS policies from `V002__wp013_privacy_tenant_lifecycle.sql` are `force`d. Without `bluto.tenant_id` set, queries return nothing or leak scope assumptions.
**Do this instead:** Open the connection, call `SetTenantContextAsync(connection, tenantId, ct, transaction)`, then query, as every method in `PostgresPartyRepository` does.

### Writing state and outbox facts separately

**What happens:** Party/link rows are inserted, then facts are published or inserted in a later step.
**Why it's wrong:** It breaks the transactional-outbox preserved decision and lets state and events diverge.
**Do this instead:** Use the single atomic SQL statement in a `Serializable` transaction and verify the inserted-fact count (`PostgresIdentityResolutionSql.AtomicCreatePartyWithInitialLinkAndOutboxFacts`).

### Leaking existence across tenants

**What happens:** An unauthorized tenant is told 403 or "forbidden" for a resource that exists.
**Why it's wrong:** It reveals cross-tenant existence.
**Do this instead:** Return the concealed 404 `SCOPE_FORBIDDEN_CONCEALED` (`MappingApi.ResolveCurrentPartyAsync`).

### Editing the root kit instead of `publish-bluto/`

**What happens:** Code or doc edits land in `C:\Users\OMack\bluto\docs\...`.
**Why it's wrong:** The root copy is not version-controlled and has diverged from the git repo.
**Do this instead:** Edit under `publish-bluto/` and regenerate `SHA256SUMS.txt` / `release-manifest.json` when release-controlled files change.

## Error Handling

**Strategy:** Exceptions by type, translated at the edge.

**Patterns:**
- `ArgumentException` = invalid input (API → 400), `UnauthorizedAccessException` = tenant scope violation (API → concealed 404), `InvalidOperationException` = invariant violation or dependency issue (API → 503), `DBConcurrencyException` = atomic write mismatch.
- Domain-level business outcomes (cross-tenant collision, same-source conflict) come back as `IdentityResolutionResult.Outcome` strings, not as exceptions.
- The outbox publisher catches all non-cancellation exceptions per fact and converts them to retry or dead-letter state with `SafeErrorCode(ex)`.

## Cross-Cutting Concerns

**Logging:** An `Action<string>` delegate taking `snake_case_event key=value` lines (e.g. `identity_resolution_completed outcome=created`). The API adapts it to `ILogger.LogInformation("{BlutoMappingOutcome}", ...)`. Never log tokens or raw identifiers.
**Validation:** Guard clauses at the start of each use case. JSON Schema contract validation (`JsonSchema.Net` in tests, AJV/Spectral in `tools/contracts`). Governance gates via `Bluto.Validation.Cli`.
**Authentication:** ASP.NET Core authorization policy `resolve-current-party` (`RequireAuthenticatedUser`). Tenant claims `tenant_id` / `authorized_tenant_id`. No authentication scheme is configured in `Program.cs`.
**Observability:** OpenTelemetry API `ActivitySource`/`Meter` named `Bluto.Worker.Outbox` and the worker resolution telemetry name. Correlation and request IDs propagate through headers and commands.

---

*Architecture analysis: 2026-10-24*
