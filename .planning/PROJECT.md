# Bluto — WP-014 Closure and Runnable POC

## What This Is

Bluto is the Fisk Road enterprise identity spine. It resolves entity (Party) identity across divisional systems ahead of time and records it, so cross-domain context has an auditable join. Identity is never inferred at query time. This GSD project takes the existing `implementation/bootstrap` codebase (WP-001 through WP-013 delivered) through WP-014 review closure and POC readiness certification. A new governed work package, WP-015, then turns the result into a POC that runs end to end on a developer machine.

## Core Value

A developer can run `docker compose up` and watch a synthetic batch resolve into Parties in PostgreSQL. An authenticated, tenant-scoped API returns those mappings, and the resulting events are delivered to a consumer. None of the existing security, tenant-isolation, or governance controls are weakened on the way.

## Requirements

### Validated

<!-- Inferred from existing code (WP-001..WP-013). See .planning/codebase/. -->

- ✓ Governed repository tooling: `Bluto.Validation.Cli` enforces GATE-01..GATE-06 and writes RVEC evidence (WP-001..WP-003)
- ✓ Contract tooling: Spectral (OpenAPI) and Ajv (event schemas) validation of contract artifacts (WP-003..WP-005)
- ✓ Domain model: Party aggregate with immutable Party IDs, source links, half-open effective intervals, merge/split/retire domain methods (WP-006)
- ✓ Deterministic identity resolution on pre-tokenized strong identifiers with tenant-scoped idempotency, concealed cross-tenant collision, and review-case deferral. No probabilistic matching (WP-006/WP-007)
- ✓ PostgreSQL persistence via Dapper/Npgsql with Flyway migrations V001/V002 and forced RLS on identity tables for reads (WP-008, WP-013)
- ✓ Current-mapping query endpoint `GET /v1/mappings/{source_system}/{source_key}`, exercised in tests with in-memory adapters (WP-009)
- ✓ Scheduled resolution worker slice and outbox publisher slice as libraries (WP-010, WP-011)
- ✓ Runtime operational smoke slice and privacy/tenant lifecycle remediation of review findings B1–B4 (WP-012, WP-013)
- ✓ CI on GitHub Actions: build, test, validator, CodeQL, Trivy, Dependabot (currently non-gating scans)

### Active

<!-- WP-014: review closure and POC readiness (certification, no new features) -->

- [ ] Reconstruct `releases/BLUTO_IMPLEMENTATION_REVIEW_v0.1.md` (missing source input), marked as reconstructed, with B1–B4 and S2–S4 rebuilt from the master plan, WP-013 commit/RVEC evidence and the codebase map
- [ ] WP-014 contract artifacts first: residual review closure matrix, POC readiness evidence plan, and explicit defer records (owner, rationale, trigger) for any S2–S4 item not closed
- [ ] S2 optimistic concurrency / aggregate version semantics: closed with tests or explicitly deferred
- [ ] S3 hydration from persisted state without command-replay side effects: closed with tests (fixes the rehydrated-source-link bug)
- [ ] S4 persisted `match_method` and `confidence`: closed or explicitly deferred
- [ ] Regression tests proving B1–B4 stay closed; raw-identifier leakage negatives; tenant RLS omitted-predicate negatives (including write paths); cross-tenant idempotency and collision concealment proofs; contract-example consistency tests
- [ ] Full local and CI parity suite passes; Flyway migrate and validate pass on PostgreSQL 16; validator and RVEC validation pass
- [ ] WP-014 RVEC evidence (gate-01..gate-10) and a POC readiness report recording an honest go/no-go

<!-- WP-015: runnable POC (new governed work package) -->

- [ ] Define WP-015 through the governed process (WORK-PACKAGE-INDEX.yaml and master plan entry, documentation-first) before any WP-015 code
- [ ] Local stack via `docker compose`: PostgreSQL 16, Flyway migrations applied, API and worker containers
- [ ] API serves real data: authentication registered in `Bluto.Api/Program.cs`, Postgres-backed `ICurrentMappingRepository`, tenant-scoped current-mapping queries
- [ ] Worker resolves a batch: hosted `Bluto.Worker` entry point, Postgres-backed `IOutboxStore`, scheduled batch resolution end to end against Postgres
- [ ] Events reach a consumer: outbox publisher actually delivers events (fixing the retry-marks-published bug and adding the outbox retry/dead-letter schema) to a local POC consumer
- [ ] Fix POC-blocking bugs from `CONCERNS.md`: multi-party batch `correlation_id` uniqueness collision, outbox retry drop, worker review misclassification and batch abort

