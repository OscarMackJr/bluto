# Stream 3 Consistency Review

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-CONSISTENCY-001 |
| Artifact ID | ART-BLUTO-ARCH-CONSISTENCY-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-DOM-REVIEW-001 |
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


## 1. Scope

The review covered all Stream 3 architecture artifacts, diagrams, metadata, dependencies, terminology, constraints, and traceability.

## 2. Result

**PASS.** No contradiction was found within Stream 3 or against the authoritative specification and available Stream 1–2 baseline artifacts.

## 3. Validation results

| Check | Result |
|---|---|
| Stable document IDs and metadata | Pass |
| Reference architecture preserved | Pass |
| Domain bounded contexts and aggregates preserved | Pass |
| No golden-record behavior | Pass |
| Deterministic batch v1 preserved | Pass |
| Tenant isolation consistently modeled | Pass |
| HMAC key custody consistently modeled | Pass |
| Logical/physical/runtime views agree | Pass |
| Data ownership and persistence views agree | Pass |
| Integration and degradation behavior agree | Pass |
| Technology decisions realize drivers | Pass |
| Quality scenarios map to controls | Pass |
| Traceability covers REQ-ID-01 through REQ-ID-07 | Pass |
| Mermaid and asset references valid | Pass |
| Downstream dependency order explicit | Pass |

## 4. Resolved consistency choices

- “Future services” are logical candidates; v1 is a modular monolith with process separation.
- Identity Governance uses an evidence/projection model rather than a second transactional aggregate authority.
- PostgreSQL is dedicated but uses context-owned schemas rather than separate databases.
- integration facts use a transactional outbox; external broker adoption remains consumer-driven.
- hometown receives governed read contracts and may cache only under defined freshness and historical rules.

## 5. Baseline recommendation

Stream 3 is complete, internally consistent, and ready for immutable v1.0 packaging.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
