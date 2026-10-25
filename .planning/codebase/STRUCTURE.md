# Codebase Structure

**Analysis Date:** 2026-10-24

## Directory Layout

```
C:\Users\OMack\bluto\                 # Workspace (NOT a git repo) - unpacked certified Starter Kit v2.0
├── .planning/codebase/               # GSD codebase map (this document)
├── 00-baseline/                      # Repository Baseline A (constitutional standards) - root copy
├── contracts/vendored/olive/         # Vendored external contract (Olive identity-review queue item)
├── docs/                             # Streams 1-9 controlled docs (root copy) + 01-Foundation/, Stream1/, releases/ (root-only)
├── prompts/                          # Root-only: BigBuild.md, getstarted.txt (AI-agent build cookbook/prompts)
├── releases/                         # ENTERPRISE-RELEASE-CERTIFICATION.md
├── repository/                       # Machine registry projections (root copy)
├── specs/                            # Authoritative specs (Identity Spine v0.1/v0.1.1, ARCH-PRINCIPLES-00)
├── README.md, RELEASE-NOTES.md       # Kit README / release notes
├── release-manifest.json             # Per-file sha256 + bytes manifest (REL-ENTERPRISE-2.0.0)
├── SHA256SUMS.txt                    # Checksums for release-controlled files
└── publish-bluto/                    # GIT REPOSITORY - the active, implementable codebase
    ├── .github/                      # CI: workflows/wp001-validation.yml, wp002-ci.yml; codeql/; dependabot.yml; CODEOWNERS
    ├── 00-baseline/                  # Baseline A standards (audit, certification, governance, quality-gates, release, reviews, standards, validation)
    ├── contracts/vendored/           # VENDORED.md (source + sha256) and olive/ contract
    ├── docs/                         # Normative controlled Markdown, Streams 0-9 (see below)
    ├── implementation/               # .NET 10 solution and implementation artifacts
    ├── releases/                     # ENTERPRISE-RELEASE-CERTIFICATION.md, BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md
    ├── repository/                   # Generated machine registry (LIR, RGTM, gates, schemas, RVEC evidence, metrics)
    ├── scripts/windows/              # Invoke-BlutoFlyway.ps1
    ├── specs/                        # BLUTO_IDENTITY_SPINE_v0.1.1.md
    ├── Directory.Build.props         # net10.0, nullable, warnings-as-errors, doc generation
    ├── Directory.Packages.props      # Central package versions
    ├── global.json                   # .NET SDK 10.0.204 (rollForward latestFeature)
    ├── .editorconfig, .gitattributes, .gitignore
    └── README.md, RELEASE-NOTES.md, SHA256SUMS.txt, release-manifest.json
```

### `implementation/`

```
implementation/
├── Bluto.Validation.sln              # Single solution containing ALL src + test projects
├── README.md                         # WP-001/WP-002 local commands
├── src/
│   ├── Bluto.Domain/Identity/IdentityModel.cs
│   ├── Bluto.Application/Identity/   # IdentityResolutionService.cs, CurrentMappingQuery.cs, InMemoryCurrentMappingRepository.cs
│   ├── Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs
│   ├── Bluto.Api/                    # Program.cs, MappingApi.cs (Web SDK, container bluto-api)
│   ├── Bluto.Worker/                 # Resolution/ScheduledResolutionWorker.cs, Outbox/OutboxPublisher.cs (container bluto-worker)
│   └── Bluto.Validation.Cli/         # Program.cs, RepositoryValidator.cs (governance gate CLI)
├── tests/
│   ├── Api/                          # Bluto.Api.Tests - route contract + ResolveCurrentParty behavior
│   ├── Bluto.Identity.Tests/         # Domain/application/acceptance specs, SliceHarness.cs, contract schema tests
│   ├── Bluto.Persistence.Tests/      # Migration contract, Postgres repository contract + Testcontainers live tests
│   ├── Bluto.Validation.Tests/       # RepositoryValidator + CI configuration tests
│   ├── Contracts/                    # Bluto.Contracts.Tests - OpenAPI/event/outbox contract completion
│   ├── Outbox/                       # Bluto.Outbox.Tests
│   ├── Smoke/                        # Bluto.Smoke.Tests - operational smoke + runtime config contracts
│   └── Worker/                       # Bluto.Worker.Tests
├── contracts/                        # baselines/bluto-v1.openapi.yaml, schemas/*.v1.schema.json, examples/, fixtures/
├── db/
│   ├── migrations/                   # Flyway V###__description.sql
│   └── callbacks/afterConnect.sql
├── decisions/                        # TECHNOLOGY-SELECTION-SUMMARY.md
├── deploy/                           # configuration/ (runtime config schema + sample), contracts/ (health), smoke/ (smoke plan)
├── infra/terraform/                  # main.tf, variables.tf, outputs.tf, versions.tf (deployment contract locals/outputs)
├── intake/                           # Architecture intake report, authority index, blockers, proposed breakdown
├── plan/                             # MASTER-IMPLEMENTATION-PLAN.md, STATE.md, WORK-PACKAGE-INDEX.yaml
├── runbooks/                         # wp-012-operational-smoke.md
├── testdata/                         # candidate-batch.v1.schema.json, synthetic-candidate-batch.valid.json
├── tools/
│   ├── contracts/                    # npm: Spectral + AJV (package.json, .spectral.yaml)
│   └── flyway/flyway.local.conf
└── validation/                       # ci-evidence.schema.json, report.json (validator output)
```

