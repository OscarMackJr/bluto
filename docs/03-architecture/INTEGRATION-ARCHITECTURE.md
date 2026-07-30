# Bluto Integration Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-INTEGRATION-001 |
| Artifact ID | ART-BLUTO-ARCH-INTEGRATION-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-RUNTIME-001, BLUTO-ARCH-PHYSICAL-001, BLUTO-DOM-CANONICAL-001, BLUTO-DOM-REVIEW-001 |
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

This document defines integration styles, ownership, reliability, and boundary protections.

## 2. Integration catalogue

| Integration | Direction | Style | Authority |
|---|---|---|---|
| Nexus CRM → Bluto | inbound | incremental read-only batch | CRM owns attributes and keys |
| Intrepid → Bluto | inbound | tenant-scoped incremental batch | Intrepid owns borrower data |
| Ledger → Bluto | inbound | incremental read-only batch | Ledger owns customer/account data |
| Key Vault ↔ Bluto | request/response | private platform API | Key Vault owns key material |
| Bluto → hometown | outbound/read | versioned mapping query or governed replicated view | Bluto owns identity mapping |
| Reviewer ↔ Bluto | interactive | authenticated web/API | Bluto owns review record |
| Bluto → telemetry | outbound | sanitized logs, metrics, traces | telemetry owns operational copies |
| Bluto events → consumers | outbound | transactional outbox then broker where required | Bluto owns event semantics |

## 3. Source ingestion contracts

Each source adapter must define:

- source-system identifier and tenant semantics;
- stable source key and change/watermark strategy;
- available referential keys;
- normalization rules and input version;
- extraction page limits, retry policy, and source-load budget;
- deletion/retirement semantics;
- source data classification;
- reconciliation and completeness evidence.

Adapters translate source vocabulary into canonical candidate models. Source DTOs do not enter domain modules.

## 4. Hometown consumption contract

The preferred contract exposes:

- current Party mapping by source system, source key, and tenant;
- bulk incremental link changes using effective dates and high-water marks;
- point-in-time Party resolution;
- merge-chain resolution;
- ruleset and schema versions;
- explicit not-found, retired, unauthorized, and degraded outcomes.

Hometown is read-only. Consumer caching may improve availability but must retain effective dates and version metadata.

## 5. Event publication

Domain facts are written to a transactional outbox with authoritative state. The publisher transforms internal facts into versioned integration events. Delivery is at least once; consumers deduplicate by event identifier. Integration events exclude raw identifiers and internal aggregate structure.

## 6. Reliability controls

- bounded retries with jitter;
- circuit breakers for remote dependencies;
- per-source rate and concurrency limits;
- replayable watermarks;
- reconciliation reports comparing extracted, processed, linked, deferred, and failed counts;
- dead-letter records with sanitized diagnostics;
- contract validation at every boundary.

## 7. Degradation policy

If Bluto mapping reads are unavailable, hometown degrades to approved single-domain behavior. It does not substitute name matching or cached data beyond approved freshness. If a source is unavailable, other source partitions continue and freshness metrics identify the stale domain.

## 8. Integration ownership

Every adapter has an owner, contract version, compatibility policy, SLO, runbook, and test suite. Changes to source-key or tenant semantics require cross-team contract review.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
