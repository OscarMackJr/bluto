# Bluto Architecture Review

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-REVIEW-001 |
| Artifact ID | ART-BLUTO-ARCH-REVIEW-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-TRACE-001, BLUTO-ARCH-QAS-001, BLUTO-ARCH-CONSTRAINT-001, BLUTO-DOM-REVIEW-001 |
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


## 1. Review objective

Certify that Stream 3 realizes the approved domain without contradiction and is sufficient for Contracts, Security, Engineering, Testing, DevOps, and AI Factory work.

## 2. Review result

**Result: PASS — suitable for v1.0 baseline.**

## 3. Findings summary

| Severity | Count | Disposition |
|---|---:|---|
| Critical | 0 | None |
| Major | 0 | None |
| Minor | 0 | None blocking |
| Informational | 5 | Deferred to downstream streams |

Informational items:

1. language/runtime product selection is deferred to Engineering Standards;
2. cloud and CI/CD product selection is deferred to DevOps;
3. external event broker is optional until a consumer requires asynchronous publication;
4. numeric capacity targets require validation with production-like data;
5. data-stewardship ownership remains a business assignment outside architecture.

## 4. Compliance assessment

| Area | Result |
|---|---|
| Identity mapping only | Pass |
| Deterministic v1 | Pass |
| Batch-first resolution | Pass |
| Tenant-scoped by default | Pass |
| No raw strong identifiers at rest | Pass |
| Immutable Party identity and lineage | Pass |
| Effective-dated point-in-time resolution | Pass |
| hometown read-only boundary | Pass |
| Domain ownership preservation | Pass |
| Contracts-first readiness | Pass |
| Codex implementation readiness | Pass |

## 5. Key architecture decisions confirmed

- modular monolith first, with separate API/worker/stewardship processes;
- dedicated managed PostgreSQL;
- context-owned schemas and least-privilege roles;
- transactional outbox;
- HTTP/JSON interactive interfaces;
- broker-neutral integration-event semantics;
- OpenTelemetry-compatible observability;
- managed Key Vault for HMAC key custody;
- immutable infrastructure and automated migration posture.

## 6. Risks and mitigations

| Risk | Mitigation |
|---|---|
| False merge | deterministic-only automation, review deferral, invariant tests |
| Cross-tenant disclosure | explicit tenant context, authorization, constraints, audit |
| Source overload | load budgets, pagination, replica preference, backpressure |
| Event/state divergence | transactional outbox |
| Historical corruption | append-only lineage, effective dating, migration controls |
| Premature distribution | modular-monolith decision and service-extraction gate |
| AI-generated shortcut | downstream AI Factory constraints and traceability |

## 7. Downstream readiness

Streams 4–9 may proceed. They shall derive from this baseline and may not redefine its domain, topology posture, trust boundaries, or data ownership without change control.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