### `docs/` (Streams)

| Dir | Stream | Content |
|---|---|---|
| `docs/00-authoritative/` | Authority | `BLUTO_IDENTITY_SPINE_v0.1.md` |
| `docs/01-foundation-governance/` | 1 | Vision, scope, principles, glossary, `adrs/` (ADR-0000..0017 + index/lifecycle/template), governance/, release/, reviews/ |
| `docs/02-domain-model/` | 2 | Aggregates, bounded contexts, entities, value objects, events, state models |
| `docs/03-architecture/` | 3 | Logical/physical/runtime/data/integration/deployment architecture, `assets/` (.dot/.png/.svg) |
| `docs/04-contracts/` | 4 | API/event/error/schema standards, `schemas/` (OpenAPI + JSON Schemas, normative source) |
| `docs/05-security/` | 5 | Threat model, tenant isolation, crypto/secrets, data classification |
| `docs/06-engineering-standards/` | 6 | Coding, naming, error handling, logging, migration, supply-chain standards |
| `docs/07-testing/` | 7 | Test strategy/architecture, contract/acceptance/security/perf testing |
| `docs/08-devops/` | 8 | Build pipeline, environments, observability, DR, release operations |
| `docs/09-ai-factory/` | 9 | Agent roles, guardrails, prompt/work-package standard, human approval gates |

Each stream dir has `README.md`, `*-TRACEABILITY.md`, `STREAMn-CONSISTENCY-REVIEW.md`, `STREAMn-RELEASE-CERTIFICATION.md`, `release/`, `reviews/`.

## Directory Purposes

**`implementation/src/`:**
- Purpose: Production C# code, one project per layer/entry point
- Contains: `Bluto.<Layer>[.<Tech>]/` project folders; code grouped by bounded-context subfolder (`Identity/`, `Outbox/`, `Resolution/`)
- Key files: `Bluto.Domain/Identity/IdentityModel.cs`, `Bluto.Application/Identity/IdentityResolutionService.cs`, `Bluto.Api/MappingApi.cs`

**`implementation/tests/`:**
- Purpose: xUnit v3 test projects, one per concern
- Contains: `*Tests.cs`, harnesses (`SliceHarness.cs`, `RepositoryFixture.cs`)
- Key files: `tests/Bluto.Identity.Tests/Wp006IdentityResolution*SpecificationTests.cs`

**`implementation/contracts/`:**
- Purpose: Implementation-side machine contracts consumed by tests
- Contains: `schemas/<event>.v1.schema.json`, `examples/<name>.v1.example.json`, `examples/events/`, `baselines/bluto-v1.openapi.yaml`, `fixtures/`

**`repository/`:**
- Purpose: Machine-readable registry projections of controlled Markdown
- Contains: `constitution/*.yaml`, `lir/logical-identity-registry.yaml` + `document-index.json`, `rgtm/dependency-graph.json` + `repository-governance-traceability.yaml`, `validation/quality-gates.yaml`, `schemas/metadata.schema.json` + `rvec.schema.json`, `evidence/rvec/*.json` (~181 files), `metrics/repository-health.yaml`, `releases/audits/`
- Key files: `repository/lir/logical-identity-registry.yaml` (must list every Document ID), `repository/validation/quality-gates.yaml`

**`00-baseline/`:**
- Purpose: Constitutional standards governing all documents (metadata, identity, versioning, validation, certification, release)
- Key files: `standards/DOCUMENT-METADATA-STANDARD.md`, `standards/IMMUTABLE-DOCUMENT-IDENTITY-STANDARD.md`, `quality-gates/REPOSITORY-QUALITY-GATE-FRAMEWORK.md`

