# Bluto Master Implementation Plan

| Metadata | Value |
|---|---|
| Document ID | BLUTO-IMPL-MASTER-PLAN-001 |
| Artifact ID | ART-BLUTO-IMPL-MASTER-PLAN-001-v0.1.0 |
| Version | 0.1.0 |
| Status | Proposed |
| Owner | Enterprise Architecture |
| Baseline | Implementation Planning Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO_IDENTITY_SPINE_v0.1, BLUTO-IMPL-TECH-SUMMARY-001, BLUTO-ADR-0001, BLUTO-ADR-0002, BLUTO-ADR-0003, BLUTO-ADR-0004, BLUTO-ADR-0005, BLUTO-ADR-0006, BLUTO-ADR-0007, BLUTO-ADR-0008, BLUTO-ADR-0009, BLUTO-ADR-0010, BLUTO-ADR-0011, BLUTO-ADR-0012, BLUTO-ADR-0013, BLUTO-ADR-0014, BLUTO-ADR-0015, BLUTO-ADR-0016, BLUTO-ADR-0017 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Reader And Action

This plan is for internal implementation engineers, reviewers, and release owners. After reading it, an engineer should be able to select the next approved work package, create a branch, add contract and test artifacts first, then implement only the production code explicitly authorized by that package.

## Planning Rules

Implementation must proceed documentation-first, architecture-first, contracts-first, tests-first, and code-last. A work package is not ready for production code until its governing documents, allowed file scope, contract artifacts, test obligations, security controls, tenant-isolation obligations, observability requirements, quality gates, expected RVEC evidence, and rollback plan are complete.

Each work package must preserve these upstream decisions: modular-monolith-first; separate API, worker, and stewardship entry points; managed PostgreSQL; transactional outbox; HTTP/JSON interactive APIs; deterministic scheduled batch resolution; tenant isolation; Key Vault-backed HMAC-SHA-256; no raw strong identifiers at rest; and no probabilistic matching in v1.

## Global Prohibited Changes

Do not implement golden-record, master-customer, survivorship, or "best value" behavior. Do not persist, log, or expose raw strong identifiers. Do not expose HMAC tokens externally. Do not implement fuzzy, probabilistic, machine-learned, or sub-threshold automatic matching in v1. Do not implement query-time identity inference. Do not create mandatory microservices. Do not create one database per bounded context. Do not allow direct cross-context writes. Do not disable audit, authorization, tenant filtering, or contract validation behind feature flags.

## Global Validation Commands

Use the commands listed in each work package. Where the command is not yet executable because the package that creates the toolchain has not run, the work package must create or document the missing command before claiming completion.

Baseline repository gates are `GATE-01` Metadata Validation, `GATE-02` Identity Validation, `GATE-03` Cross-Reference Validation, `GATE-04` Dependency Validation, `GATE-05` Authority Validation, `GATE-06` Traceability Validation, `GATE-07` Release Validation, `GATE-08` Evidence Validation, `GATE-09` Certification Validation, and `GATE-10` Repository Audit.

## Work-Package Sequence

### WP-001 - Repository Tooling And Validation Baseline

**Objective:** Establish the implementation repository skeleton, tool manifests, validation scripts, and quality-gate commands without implementing Bluto behavior.

**Governing Document IDs:** `BLUTO-IMPL-TECH-SUMMARY-001`, `BLUTO-ADR-0001`, `BLUTO-ADR-0008`, `BLUTO-ADR-0010`, `BLUTO-ADR-0011`, `BLUTO-ENG-REPO-001`, `BLUTO-ENG-CODE-001`, `BLUTO-ENG-SUPPLY-001`, `BLUTO-AI-VALIDATE-001`.

**Files allowed to change:** `implementation/README.md`, `implementation/src/**`, `implementation/tests/**`, `implementation/tools/**`, `implementation/validation/**`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, `.config/dotnet-tools.json`, `repository/evidence/rvec/**`.

**Prohibited changes:** No API endpoints, domain behavior, database migrations, cloud resources, production secrets, generated clients, or runtime service code beyond empty projects and build/test scaffolding.

