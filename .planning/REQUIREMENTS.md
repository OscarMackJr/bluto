# Requirements: Bluto — WP-014 Closure and Runnable POC

**Defined:** 2026-10-25
**Core Value:** A developer can run `docker compose up` and watch a synthetic batch resolve into Parties in PostgreSQL. An authenticated, tenant-scoped API returns those mappings, and the resulting events are delivered to a consumer. None of the existing security, tenant-isolation, or governance controls are weakened on the way.

## Work Package Placement

WP-014 may only change its `allowed_files` (Domain, Application, Infrastructure.Postgres, the Identity/Persistence/Contracts tests, migrations, testdata, plan, reviews, RVEC evidence, and the review document). Requirements that touch `src/Bluto.Api/**`, `src/Bluto.Worker/**`, `.github/**`, or compose/deploy files belong to WP-015. WP15-01 must be complete before any WP-015 code.

| Package | Categories |
|---------|-----------|
| WP-014 | REVW, CLOS, FIND, REGR, SEC-01..03, GATE |
| WP-015 | WP15, SEC-04..05, STACK, API, WORK, EVT |

## v1 Requirements

### Review Reconstruction (REVW)

- [ ] **REVW-01**: Reviewer can read `releases/BLUTO_IMPLEMENTATION_REVIEW_v0.1.md`, marked as reconstructed. It lists its sources: the master plan, the WP-013 commit `d59dc9b` and its RVEC evidence, and the 2026-10-24 codebase map. It follows the format of `~/hometown/specs/HOMETOWN_IMPLEMENTATION_REVIEW_v0.1.md`.
- [ ] **REVW-02**: The reconstructed review defines B1–B4 (POC blockers that WP-013 remediated) and S2–S4 (not blocking), each with its evidence and the files it touches.

### WP-014 Closure Artifacts (CLOS)

- [ ] **CLOS-01**: A residual review closure matrix maps every B and S finding to a status (closed or deferred) and the test or record that proves it.
- [ ] **CLOS-02**: A POC readiness evidence plan lists every WP-014 validation command and the evidence it must produce.
- [ ] **CLOS-03**: Any S2–S4 item not closed has a defer record giving the owner, the risk rationale and the condition that would reopen it.

### Findings S2–S4 (FIND)

- [ ] **FIND-01**: S2: Party updates enforce optimistic concurrency (`UPDATE … WHERE version = @expected`), and the existing-party link path bumps the version. Tests prove a concurrent update is rejected. If this is deferred instead, tests pin down the current behaviour.
- [ ] **FIND-02**: S3: Rehydrating a Party from persisted state keeps each source link's status and ID (fixing `LoadSourceLink`), with no side effects from replaying commands. Tests cover it.
- [ ] **FIND-03**: S4: `match_method` and `confidence` are persisted and round-trip, or are explicitly deferred with tests that pin down the current behaviour.

### Regression & Negative Tests (REGR)

- [ ] **REGR-01**: Executable tests prove B1–B4 controls stay closed.
- [ ] **REGR-02**: Negative tests prove no raw strong identifier appears in persistence, logs or API responses.
- [ ] **REGR-03**: Tests prove RLS blocks rows even when the application omits its tenant predicate, for both reads and writes, running as a non-superuser role.
- [ ] **REGR-04**: Tests prove that the same idempotency key used by different tenants neither replays nor leaks, and that cross-tenant collision results stay concealed.
- [ ] **REGR-05**: Contract-example consistency tests prove the examples validate against their schemas and the duplicate contract copies are identical.

### Security Hardening (SEC)

- [ ] **SEC-01**: `PUBLIC` cannot execute the `SECURITY DEFINER` cross-tenant function. Only the intended role can, and a test proves it. (WP-014)
- [ ] **SEC-02**: `integration_outbox` has forced RLS by tenant, applied in a forward-only migration with tests. (WP-014)
- [ ] **SEC-03**: Tenant context is scoped to the transaction (`SET LOCAL` or `set_config(..., true)`), so a pooled connection never carries one tenant into another request. A test proves it. (WP-014)
- [ ] **SEC-04**: Trivy, CodeQL and the NuGet vulnerability audit fail CI on findings. (WP-015)
- [ ] **SEC-05**: CI runs on pushes to `implementation/bootstrap`, not only `main`/`master`. (WP-015)

### Gates & Certification (GATE)

