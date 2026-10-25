# Roadmap: Bluto — WP-014 Closure and Runnable POC

## Overview

The roadmap runs two governed work packages in order. WP-014 (Phases 1-4) certifies the WP-013 remediation. It reconstructs the missing review, writes the contract artifacts first, writes tests before production code, and ends with gates, RVEC evidence and an honest go/no-go. WP-015 (Phases 5-8) is defined in the plan documents first, then builds the runnable local POC in two code phases. It finishes with the scripted end-to-end demo (EVT-04) as the final success signal. No phase mixes WP-014 and WP-015 requirements.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

### Work Package WP-014: Post-Remediation Review Closure and POC Readiness Certification

- [ ] **Phase 1: Review Reconstruction and Closure Contracts** - Rebuild the missing review, then write the closure matrix, readiness plan and defer records (WP-014)
- [ ] **Phase 2: Findings S2-S4 Closure** - Tests first, then the minimal code to close or pin down optimistic concurrency, hydration fidelity and match metadata (WP-014)
- [ ] **Phase 3: Regression and Security Hardening** - Tests first for B1-B4, leakage, RLS, idempotency and contracts, then the SEC-01..03 fixes (WP-014)
- [ ] **Phase 4: Gates, RVEC Evidence and Go/No-Go** - Full parity suite, checksum drift, RVEC gate-01..10 and the readiness report (WP-014)

### Work Package WP-015: Runnable Local POC (to be defined)

- [ ] **Phase 5: WP-015 Definition** - Add WP-015 to the work-package index and master plan, documentation only (WP-015)
- [ ] **Phase 6: Authenticated API on Postgres** - Dev-secret and seed bootstrap, a Postgres-backed tenant-scoped API, and gating CI scans (WP-015)
- [ ] **Phase 7: Worker Resolution and Event Delivery** - Hosted worker, Postgres outbox, working publisher and a local consumer (WP-015)
- [ ] **Phase 8: End-to-End Demo** - `docker compose up` brings up the whole stack and a scripted demo proves seed to consumer (WP-015)

## Phase Details

### Phase 1: Review Reconstruction and Closure Contracts
**Work package**: WP-014
**Goal**: Reviewers have a reconstructed, clearly marked implementation review and the contract artifacts that WP-014 requires before any test or code is written
**Depends on**: Nothing (first phase)
**Requirements**: REVW-01, REVW-02, CLOS-01, CLOS-02, CLOS-03
**Success Criteria** (what must be TRUE):
  1. Reviewer can open `releases/BLUTO_IMPLEMENTATION_REVIEW_v0.1.md`, see it marked as reconstructed, see its listed sources (master plan, WP-013 commit `d59dc9b` and RVEC evidence, 2026-10-24 codebase map), and see it follow the Hometown review format
  2. Reviewer can find B1-B4 and S2-S4 each defined with evidence and touched files
  3. Reviewer can open the residual review closure matrix and see every B and S finding with a closed or deferred status and the test or record that proves it
  4. Reviewer can open the POC readiness evidence plan and see every WP-014 validation command with the evidence it must produce
  5. Reviewer can open a defer record (owner, risk rationale, reopen condition) for each S2-S4 item not planned for closure. Records are drafted now and confirmed in Phase 2.
**Plans**: TBD

### Phase 2: Findings S2-S4 Closure
**Work package**: WP-014
**Goal**: S2, S3 and S4 are each either closed by tests and a minimal code change or explicitly deferred with tests that pin down current behaviour
**Depends on**: Phase 1
**Requirements**: FIND-01, FIND-02, FIND-03
**Success Criteria** (what must be TRUE):
  1. A test shows a concurrent Party update is rejected through version-checked writes, and the existing-party link path bumps the persisted version (or the deferral record is backed by tests pinning current behaviour)
  2. A test shows a Party rehydrated from Postgres keeps each source link's persisted status and ID, so a superseded or rejected link no longer causes false conflicts, and replay causes no command side effects
  3. A test shows `match_method` and `confidence` round-trip through persistence (or are pinned and deferred with a record)
  4. The closure matrix is updated so every S2-S4 row matches the actual outcome
**Plans**: TBD

### Phase 3: Regression and Security Hardening
**Work package**: WP-014
**Goal**: Executable tests prove the WP-013 controls and tenant isolation hold, and the three known security gaps are closed without weakening any control
**Depends on**: Phase 2
**Requirements**: REGR-01, REGR-02, REGR-03, REGR-04, REGR-05, SEC-01, SEC-02, SEC-03
**Success Criteria** (what must be TRUE):
  1. Running the regression suite fails if any of B1-B4 is reopened, and no raw strong identifier appears in persistence, logs or API responses
  2. With the application's tenant predicate omitted, RLS blocks cross-tenant rows on both reads and writes, running as a non-superuser role
  3. The same idempotency key used by two tenants neither replays nor leaks, and cross-tenant collision results stay concealed
  4. `PUBLIC` cannot execute the `SECURITY DEFINER` cross-tenant function, and `integration_outbox` has forced RLS by tenant, both through forward-only migrations with tests
  5. A test that reuses one pooled connection across two tenants shows no tenant context carries over, and contract examples validate against their schemas with identical duplicate copies