**`implementation/plan/`:**
- Purpose: Work-package sequencing and implementation state
- Key files: `WORK-PACKAGE-INDEX.yaml` (WP-001..WP-014, gates, preserved decisions), `STATE.md`, `MASTER-IMPLEMENTATION-PLAN.md`

## Key File Locations

**Entry Points:**
- `implementation/src/Bluto.Api/Program.cs`: ASP.NET Core minimal API host
- `implementation/src/Bluto.Validation.Cli/Program.cs`: Governance validator CLI
- `scripts/windows/Invoke-BlutoFlyway.ps1`: Migration runner wrapper
- `implementation/Bluto.Validation.sln`: Solution for build/test/format

**Configuration:**
- `Directory.Build.props`: Shared MSBuild properties
- `Directory.Packages.props`: Central NuGet versions (Dapper, Npgsql, OpenTelemetry, xunit.v3, Testcontainers, JsonSchema.Net)
- `global.json`: SDK pin
- `.editorconfig`: Formatting enforced by `dotnet format --verify-no-changes`
- `implementation/tools/flyway/flyway.local.conf`: Flyway locations/schemas (no credentials)
- `implementation/deploy/configuration/bluto-runtime-config.schema.json` + `nonproduction.sample.json`: Runtime config contract
- `.github/workflows/wp002-ci.yml`: Build/test/format/audit/validator/CodeQL/Trivy/SBOM

**Core Logic:**
- `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs`: Resolution use case, `IPartyRepository`, `OutboxFact`, `InMemoryPartyRepository`
- `implementation/src/Bluto.Infrastructure.Postgres/Identity/PostgresIdentityResolutionSql.cs`: Postgres adapter + atomic SQL
- `implementation/db/migrations/V001__identity_resolution_minimal_slice.sql`, `V002__wp013_privacy_tenant_lifecycle.sql`: Schema + RLS

**Testing:**
- `implementation/tests/*/`: See test layout above
- `implementation/testdata/`: Synthetic batch fixtures

**Authority / Specs:**
- `specs/BLUTO_IDENTITY_SPINE_v0.1.1.md`, `docs/00-authoritative/BLUTO_IDENTITY_SPINE_v0.1.md`
- `releases/BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` (draft v0.3 spec referenced by `plan/STATE.md`)

## Naming Conventions

**Files:**
- C# source: PascalCase, named for the primary type or feature (`MappingApi.cs`, `OutboxPublisher.cs`); multiple related types per file is normal (`IdentityModel.cs` holds all identity domain types)
- Tests: `<Subject>Tests.cs`; work-package specs `Wp0NN<Feature><Layer>SpecificationTests.cs`
- Projects: `Bluto.<Layer>` / `Bluto.<Layer>.<Technology>` (`Bluto.Infrastructure.Postgres`); test projects `Bluto.<Area>.Tests.csproj` (folder may be short: `tests/Api/Bluto.Api.Tests.csproj`)
- Controlled docs: UPPER-KEBAB-CASE.md (`LOGICAL-ARCHITECTURE.md`); specs `BLUTO_<NAME>_v<x.y.z>.md`; ADRs `ADR-NNNN-<STATUS>-<TOPIC>.md`
- Contracts: `<kebab-name>.v<major>.schema.json`, `<kebab-name>.v<major>.example.json`, `<operation>.<scenario>.v1.example.json`
- Migrations: Flyway `V###__snake_description.sql`
- Evidence: `repository/evidence/rvec/implementation-wp-0NN-gate-0N.json`
- Document identity (inside metadata table, not filename): `BLUTO-<AREA>-<NAME>-NNN`, artifact `ART-BLUTO-...-NNN-vX.Y.Z`

**Directories:**
- Streams: `NN-kebab-name/` (`03-architecture/`)
- Source projects: dotted PascalCase; feature subfolders PascalCase (`Identity/`, `Outbox/`, `Resolution/`)
- Implementation artifacts: lowercase (`contracts/`, `deploy/`, `runbooks/`)

**Namespaces:** `Bluto.<Layer>.<Feature>` file-scoped (`namespace Bluto.Application.Identity;`, `Bluto.Api.Mapping`, `Bluto.Infrastructure.Postgres.Identity`). Exception: Validation CLI uses `RootNamespace=Bluto.Validation`.

## Where to Add New Code

