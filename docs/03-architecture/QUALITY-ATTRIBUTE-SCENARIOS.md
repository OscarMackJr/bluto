# Bluto Quality Attribute Scenarios

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-QAS-001 |
| Artifact ID | ART-BLUTO-ARCH-QAS-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-DRIVER-001, BLUTO-ARCH-CONSTRAINT-001, BLUTO-DOM-TRACE-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing references

This document is subordinate to the authoritative Bluto Enterprise Identity Spine v0.1 and the immutable Stream 1 and Stream 2 baselines. It preserves the following upstream invariants:

- Bluto owns identity mapping, stable Party identifiers, link lineage, rules, confidence, review decisions, and merge/split history.
- Bluto does not own source-system business attributes and shall not become a golden-record or master-data store.
- v1 resolution is deterministic, incremental batch, and tenant-scoped by default.
- raw strong identifiers are never persisted in Bluto; matching uses HMAC-SHA-256 tokens created with a Tier-0 key held outside the database.
- Party identifiers are never reused, and historical resolution is effective-dated and reproducible.
- hometown consumes Bluto mappings read-only and never performs ad hoc identity matching.


## 1. Purpose

This document converts architecture qualities into measurable scenarios. Stream 7 shall derive executable tests from these scenarios.

## 2. Scenario catalogue

| ID | Attribute | Stimulus and environment | Required response | Measure |
|---|---|---|---|---|
| QAS-COR-01 | Correctness | Ambiguous candidates are processed in production batch | No active link is created; review case is opened | 100% of seeded ambiguous cases deferred |
| QAS-TEN-01 | Tenant isolation | Same strong-token value appears in two tenants without authorization | Records remain separate; attempt is audited | Zero cross-tenant links |
| QAS-HIS-01 | Historical truth | Parties merge after a prior answer was issued | Point-in-time query returns pre-merge identity | Exact reproduction for any retained date |
| QAS-SEC-01 | Data protection | Store, logs, events, backups, and configuration are inspected | No raw strong identifiers or key material found | Zero prohibited values |
| QAS-AVL-01 | Availability | Bluto mapping interface is unavailable | hometown degrades to approved single-domain behavior | No ad hoc matching; alert within 5 minutes |
| QAS-RES-01 | Resilience | Worker fails after partial candidate processing | Retry completes without duplicate active links | Idempotent recovery; no invariant violation |
| QAS-PER-01 | Batch performance | Daily incremental volume at 2× forecast enters normal window | Processing completes inside window | P95 batch completion within 60 minutes unless capacity plan revises |
| QAS-QRY-01 | Query performance | Consumer requests current mapping by source key | Correct mapping returned | P95 ≤ 250 ms within service boundary |
| QAS-AUD-01 | Auditability | Auditor selects any active or historical link | Full provenance is retrievable | Evidence returned within 5 minutes operationally |
| QAS-CHG-01 | Modifiability | New deterministic rule version is introduced | Existing history remains reproducible | No mutation of prior rule versions or links |
| QAS-OBS-01 | Observability | Conflict rate rises above baseline | Alert identifies source, tenant scope, rule version, batch | Detection within 10 minutes |
| QAS-RPO-01 | Recoverability | Primary data store is lost | Restore preserves lineage and audit | RPO ≤ 15 minutes; RTO ≤ 4 hours target |
| QAS-DEP-01 | Deployability | Compatible application release is deployed | No consumer outage or partial schema state | Automated rollback or forward recovery |
| QAS-PRV-01 | Privacy | Support engineer investigates a failed match | Diagnostic evidence excludes raw source PII | 100% redaction policy compliance |

## 3. Quality budgets

- **Correctness budget:** no accepted false merge is tolerated as routine error.
- **Tenant-safety budget:** zero unauthorized cross-tenant identity links.
- **Data-exposure budget:** zero raw strong identifiers in Bluto-controlled persistence.
- **Availability objective:** 99.9% monthly for mapping reads after production stabilization; batch freshness is monitored separately.
- **Freshness objective:** v1 mappings available within the approved batch cadence; on-demand runs are controlled, not a substitute for streaming.
- **Recovery objective:** target RPO 15 minutes and RTO 4 hours, subject to Stream 8 operational validation.

## 4. Scenario governance

A scenario may be changed only with impact analysis across Architecture, Contracts, Security, Testing, and DevOps. Numeric targets are initial production targets and shall be revised from measured capacity through change control, never silently relaxed.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
