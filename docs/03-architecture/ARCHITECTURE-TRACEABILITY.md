# Bluto Architecture Traceability

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-TRACE-001 |
| Artifact ID | ART-BLUTO-ARCH-TRACE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-VIEWS-001, BLUTO-ARCH-CROSSCUT-001, BLUTO-DOM-REVIEW-001 |
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

This document proves that architecture elements realize approved domain requirements and identifies downstream obligations.

## 2. Requirement realization matrix

| Requirement | Domain realization | Architecture realization | Quality evidence |
|---|---|---|---|
| REQ-ID-01 one stable Party | Party Aggregate | Resolution module, PartyRepository, unique active-link constraint | QAS-COR-01 |
| REQ-ID-02 reproducible link | Rule Version, provenance values | Rule Management, tokenization version, audit/outbox | QAS-AUD-01 |
| REQ-ID-03 ambiguous never auto-links | Review Aggregate | deterministic worker and review flow | QAS-COR-01 |
| REQ-ID-04 history survives change | Merge Aggregate, effective dates | temporal model, lifecycle transaction, point-in-time query | QAS-HIS-01 |
| REQ-ID-05 no raw strong identifiers | HMAC token value object | Key Vault adapter, memory-only normalization, redaction | QAS-SEC-01 |
| REQ-ID-06 tenant boundaries | Tenant Identifier | tenant context, authorization, schema constraints | QAS-TEN-01 |
| REQ-ID-07 identifiers never reused | immutable Party Identifier | UUID policy, append-only lineage | QAS-HIS-01 |

## 3. Domain-to-architecture mapping

| Bounded context | Aggregate/model | Logical module | Physical runtime | Persistence |
|---|---|---|---|---|
| Rule Management | Rule Aggregate | Rule Management | API/worker module | `rule_mgmt` |
| Identity Resolution | Party Aggregate | Identity Resolution | API and worker | `identity_resolution` |
| Identity Lifecycle | Merge Aggregate | Identity Lifecycle | API/worker | `identity_lifecycle` |
| Stewardship | Review Aggregate | Stewardship | Stewardship Web/API | `stewardship` |
| Identity Governance | Evidence Model | Identity Governance | worker/API projection | `identity_governance` |

## 4. Architecture-to-downstream obligations

| Architecture element | Stream 4 | Stream 5 | Stream 6 | Stream 7 | Stream 8 | Stream 9 |
|---|---|---|---|---|---|---|
| Mapping API | OpenAPI and compatibility | caller and tenant policy | implementation standard | contract/performance tests | deployment/SLO | generation constraints |
| Integration events | schema registry rules | data minimization | serialization standard | consumer tests | broker/outbox ops | schema generation |
| PostgreSQL schemas | query/repository contracts | row/schema access | migration conventions | persistence tests | backup/DR | no direct SQL generation outside adapters |
| Worker partitions | trigger/job contracts | workload identity | concurrency standard | restart/idempotency tests | scheduling/autoscaling | task decomposition |
| Stewardship surface | review command/query contracts | reviewer RBAC | UI/API standards | workflow/security tests | deployment/runbook | human gate rules |
| Key Vault tokenization | tokenization port | Tier-0 controls | secret-use standard | no-raw-PII audit tests | rotation migration | prohibit secret invention |

## 5. Traceability completeness rule

No downstream contract, security control, engineering rule, test, deployment artifact, or AI instruction may exist without an upstream architecture reference. No architecture element may proceed to implementation without at least one planned verification.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