**Plans**: TBD

### Phase 4: Gates, RVEC Evidence and Go/No-Go
**Work package**: WP-014
**Goal**: WP-014 is certified with traceable evidence and an honest POC readiness decision
**Depends on**: Phase 3
**Requirements**: GATE-A, GATE-B, GATE-C, GATE-D
**Success Criteria** (what must be TRUE):
  1. The full WP-014 validation command list passes locally and in CI, including Flyway migrate and validate on PostgreSQL 16
  2. The validator passes GATE-07 with checksum and CRLF drift fixed and baselined document content unchanged
  3. `implementation-wp-014-gate-01.json` through `gate-10.json` exist and each traces to a commit SHA and to WP-014
  4. The POC readiness report records a go/no-go decision with reasons
**Plans**: TBD

### Phase 5: WP-015 Definition
**Work package**: WP-015
**Goal**: WP-015 "Runnable Local POC" exists as a governed work package so WP-015 code can start legitimately
**Depends on**: Phase 4
**Requirements**: WP15-01
**Success Criteria** (what must be TRUE):
  1. `implementation/plan/WORK-PACKAGE-INDEX.yaml` and `MASTER-IMPLEMENTATION-PLAN.md` both contain a WP-015 entry
  2. The entry lists allowed files, prohibited changes, required tests, security controls, tenant-isolation obligations, acceptance criteria, validation commands and expected RVEC evidence
  3. The repository validator passes with the new entry, and no WP-015 code has been written
**Plans**: TBD

### Phase 6: Authenticated API on Postgres
**Work package**: WP-015
**Goal**: A developer can start a bootstrapped database and API and get real, tenant-scoped mappings from Postgres with a locally issued token, while CI starts failing on security findings
**Depends on**: Phase 5
**Requirements**: STACK-02, STACK-03, API-01, API-02, API-03, SEC-04, SEC-05
**Success Criteria** (what must be TRUE):
  1. Starting the stack generates the JWT signing key and DB role passwords at startup, and none are committed to the repo
  2. A seed step loads synthetic pre-tokenized batches for at least two tenants
  3. A caller with a local-issuer JWT gets the current mapping from `GET /v1/mappings/{source_system}/{source_key}` served from Postgres, and a smoke test starts the real `Program` with no DI overrides
  4. A missing or invalid token, or a request for another tenant's data, is rejected (401/403/404) without revealing whether the data exists
  5. `/healthz` reports liveness and readiness including database connectivity
  6. CI fails on Trivy, CodeQL and NuGet audit findings, and runs on pushes to `implementation/bootstrap`
**Plans**: TBD

### Phase 7: Worker Resolution and Event Delivery
**Work package**: WP-015
**Goal**: A hosted worker resolves batches against Postgres, and the resulting events are reliably delivered to a local consumer
**Depends on**: Phase 6
**Requirements**: WORK-01, WORK-02, WORK-03, EVT-01, EVT-02, EVT-03
**Success Criteria** (what must be TRUE):
  1. The worker starts as its own hosted process and resolves scheduled candidate batches against Postgres
  2. A batch with several new parties succeeds, and a batch with a conflict or a failing candidate classifies review outcomes correctly and continues with the remaining candidates
  3. A Postgres-backed outbox with retry and dead-letter state (forward migration) drives the publisher, and a transport failure followed by a retry sends the event before marking it published
  4. The local consumer receives `party-created` and `source-link-established` events over HTTP, validates them against the v1 schemas, and records them
**Plans**: TBD

### Phase 8: End-to-End Demo
**Work package**: WP-015
**Goal**: The core value is demonstrable on a developer machine with one command and one script
**Depends on**: Phase 7
**Requirements**: STACK-01, EVT-04
**Success Criteria** (what must be TRUE):
  1. `docker compose up` starts PostgreSQL 16, applies the Flyway migrations, and starts the API, the worker and the event consumer
  2. A scripted demo seeds a batch, the worker resolves it into Parties, the API returns the mapping for an authenticated tenant, and the consumer shows the resulting events
  3. A smoke test runs the same demo flow and passes
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8

| Phase | Work Package | Plans Complete | Status | Completed |
|-------|--------------|----------------|--------|-----------|
| 1. Review Reconstruction and Closure Contracts | WP-014 | 0/TBD | Not started | - |
| 2. Findings S2-S4 Closure | WP-014 | 0/TBD | Not started | - |
| 3. Regression and Security Hardening | WP-014 | 0/TBD | Not started | - |
| 4. Gates, RVEC Evidence and Go/No-Go | WP-014 | 0/TBD | Not started | - |
| 5. WP-015 Definition | WP-015 | 0/TBD | Not started | - |
| 6. Authenticated API on Postgres | WP-015 | 0/TBD | Not started | - |
| 7. Worker Resolution and Event Delivery | WP-015 | 0/TBD | Not started | - |
| 8. End-to-End Demo | WP-015 | 0/TBD | Not started | - |
