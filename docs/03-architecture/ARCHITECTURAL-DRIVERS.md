# Bluto Architectural Drivers

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-DRIVER-001 |
| Artifact ID | ART-BLUTO-ARCH-DRIVER-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-VISION-001, BLUTO-ARCH-REF-001, BLUTO-DOM-TRACE-001, BLUTO-DOM-REVIEW-001 |
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

This document identifies the forces that shape the architecture and establishes their priority when trade-offs arise.

## 2. Business drivers

| Driver | Architectural consequence |
|---|---|
| Auditable cross-system identity | Persist stable Party identifiers, source links, rule provenance, reviewer identity, and effective dating |
| Near-zero false merges | Deterministic rules only for v1; ambiguity enters stewardship |
| Cross-domain consumption by hometown | Governed read contract; no query-time matching |
| Regulatory and disclosure sensitivity | Tenant isolation, minimal data retention, immutable audit evidence |
| Autonomous implementation by Codex | Explicit boundaries, decisions, invariants, contracts, and tests |
| Small initial team | Minimize deployables and platform complexity while preserving modularity |

## 3. Quality drivers in priority order

1. Correctness and tenant safety.
2. Security and data minimization.
3. Auditability and historical reproducibility.
4. Maintainability and architectural clarity.
5. Availability and recoverability.
6. Operational observability.
7. Performance and scalability.
8. Delivery speed.

A lower-ranked quality may not be optimized by weakening a higher-ranked quality without an approved ADR.

## 4. Functional drivers

The platform must:

- ingest candidate identity keys from Nexus CRM, Intrepid, and ledger sources;
- normalize and tokenize sensitive identifiers in memory;
- execute versioned deterministic match rules in priority order;
- create and maintain Party and Party Source Link state;
- queue non-matches and conflicts for review;
- record review outcomes and evidence;
- execute merge, split, and retirement while preserving lineage;
- answer current and point-in-time identity queries;
- expose governed mappings to hometown;
- emit coverage, conflict, queue, latency, and failure metrics.

## 5. Regulatory and risk drivers

The architecture assumes that an incorrect cross-tenant or cross-person link may create a disclosure incident. Consequently:

- tenant identity is part of authorization and matching scope, not optional metadata;
- strong identifiers are tokenized before persistence;
- production logs shall not include raw identifiers or source payloads;
- manual decisions require attributable reviewer identity and evidence;
- administrative override paths are narrow, audited, and reversible through explicit domain operations.

## 6. Organizational drivers

The architecture must support separate accountability for Enterprise Architecture, Data Stewardship, Security, Platform Engineering, and application engineering. Logical ownership is documented even where the initial deployment is consolidated.

## 7. Technology-neutral drivers

The architecture requires relational transactions, effective-dated queries, reliable event publication, secrets isolation, and observable batch execution. Product selection is constrained by these capabilities but remains subordinate to architecture.

## 8. Trade-off rules

When options conflict:

- prefer missing a match over asserting an unsafe match;
- prefer explicit review over heuristic automation;
- prefer one well-structured deployable over multiple weakly governed services;
- prefer append-only lineage over destructive correction;
- prefer compatibility-preserving change over synchronized consumer upgrades;
- prefer operationally proven components over novel infrastructure.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