**Contract artifacts required first:** A repository implementation layout note and package inventory. No public API or event contract changes are allowed in this package.

**Tests required before production code:** A smoke test proving the empty solution test runner executes; static-analysis verification for nullable and analyzer settings; package restore audit; tool-version verification.

**Security controls:** Pin .NET SDK policy, central package management, lockfile policy, warnings-as-errors baseline, dependency audit enabled, no secrets in configuration samples.

**Tenant-isolation obligations:** Create tenant-isolation checklist placeholders for later packages; do not implement tenant behavior yet.

**Observability requirements:** Define logging and OpenTelemetry package placeholders only; no runtime telemetry export.

**Acceptance criteria:** Clean solution skeleton exists; no production behavior exists; toolchain commands run locally; package inventory cites approved ADRs; repository remains clean after validation.

**Quality Gates and validation commands:** `git status --short`; `dotnet --version`; `dotnet restore`; `dotnet build`; `dotnet test`; `dotnet format --verify-no-changes`; `dotnet list package --vulnerable --include-transitive`; repository gates `GATE-01` through `GATE-06`.

**Expected RVEC evidence:** `repository/evidence/rvec/implementation-wp-001-gate-01.json` through `implementation-wp-001-gate-06.json`, plus build/test command transcript references.

**Rollback strategy:** Revert the scaffolding commit. No data, contracts, or deployed resources exist.

### WP-002 - CI, Supply Chain, And Evidence Automation

**Objective:** Add CI workflows and evidence automation for build, test, formatting, dependency audit, CodeQL, Trivy, SBOM, and RVEC collection.

**Governing Document IDs:** `BLUTO-ADR-0011`, `BLUTO-ADR-0012`, `BLUTO-ADR-0013`, `BLUTO-ENG-SUPPLY-001`, `BLUTO-SEC-SDLC-001`, `BLUTO-OPS-BUILD-001`, `BLUTO-OPS-RELEASE-001`, `BLUTO-TEST-GATE-001`.

**Files allowed to change:** `.github/workflows/**`, `.github/dependabot.yml`, `.github/codeql/**`, `implementation/tools/**`, `implementation/validation/**`, `repository/evidence/rvec/**`, `README.md` only for CI status documentation.

**Prohibited changes:** No application behavior, migrations, contract semantics, cloud deployment, production credentials, or branch-protection bypass.

**Contract artifacts required first:** CI evidence schema or RVEC mapping note for implementation packages.

**Tests required before production code:** Workflow dry-run or local script equivalent for build/test/format/audit; evidence generation tests; scanner configuration validation.

**Security controls:** Pin third-party actions by SHA where feasible; use least-privilege `GITHUB_TOKEN`; enable Dependabot; require CodeQL and Trivy outputs; no long-lived cloud credentials.

**Tenant-isolation obligations:** CI must reserve a named gate for tenant-isolation tests once code exists.

**Observability requirements:** CI must publish test, scan, and evidence artifacts with correlation to commit SHA and work-package ID.

**Acceptance criteria:** Pull request checks exist for build, tests, format, dependency audit, SAST, container scan placeholder, SBOM placeholder, and RVEC evidence validation.

**Quality Gates and validation commands:** `gh workflow list`; `dotnet build`; `dotnet test`; `dotnet format --verify-no-changes`; `dotnet list package --vulnerable --include-transitive`; `trivy fs .`; repository gates `GATE-01` through `GATE-08`.

**Expected RVEC evidence:** `implementation-wp-002-gate-01.json` through `implementation-wp-002-gate-08.json`, CI run URL, scan artifact references, SBOM placeholder reference.

**Rollback strategy:** Disable or revert workflow files and scanner configuration. No runtime resources or data are affected.

### WP-003 - Contract Tooling And Compatibility Harness

**Objective:** Establish OpenAPI, JSON Schema, Spectral, Ajv, and compatibility-check commands before adding or changing contracts.