### Out of Scope

- New end-user features or new API endpoints (point-in-time and review-evidence routes, merge/split/retire services): prohibited by WP-014, and not needed to show the POC
- Probabilistic matching: excluded from v1 by preserved decision
- Production deployment and real Azure resources (Terraform stays a contract stub): POC runs locally only
- Choosing a production message broker (BLK-007): the POC consumer is a local stand-in behind the publisher port
- Real HMAC tokenization and key custody inside Bluto: key owner is unassigned (REQ-ID-10); the POC uses synthetic pre-tokenized identifiers
- Modifying baselined (immutable) documents: changes need ADR and change control
- Reconciling the docs-only root kit at `C:\Users\OMack\bluto`: outside the repo; `publish-bluto/` is the source of truth
- Production observability backend (BLK-008)

## Context

- **Program:** Fisk Road ecosystem (`~/fiskroad`), with sibling repos hometown (knowledge graph / Answer Trace Envelope), popeye (AI governance gateway), olive (service tokens), liam and nono. Bluto was chartered 2026-07-30 in response to architecture-evaluation gap C1. Sibling integrations exist only as contracts and stubs.
- **Governance model:** documentation-first → architecture → contracts → tests → code. Each work package has allowed files, prohibited changes, required tests before production code, and RVEC evidence for GATE-01..GATE-10. Past WPs each merged as one PR into `implementation/bootstrap`.
- **Codebase state (2026-10-24 map):** .NET 10 modular monolith (`implementation/Bluto.Validation.sln`, 6 src and 9 test projects). The API has no auth scheme and no registered repository. The worker is a class library with no host. There is no Postgres `IOutboxStore` or `ICurrentMappingRepository`. See `.planning/codebase/CONCERNS.md`.
- **Known security concerns** to address or record: `SECURITY DEFINER` cross-tenant function executable by `PUBLIC`; outbox table without RLS; session-scoped tenant context on pooled connections; RLS write paths tested only as superuser; non-gating CI scans.
- **Governance drift:** 7 of 273 baselined files no longer match `SHA256SUMS.txt` (partly CRLF); duplicate OpenAPI/schema copies; CODEOWNERS paths wrong; CI `push` triggers do not include `implementation/bootstrap`.
- **Review format reference:** `~/hometown/specs/HOMETOWN_IMPLEMENTATION_REVIEW_v0.1.md` (H/B-numbered POC blockers, S-numbered non-blocking findings) is the template for the reconstructed Bluto review.
- **Open blockers:** BLK-001..BLK-022 in `implementation/intake/IMPLEMENTATION-BLOCKERS.md`. Local Trivy evidence is `BLOCKED_LOCALLY` (WP-011).

## Constraints

- **Governance**: Work packages execute in order. WP-014 changes are limited to its `allowed_files`, and WP-015 must be defined before its code. This is the repository's constitutional process, and the RVEC evidence depends on it.
- **Tech stack**: .NET 10 (SDK 10.0.204), PostgreSQL 16, Flyway, Dapper/Npgsql, xUnit v3 with Testcontainers. These are preserved decisions and the existing toolchain.
- **Security**: Fail-closed token-shape validation. No raw strong identifiers at rest or in logs. Forced RLS. Tenant-scoped idempotency. No key material or secrets in repo or config. These are WP-014 security controls and preserved decisions.
- **Migrations**: Forward-only, with no mutation of existing identity history. Rollback strategy is forward recovery.
- **Delivery**: Changes go through PRs into `implementation/bootstrap` (the branch has a PR rule). One PR per work package keeps evidence traceable to the commit SHA and WP ID.
- **Environment**: Developer machine is Windows. CRLF conversion affects checksum validation, so line endings must be handled deliberately.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Run WP-014 exactly as defined, then add a new WP-015 for runnability | WP-014's allowed files exclude Bluto.Api/Bluto.Worker and it forbids new features; keeping it pure preserves governance integrity | — Pending |
| Reconstruct the missing implementation review and mark it as reconstructed | The source input is absent everywhere (repo, root kit, sibling repos, git history); WP-014 cannot proceed without B/S definitions | — Pending |
| POC definition = compose stack + authenticated API on real data + worker batch + delivered events | User's chosen bar for "runnable" | — Pending |
| Planning docs live in `publish-bluto/.planning/` and are committed | The repo is the source of truth; the root kit is not version-controlled | ✓ Good |
| POC event consumer is a local stand-in behind the publisher port | The production broker decision (BLK-007) is open | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-10-25 after initialization*
