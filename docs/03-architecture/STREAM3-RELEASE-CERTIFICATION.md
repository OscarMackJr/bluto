# Stream 3 Release Certification

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-RELEASE-001 |
| Artifact ID | ART-BLUTO-ARCH-RELEASE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-REVIEW-001, BLUTO-ARCH-CONSISTENCY-001, BLUTO-ARCH-DEPENDENCY-001, BLUTO-DOM-REVIEW-001 |
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


## 1. Release identity

- Release: `Enterprise_Starter_Kit_Stream3_v1.0`
- Stream: 3 — Architecture
- Certification date: 2026-07-30
- Status: Certified for baseline
- Authority: Enterprise Architecture operating under Stream 1 governance

## 2. Included artifact classes

- reference and vision architecture;
- drivers, constraints, and quality scenarios;
- logical, physical, runtime, integration, data, and deployment architecture;
- technology decisions and cross-cutting concerns;
- stakeholder views and architecture traceability;
- architecture review and consistency certification;
- repository dependency graph.

## 3. Certification statement

The Stream 3 package provides a complete production architecture for the deterministic Bluto v1 identity spine. It is consistent with the authoritative specification, preserves approved domain semantics, defines implementation boundaries and operating behavior, and is suitable as the controlling architecture input for Streams 4–9 and autonomous Codex implementation.

## 4. Known deferrals

Product-specific language/runtime, cloud, broker, CI/CD, and observability selections are intentionally deferred. Probabilistic matching, fuzzy matching, organization hierarchy resolution, and streaming resolution remain outside the charter.

## 5. Baseline rule

After publication, these artifacts are immutable except through approved change control. Downstream documents may add detail but shall not contradict this release.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