**Governing Document IDs:** `BLUTO-ADR-0006`, `BLUTO-ADR-0007`, `BLUTO-CONTRACT-ARCH-001`, `BLUTO-CONTRACT-API-001`, `BLUTO-CONTRACT-SCHEMA-001`, `BLUTO-CONTRACT-COMPAT-001`, `BLUTO-TEST-CONTRACT-001`.

**Files allowed to change:** `implementation/contracts/**`, `implementation/tests/Contracts/**`, `implementation/tools/**`, `docs/04-contracts/schemas/**` only for non-semantic lint fixes approved by contract owners, `repository/evidence/rvec/**`.

**Prohibited changes:** No new endpoint semantics, no schema field additions/removals, no production code, no generated clients committed as authoritative contracts.

**Contract artifacts required first:** Contract validation README, schema lint rules, OpenAPI lint rules, compatibility baseline procedure.

**Tests required before production code:** Tests that validate existing OpenAPI and JSON Schema files; negative tests for malformed examples; compatibility test that compares current baseline to itself.

**Security controls:** Contract lint rules must fail on raw strong identifier fields, externally exposed HMAC tokens, missing auth metadata, missing correlation headers, and unbounded additional properties where prohibited.

**Tenant-isolation obligations:** Contract rules must require tenant-scope behavior to be explicit in request context or authorization metadata and must reject public tenant enumeration fields.

**Observability requirements:** Contract examples must include correlation identifiers where applicable and document safe operational dimensions.

**Acceptance criteria:** Contract validation commands are deterministic; existing schemas pass or documented findings are opened; no contract semantics changed.

**Quality Gates and validation commands:** `npm exec spectral lint docs/04-contracts/schemas/bluto-v1.openapi.yaml`; `npm exec ajv validate -s docs/04-contracts/schemas/source-link-established.v1.schema.json`; `dotnet test --filter Contract`; repository gates `GATE-01` through `GATE-06`.

**Expected RVEC evidence:** `implementation-wp-003-gate-01.json` through `implementation-wp-003-gate-06.json`, contract lint transcript, compatibility baseline artifact.

**Rollback strategy:** Revert contract tooling files. Existing controlled contracts remain unchanged.

### WP-004 - Minimal Mapping Query Contract Completion

**Objective:** Complete the v1 contract artifacts needed for the first vertical slice: current mapping query, not-found/forbidden/degraded errors, required headers, and safe examples.

**Governing Document IDs:** `BLUTO-CONTRACT-API-001`, `BLUTO-CONTRACT-CQ-001`, `BLUTO-CONTRACT-ERROR-001`, `BLUTO-CONTRACT-SCHEMA-001`, `BLUTO-CONTRACT-CONSUMER-001`, `BLUTO-TEST-CONTRACT-001`, `BLUTO-SEC-DATA-001`.