All paths relative to `C:\Users\OMack\bluto\publish-bluto\`. Start from a work package entry in `implementation/plan/WORK-PACKAGE-INDEX.yaml`; add failing tests before code.

**New domain concept (e.g., merge/split rules, rule management):**
- Primary code: `implementation/src/Bluto.Domain/<Context>/` (no package/project references)
- Tests: `implementation/tests/Bluto.Identity.Tests/` (or a new `Bluto.<Context>.Tests` added to `Bluto.Validation.sln`)

**New use case / port:**
- Implementation: `implementation/src/Bluto.Application/<Context>/` — command/query `sealed record`, `I<Name>Repository` interface, `<Name>Service` class taking ports + `Action<string> log`
- In-memory adapter for tests alongside it

**New Postgres adapter:**
- Implementation: `implementation/src/Bluto.Infrastructure.Postgres/<Context>/` — Dapper + `NpgsqlDataSource`, always call tenant context before queries
- Schema: new `implementation/db/migrations/V003__<description>.sql` (never edit applied migrations); add RLS policy for tenant tables
- Tests: `implementation/tests/Bluto.Persistence.Tests/` (contract + Testcontainers live tests)

**New API endpoint:**
- Implementation: `implementation/src/Bluto.Api/` — extend `Add.../Map...` extension methods; register in `Program.cs`
- Contract first: `docs/04-contracts/schemas/bluto-v1.openapi.yaml` and `implementation/contracts/baselines/bluto-v1.openapi.yaml`, examples in `implementation/contracts/examples/`
- Tests: `implementation/tests/Api/`, `implementation/tests/Contracts/`

**New event / outbox fact:**
- Schema: `implementation/contracts/schemas/<event>.v1.schema.json` (+ `docs/04-contracts/schemas/`), examples in `implementation/contracts/examples/events/`
- Factory: static method on `OutboxFact` in `implementation/src/Bluto.Application/Identity/IdentityResolutionService.cs`
- Tests: `implementation/tests/Contracts/EventOutboxContractCompletionTests.cs`, `implementation/tests/Outbox/`

**New worker job:**
- Implementation: `implementation/src/Bluto.Worker/<JobArea>/` with static `Telemetry` class (`ActivitySource` + `Meter`, metric names `bluto.<area>.<metric>`)
- Tests: `implementation/tests/Worker/`

**New governance gate / validator rule:**
- Implementation: `implementation/src/Bluto.Validation.Cli/RepositoryValidator.cs`; gate definitions in `repository/validation/quality-gates.yaml`
- Tests: `implementation/tests/Bluto.Validation.Tests/`

**New controlled document:**
- Place in the owning stream dir under `docs/`, include the full 14-field metadata table (`00-baseline/standards/DOCUMENT-METADATA-STANDARD.md`), register the Document ID in `repository/lir/logical-identity-registry.yaml`, dependencies in `repository/rgtm/dependency-graph.json`; run the validator

**Utilities:**
- No shared utility project exists. Keep helpers `private static` in the consuming class; promote to `Bluto.Application` only when shared across entry points.

**Infrastructure / deployment:**
- `implementation/infra/terraform/`, `implementation/deploy/`, runbooks in `implementation/runbooks/`

## Special Directories

**`repository/evidence/rvec/`:**
- Purpose: Validator-emitted RVEC evidence JSON
- Generated: Yes (by `Bluto.Validation.Cli`)
- Committed: Yes

**`implementation/validation/report.json`:**
- Purpose: Latest validator report
- Generated: Yes
- Committed: Yes (`*.local.json` variants are gitignored)

**`implementation/src/*/obj/`, `tests/*/obj/`:**
- Purpose: MSBuild intermediate output
- Generated: Yes
- Committed: No (`.gitignore`: `bin/`, `obj/`, `TestResults/`)

**`implementation/tools/contracts/node_modules`:**
- Purpose: npm deps for Spectral/AJV
- Generated: Yes
- Committed: No (`tools/contracts/.gitignore`)

**`contracts/vendored/`:**
- Purpose: Copies of external (Olive) contracts with source + SHA-256 recorded in `VENDORED.md`
- Generated: No
- Committed: Yes — do not edit; re-vendor and update hash

**`release-manifest.json` / `SHA256SUMS.txt` (both roots):**
- Purpose: Release integrity manifest of controlled files
- Generated: Yes (release process)
- Committed: Yes (in `publish-bluto/`)

**Workspace root (`C:\Users\OMack\bluto\` excluding `publish-bluto/`):**
- Purpose: Original unpacked certified kit plus root-only prompts/specs
- Generated: No
- Committed: Not under version control — treat as read-only reference

---

*Structure analysis: 2026-10-24*