- [ ] **GATE-A**: The full WP-014 validation command list passes locally and in CI, including Flyway migrate and validate on PostgreSQL 16.
- [ ] **GATE-B**: Checksum and CRLF drift is fixed wherever it fails GATE-07 or any other validator. Baselined documents keep their content.
- [ ] **GATE-C**: RVEC evidence `implementation-wp-014-gate-01.json` through `gate-10.json` exists and traces to a commit SHA and to WP-014.
- [ ] **GATE-D**: The POC readiness report records a go/no-go with reasons.

### WP-015 Definition (WP15)

- [ ] **WP15-01**: WP-015 "Runnable Local POC" is added to `implementation/plan/WORK-PACKAGE-INDEX.yaml` and `MASTER-IMPLEMENTATION-PLAN.md`, with allowed files, prohibited changes, required tests, security controls, tenant-isolation obligations, acceptance criteria, validation commands and expected RVEC evidence, before any WP-015 code is written.

### Local Stack (STACK)

- [ ] **STACK-01**: `docker compose up` starts PostgreSQL 16, runs the Flyway migrations, and starts the API, the worker and the event consumer.
- [ ] **STACK-02**: The stack generates its own dev secrets (JWT signing key, DB role passwords) at startup. None are committed to the repo.
- [ ] **STACK-03**: A seed step loads synthetic, pre-tokenized candidate batches for at least two tenants.

### API (API)

- [ ] **API-01**: A caller presenting a JWT from the local issuer (tenant taken from a claim) gets current mappings from `GET /v1/mappings/{source_system}/{source_key}`, served from Postgres through a registered `ICurrentMappingRepository`.
- [ ] **API-02**: Requests that are missing a token, carry an invalid one, or ask for another tenant's data are rejected (401/403/404), without revealing whether the data exists.
- [ ] **API-03**: `/healthz` reports liveness and readiness, including database connectivity, as the health contract specifies.

### Worker (WORK)

- [ ] **WORK-01**: The worker runs as a hosted service (it has its own entry point) that resolves scheduled candidate batches against Postgres.
- [ ] **WORK-02**: A batch with several new parties succeeds; the shared `correlation_id` uniqueness collision is fixed.
- [ ] **WORK-03**: Review outcomes are classified correctly, and one failing candidate doesn't abort the batch.

### Events (EVT)

- [ ] **EVT-01**: A Postgres-backed `IOutboxStore` with retry and dead-letter columns (forward migration) drives the publisher.
- [ ] **EVT-02**: Retried events are actually sent before they're marked published.
- [ ] **EVT-03**: The local consumer container receives `party-created` and `source-link-established` events over HTTP, checks them against the v1 schemas, and records them.
- [ ] **EVT-04**: A full demo works: seed → worker resolves → API returns the mapping → consumer shows the events. It's scripted and covered by a smoke test.

## v2 Requirements

Deferred to a future release. Tracked, but not in the current roadmap.

### Hygiene

- **HYG-01**: CODEOWNERS paths match the repository layout, and the team slug is confirmed
- **HYG-02**: The duplicate OpenAPI and schema contract copies are reduced to one source of truth
- **HYG-03**: The docs-only root kit at `C:\Users\OMack\bluto` is reconciled or archived

### Platform

- **PLAT-01**: OpenTelemetry exporter and an observability backend (BLK-008)
- **PLAT-02**: Production message broker selection and adapter (BLK-007)
- **PLAT-03**: Live integration with hometown and with olive service tokens
- **PLAT-04**: Real HMAC tokenization and key custody once an owner is assigned (REQ-ID-10)

### Contract Surface

- **SURF-01**: Point-in-time mapping endpoint `/v1/mappings/{source_system}/{source_key}/at/{timestamp}`
- **SURF-02**: Review-evidence endpoint `/v1/review-evidence/{candidate_id}`
- **SURF-03**: Merge, split and retire services with persistence (BLK-013..015)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Probabilistic matching | Excluded from v1 by a preserved decision |
| Production or Azure deployment | The POC is local only; Terraform stays a contract stub |
| New end-user features or endpoints | Prohibited by WP-014; not needed for the POC |
| Modifying baselined documents | Needs ADR and change control; documents are immutable in this release |
| Raw strong identifiers anywhere | Preserved decision: no raw strong identifiers at rest or in logs |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|

**Coverage:**
- v1 requirements: 36 total
- Mapped to phases: 0
- Unmapped: 36 ⚠️

---
*Requirements defined: 2026-10-25*
*Last updated: 2026-10-25 after initial definition*