**Files allowed to change:** `docs/04-contracts/schemas/bluto-v1.openapi.yaml`, `docs/04-contracts/schemas/**/*.schema.json`, `implementation/contracts/examples/**`, `implementation/tests/Contracts/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No production endpoint implementation; no raw source identifiers in examples; no HMAC token exposure; no new consumer write capability; no query-time matching behavior.

**Contract artifacts required first:** Updated OpenAPI contract for `ResolveCurrentParty`; JSON examples for success, absent, forbidden/concealed, stale/degraded, merged, and retired outcomes; error schema examples.

**Tests required before production code:** OpenAPI lint, JSON Schema validation, provider contract tests that initially fail until WP-009 implementation, consumer compatibility examples, raw-identifier leak tests.

**Security controls:** Required auth metadata, correlation headers, concealed existence semantics, no source-owned business attributes, no raw identifiers, no HMAC tokens.

**Tenant-isolation obligations:** Contract must express tenant-scoped authorization outcomes and prevent cross-tenant mapping enumeration.

**Observability requirements:** Contract examples include safe correlation and request IDs; operational errors distinguish degraded dependency without exposing sensitive state.

**Acceptance criteria:** Contract artifacts are complete enough for a failing provider test suite for the current mapping query.

**Quality Gates and validation commands:** `npm exec spectral lint docs/04-contracts/schemas/bluto-v1.openapi.yaml`; `npm exec ajv validate -s <schema> -d <example>`; `dotnet test --filter Contract`; repository gates `GATE-01` through `GATE-08`.

**Expected RVEC evidence:** `implementation-wp-004-gate-01.json` through `implementation-wp-004-gate-08.json`, contract diff report, example validation report.

**Rollback strategy:** Revert contract and example changes. No production code or database changes exist.

### WP-005 - Minimal Event And Outbox Contract Completion

**Objective:** Complete the event schema and outbox publication contract artifacts needed for Party creation and Source Link establishment in the first vertical slice.

**Governing Document IDs:** `BLUTO-ADR-0017`, `BLUTO-ARCH-RUNTIME-001`, `BLUTO-ARCH-INTEGRATION-001`, `BLUTO-CONTRACT-EVENT-001`, `BLUTO-CONTRACT-SCHEMA-001`, `BLUTO-TEST-CONTRACT-001`, `BLUTO-SEC-THREAT-001`.

**Files allowed to change:** `docs/04-contracts/schemas/**/*.schema.json`, `implementation/contracts/examples/events/**`, `implementation/tests/Contracts/**`, `implementation/tests/Outbox/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No broker provisioning, no production outbox code, no event payload raw identifiers, no event schema coupling to Azure Service Bus, no mandatory external broker.

**Contract artifacts required first:** `PartyCreated.v1` schema and examples, reviewed `SourceLinkEstablished.v1` examples, event envelope constraints, outbox delivery-state contract note.

**Tests required before production code:** JSON Schema validation for valid/invalid event examples; compatibility checks for event versioning; negative tests for raw identifier and HMAC token leakage.

**Security controls:** Event payload allowlist, aggregate partition key rules, no sensitive tokens, idempotent event IDs, at-least-once delivery expectations.

**Tenant-isolation obligations:** Event tenant metadata must be scoped and non-enumerative; cross-tenant authorization evidence must not be emitted unless explicitly approved later.

**Observability requirements:** Event envelope must support trace/correlation IDs, causation ID, batch ID where applicable, and outbox lag measurement.

**Acceptance criteria:** Event and outbox contracts are sufficient for failing outbox and publisher tests in later packages.

**Quality Gates and validation commands:** `npm exec ajv validate -s <event-schema> -d <event-example>`; `dotnet test --filter Contract`; `dotnet test --filter Outbox`; repository gates `GATE-01` through `GATE-08`.

**Expected RVEC evidence:** `implementation-wp-005-gate-01.json` through `implementation-wp-005-gate-08.json`, event compatibility report, leakage-scan report.

**Rollback strategy:** Revert event schemas and examples. No runtime broker or database state exists.

### WP-006 - Minimal Identity-Resolution Test Specification

**Objective:** Define and add failing tests for the first identity-resolution vertical slice before production code exists.

**Governing Document IDs:** `BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-DOM-OVERVIEW-001`, `BLUTO-DOM-AGGREGATE-001`, `BLUTO-DOM-VALUEOBJECT-001`, `BLUTO-DOM-STATE-001`, `BLUTO-TEST-DOMAIN-001`, `BLUTO-TEST-ACCEPT-001`, `BLUTO-ADR-0008`, `BLUTO-ADR-0009`.

**Files allowed to change:** `implementation/tests/Domain/**`, `implementation/tests/Application/**`, `implementation/tests/Acceptance/**`, `implementation/testdata/**`, `implementation/plan/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No production code except test-only fixtures; no database migrations; no API endpoints; no generated production types; no probabilistic matching fixture.

**Contract artifacts required first:** WP-004 and WP-005 contract artifacts must be complete. Test scenarios must cite the exact contract examples they exercise.

**Tests required before production code:** Failing domain tests for Party ID non-reuse, one active Source Link per scope, effective dating, deterministic exact-token match, ambiguous/conflicting deferral, tenant isolation negatives, raw-identifier rejection, outbox fact emission expectations; FsCheck properties for temporal interval and idempotency invariants.

**Security controls:** Test fixtures must use synthetic non-real identifiers; raw identifier examples are invalid-input fixtures only and must not be stored as accepted state.

**Tenant-isolation obligations:** Negative tests must prove tenant A cannot resolve or infer tenant B mappings and that missing tenant scope fails closed.

**Observability requirements:** Test expectations must include safe correlation, batch ID, rule version, and outcome counters for later instrumentation.

**Acceptance criteria:** Tests fail for missing production behavior and are traceable to requirements, contracts, threats, and acceptance scenarios.

**Quality Gates and validation commands:** `dotnet test --filter Domain`; `dotnet test --filter Application`; `dotnet test --filter Acceptance`; `dotnet test --filter Property`; repository gates `GATE-01` through `GATE-08`.

**Expected RVEC evidence:** `implementation-wp-006-gate-01.json` through `implementation-wp-006-gate-08.json`, failing-test transcript with expected failures, traceability matrix update.

**Rollback strategy:** Revert test artifacts. No production behavior or persisted state exists.

### WP-007 - Domain Model Minimal Vertical Slice

**Objective:** Implement only the domain model needed to pass WP-006 tests for deterministic creation of a Party and active Source Link from an authorized exact HMAC token candidate.

**Governing Document IDs:** `BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-DOM-AGGREGATE-001`, `BLUTO-DOM-VALUEOBJECT-001`, `BLUTO-DOM-EVENT-001`, `BLUTO-DOM-STATE-001`, `BLUTO-ARCH-REF-001`, `BLUTO-ADR-0001`, `BLUTO-ADR-0009`.

**Files allowed to change:** `implementation/src/Bluto.Domain/**`, `implementation/src/Bluto.Application/**` only for ports and commands needed by this slice, `implementation/tests/Domain/**`, `implementation/tests/Application/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No persistence implementation, no HTTP endpoints, no worker scheduling, no Key Vault client, no real HMAC key material, no merge/split/retirement behavior beyond explicit guard errors, no stewardship workflow.

**Contract artifacts required first:** Existing mapping and event contracts from WP-004/WP-005; no new public contracts in this package.

**Tests required before production code:** WP-006 tests must exist and fail. Add any missing edge tests before implementing behavior.

**Security controls:** Domain must model tokenized identifiers only; raw identifier types must be rejected or absent from accepted state; provenance is required for identity assertions.

**Tenant-isolation obligations:** Tenant scope is required for link creation and lookup decisions; missing or mismatched scope fails closed.

**Observability requirements:** Domain emits outcome information through domain events or result metadata, not direct logging.

**Acceptance criteria:** Domain/application tests for the minimal slice pass; no forbidden behavior is introduced; no public API or database code exists.

**Quality Gates and validation commands:** `dotnet test --filter Domain`; `dotnet test --filter Application`; `dotnet test --filter Property`; `dotnet build`; `dotnet format --verify-no-changes`; repository gates `GATE-01` through `GATE-08`.

**Expected RVEC evidence:** `implementation-wp-007-gate-01.json` through `implementation-wp-007-gate-08.json`, before/after test transcript, invariant coverage report.

**Rollback strategy:** Revert domain/application slice commit. No data migration or external contract rollback required.

### WP-008 - PostgreSQL Persistence And Migration Minimal Slice

**Objective:** Add Flyway migrations and repository implementation for the minimal Party, Source Link, and transactional outbox persistence slice.

**Governing Document IDs:** `BLUTO-ADR-0004`, `BLUTO-ADR-0005`, `BLUTO-ADR-0017`, `BLUTO-ARCH-DATA-001`, `BLUTO-ARCH-PHYSICAL-001`, `BLUTO-ARCH-RUNTIME-001`, `BLUTO-ENG-DATA-001`, `BLUTO-TEST-INTEGRATION-001`, `BLUTO-SEC-TENANT-001`.

**Files allowed to change:** `implementation/db/migrations/**`, `implementation/src/Bluto.Infrastructure.Postgres/**`, `implementation/tests/Integration/**`, `implementation/tests/Migrations/**`, `implementation/tools/flyway/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No API endpoints, no worker scheduling, no direct cross-context writes, no ORM introduction, no raw strong identifier columns, no external broker code.

**Contract artifacts required first:** Outbox event contracts from WP-005 and repository port contracts from WP-007.

**Tests required before production code:** Migration tests; Testcontainers PostgreSQL tests for constraints, tenant partition keys, effective intervals, one active link, outbox atomicity, rollback/forward-recovery, and no raw identifier columns.

**Security controls:** Least-privilege runtime and migration roles; parameterized SQL; no secrets in migrations; constraints for tenant scope and effective-date validity.

**Tenant-isolation obligations:** Schema must encode tenant scope in keys/constraints; all repository queries require tenant scope; tests prove cross-tenant negatives.

**Observability requirements:** Repository operations expose safe activity names, query outcome counters, and outbox lag dimensions without sensitive attributes.

**Acceptance criteria:** Migrations apply and validate; repository integration tests pass; outbox rows commit atomically with domain state; no contract changes are needed.

**Quality Gates and validation commands:** `flyway validate`; `flyway migrate`; `dotnet test --filter Integration`; `dotnet test --filter Migration`; `dotnet build`; repository gates `GATE-01` through `GATE-08`.

**Expected RVEC evidence:** `implementation-wp-008-gate-01.json` through `implementation-wp-008-gate-08.json`, migration checksum report, PostgreSQL integration transcript.

**Rollback strategy:** Use expand-migrate-contract where possible. Before production data, revert migration files and recreate test databases. After promoted environments, use approved forward-recovery migration; never mutate identity history manually.

### WP-009 - Current Mapping API Minimal Slice

**Objective:** Implement the `ResolveCurrentParty` HTTP/JSON endpoint over the minimal repository slice with authentication, authorization boundary hooks, tenant scoping, error semantics, and contract tests.

**Governing Document IDs:** `BLUTO-ADR-0002`, `BLUTO-ADR-0003`, `BLUTO-ADR-0006`, `BLUTO-CONTRACT-API-001`, `BLUTO-CONTRACT-CQ-001`, `BLUTO-CONTRACT-ERROR-001`, `BLUTO-SEC-IAM-001`, `BLUTO-SEC-TENANT-001`, `BLUTO-OPS-OBS-001`.

**Files allowed to change:** `implementation/src/Bluto.Api/**`, `implementation/src/Bluto.Application/**` only for query handler integration, `implementation/tests/Contracts/**`, `implementation/tests/Api/**`, `implementation/tests/Security/**`, `implementation/tests/Integration/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No write endpoints, no review endpoints, no point-in-time endpoint unless separately packaged, no query-time matching, no raw source identifiers persisted or logged, no bypass for unauthenticated local calls.

**Contract artifacts required first:** WP-004 OpenAPI and examples must be complete and passing schema validation.

**Tests required before production code:** Provider contract tests, authorization tests, cross-tenant forbidden/concealed tests, absent mapping tests, degraded dependency tests, correlation-header tests, raw-input logging guard tests.

**Security controls:** Authentication middleware, authorization policy abstraction, idempotency not applicable to GET, concealed existence, rate-limit hook, input validation, structured error contract.

**Tenant-isolation obligations:** Tenant claim/scope must be required before repository access; all responses must be tenant-scoped; cross-tenant enumeration is forbidden.

**Observability requirements:** OpenTelemetry spans for request and repository call; metrics for status/outcome/latency; logs include correlation ID and safe outcome only.

**Acceptance criteria:** API contract tests pass; security negatives pass; current mapping query returns only governed mappings; P95 target is measured in local integration performance smoke.

**Quality Gates and validation commands:** `dotnet test --filter Contract`; `dotnet test --filter Api`; `dotnet test --filter Security`; `dotnet test --filter Integration`; `dotnet build`; `dotnet format --verify-no-changes`; repository gates `GATE-01` through `GATE-09`.

**Expected RVEC evidence:** `implementation-wp-009-gate-01.json` through `implementation-wp-009-gate-09.json`, OpenAPI provider report, security negative report, latency smoke report.

**Rollback strategy:** Revert API slice. If deployed, route traffic back to previous image; no schema rollback required beyond WP-008 compatibility.

### WP-010 - Scheduled Resolution Worker Minimal Slice

**Objective:** Implement a deterministic scheduled worker path that processes synthetic candidate batches into Party/Source Link state and outbox facts using the minimal domain and persistence slices.

**Governing Document IDs:** `BLUTO-ADR-0002`, `BLUTO-ADR-0016`, `BLUTO-ADR-0017`, `BLUTO-ARCH-RUNTIME-001`, `BLUTO-ARCH-INTEGRATION-001`, `BLUTO-SEC-CRYPTO-001`, `BLUTO-TEST-PERF-001`, `BLUTO-OPS-RUNBOOK-001`.

**Files allowed to change:** `implementation/src/Bluto.Worker/**`, `implementation/src/Bluto.Application/**` only for resolution orchestration, `implementation/tests/Worker/**`, `implementation/tests/Integration/**`, `implementation/tests/Security/**`, `implementation/testdata/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No live source adapters, no external broker requirement, no streaming resolution, no probabilistic matching, no raw strong identifier storage, no stewardship case implementation except explicit deferral record placeholder if already contracted.

**Contract artifacts required first:** Candidate input test fixture contract, event schemas from WP-005, and domain/application tests from WP-006/WP-007.

**Tests required before production code:** Worker tests for deterministic ordering, idempotent rerun, ambiguous/conflicting deferral, tenant-scope failure, outbox atomicity, duplicate page handling, safe logging, and batch metrics.

**Security controls:** Synthetic HMAC token inputs only; Key Vault abstraction remains a port unless real integration is separately packaged; no secrets in worker config; source spoofing checks at adapter boundary.

**Tenant-isolation obligations:** Batch partitioning and repository calls require tenant scope; cross-tenant candidate collisions fail closed or defer as governed.

**Observability requirements:** Batch run spans, candidate counts by safe outcome, outbox lag, retry counters, failed partition counters, and correlation from scheduled trigger to writes.

**Acceptance criteria:** Worker processes deterministic synthetic batches reproducibly; API can read resulting mappings; outbox contains valid facts; ambiguous/conflicting cases do not create active links.

**Quality Gates and validation commands:** `dotnet test --filter Worker`; `dotnet test --filter Integration`; `dotnet test --filter Security`; `dotnet test --filter Contract`; `dotnet build`; repository gates `GATE-01` through `GATE-09`.

**Expected RVEC evidence:** `implementation-wp-010-gate-01.json` through `implementation-wp-010-gate-09.json`, deterministic rerun transcript, outbox validation report, batch observability report.

**Rollback strategy:** Disable worker schedule and roll back worker image. Preserve database history; compensate only through approved lifecycle/stewardship workflows, not direct mutation.

### WP-011 - Outbox Publisher And Optional Service Bus Adapter

**Objective:** Add an outbox publisher that marks events for delivery and optionally publishes to Azure Service Bus when external push delivery is approved for v1.

**Governing Document IDs:** `BLUTO-ADR-0017`, `BLUTO-CONTRACT-EVENT-001`, `BLUTO-ARCH-INTEGRATION-001`, `BLUTO-OPS-OBS-001`, `BLUTO-SEC-THREAT-001`, `BLUTO-TEST-INTEGRATION-001`.

**Files allowed to change:** `implementation/src/Bluto.Worker/**`, `implementation/src/Bluto.Infrastructure.Messaging/**`, `implementation/tests/Outbox/**`, `implementation/tests/Integration/**`, `implementation/tests/Contract/**`, `implementation/infra/**` only if optional Service Bus is explicitly approved, `repository/evidence/rvec/**`.

**Prohibited changes:** No event schema changes, no mandatory external broker, no exactly-once delivery claims, no payload sensitive data, no direct event publication outside outbox state.

**Contract artifacts required first:** Event schemas and examples from WP-005; optional Service Bus adapter decision flag if external broker is enabled.

**Tests required before production code:** Publisher idempotency tests, duplicate delivery tests, schema validation before publish, retry/dead-letter behavior, outbox lag metrics, Service Bus adapter tests if enabled.

**Security controls:** Scoped workload identity for broker access, encrypted transport, payload allowlist, dead-letter audit, no raw identifiers.

**Tenant-isolation obligations:** Event routing must not leak tenant information across unauthorized consumers; tenant metadata must remain scoped and contract-approved.

**Observability requirements:** Outbox lag, publish attempts, duplicate suppressions, dead-letter counts, publish latency, and correlation IDs.

**Acceptance criteria:** Outbox publisher safely drains test events; duplicate handling is proven; optional Service Bus path is adapter-only and can be disabled without domain changes.

**Quality Gates and validation commands:** `dotnet test --filter Outbox`; `dotnet test --filter Integration`; `dotnet test --filter Contract`; `trivy fs .`; repository gates `GATE-01` through `GATE-09`.

**Expected RVEC evidence:** `implementation-wp-011-gate-01.json` through `implementation-wp-011-gate-09.json`, duplicate-delivery report, broker-disabled test report.

**Rollback strategy:** Disable publisher execution or broker adapter. Keep outbox records for replay after fix; do not delete undispatched facts except through approved retention policy.

### WP-012 - Deployment, Runtime Configuration, And Operational Smoke

**Objective:** Package and deploy the minimal API and worker slice to nonproduction Azure infrastructure with Terraform, container images, managed PostgreSQL, Key Vault references, and OpenTelemetry export.

**Governing Document IDs:** `BLUTO-ADR-0012`, `BLUTO-ADR-0014`, `BLUTO-ADR-0015`, `BLUTO-ADR-0016`, `BLUTO-OPS-INFRA-001`, `BLUTO-OPS-RELEASE-001`, `BLUTO-OPS-OBS-001`, `BLUTO-OPS-DR-001`, `BLUTO-SEC-IAM-001`, `BLUTO-SEC-CRYPTO-001`.

**Files allowed to change:** `.github/workflows/**`, `implementation/infra/**`, `implementation/deploy/**`, `implementation/src/**` only for configuration binding and health endpoints, `implementation/tests/Smoke/**`, `implementation/runbooks/**`, `repository/evidence/rvec/**`.

**Prohibited changes:** No production environment rollout, no hard-coded secrets, no public database endpoints, no manual portal-only resources, no disabling auth/tenant controls for smoke tests.

**Contract artifacts required first:** Operational health contract, configuration contract, and runbook draft for migration failure, API degradation, outbox lag, and key-vault outage.

**Tests required before production code:** Terraform validation, configuration validation tests, container build tests, smoke tests for health, mapping read, worker dry run, telemetry export, and Key Vault access failure behavior.

**Security controls:** Managed workload identity, Key Vault-backed secrets, private endpoints where available, scoped database roles, no shared human credentials, TLS, image scanning.

**Tenant-isolation obligations:** Nonproduction seed data must include at least two tenants and cross-tenant negative smoke checks.

**Observability requirements:** OpenTelemetry traces, metrics, and logs visible in the configured backend; dashboard or query links for API latency, batch completion, outbox lag, and errors.

**Acceptance criteria:** Nonproduction deployment succeeds from immutable images; smoke tests pass; telemetry appears; rollback to previous image or disabled worker is documented and exercised.

**Quality Gates and validation commands:** `terraform fmt -check`; `terraform validate`; `dotnet publish`; `dotnet test --filter Smoke`; `trivy image <image>`; `gh run watch`; repository gates `GATE-01` through `GATE-10`.

**Expected RVEC evidence:** `implementation-wp-012-gate-01.json` through `implementation-wp-012-gate-10.json`, Terraform plan reference, image digest/SBOM, smoke-test transcript, telemetry screenshot or query reference.

**Rollback strategy:** Roll back container app revisions, disable worker schedule, restore prior Terraform state through reviewed plan, and preserve database history. Database forward recovery follows WP-008 migration rules.

## Reader-Test Notes

A cold reader should start at WP-001 unless repository tooling already exists and evidence proves it. The first package that permits production behavior is WP-007, and it is blocked by contract and failing-test packages. The first externally callable behavior is WP-009. The first scheduled resolution behavior is WP-010. Production rollout is outside this plan until nonproduction evidence and release approval exist.
