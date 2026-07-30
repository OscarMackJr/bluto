# Bluto Data Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-DATA-001 |
| Artifact ID | ART-BLUTO-ARCH-DATA-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-RUNTIME-001, BLUTO-DOM-CANONICAL-001, BLUTO-DOM-REPOSITORY-001, BLUTO-DOM-REVIEW-001 |
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

This document defines authoritative data ownership, storage patterns, temporal semantics, integrity, retention, and access boundaries.

## 2. Data ownership

| Data | Authority |
|---|---|
| Party identifier and status | Bluto Identity Resolution |
| Party Source Link and provenance | Bluto Identity Resolution |
| Merge/split lineage | Bluto Identity Lifecycle |
| Rule definitions and versions | Bluto Rule Management |
| Review cases and decisions | Bluto Stewardship |
| Audit and compliance evidence | Bluto Identity Governance |
| Names, addresses, balances, customer status | Source systems |
| Strong-identifier HMAC key | Platform Key Vault |

## 3. Relational model posture

PostgreSQL is selected for v1 because transactions, constraints, effective-dated queries, UUID identity, auditability, and operational maturity align with the workload. The physical schema derives from approved aggregates but does not collapse aggregate boundaries.

## 4. Temporal model

Authoritative records preserve:

- `effective_from` and nullable `effective_to`;
- `asserted_at` and `asserted_by`;
- rule and ruleset version;
- processing correlation;
- status and supersession/rejection lineage.

Active intervals are half-open and non-overlapping for a given source record and tenant scope. Corrections append or close records; they do not rewrite historical meaning.

## 5. Integrity controls

- stable UUID identifiers;
- uniqueness for active source links within tenant scope;
- check constraints for status-dependent fields;
- foreign keys inside ownership boundaries;
- merge target cannot equal source Party;
- effective end must be greater than effective start;
- immutable published rule versions;
- append-only review decisions and audit evidence.

## 6. Token data

HMAC tokens are restricted data. Token columns are never exposed to hometown or stewardship unless necessary and authorized. Token comparison is exact. Key identifiers and tokenization version are stored, but key material is not.

## 7. Read models

Optimized projections may support:

- current source-key mapping;
- point-in-time lookup;
- merge-chain resolution;
- coverage metrics;
- stewardship queue;
- audit evidence search.

Projections are rebuildable from authoritative state and outbox facts. They do not become alternate authorities.

## 8. Retention and deletion

Identity lineage, review decisions, and rule provenance are retained according to legal and records policy because they support historical explanation. Source payloads and transient normalization data are not retained. Deletion requests affecting source-owned attributes remain with source systems; Bluto evaluates only whether mapping or evidence retention must be restricted or tombstoned under policy.

## 9. Backup and recovery

Backups are encrypted, access-controlled, tested, and retained according to Stream 8. Point-in-time recovery must preserve consistency between authoritative tables and outbox state.

## 10. Migration rules

Migrations are forward-compatible with the active and immediately previous application version. Destructive changes use expand-migrate-contract sequencing. Historical semantics are never changed by a schema migration.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
