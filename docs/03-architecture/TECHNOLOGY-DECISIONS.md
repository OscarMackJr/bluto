# Bluto Technology Decisions

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-TECH-001 |
| Artifact ID | ART-BLUTO-ARCH-TECH-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-DRIVER-001, BLUTO-ARCH-CONSTRAINT-001, BLUTO-ARCH-DEPLOY-001, BLUTO-DOM-REVIEW-001 |
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

This document records v1 technology decisions at the architecture level. Product versions belong to Engineering and DevOps standards.

## 2. Decisions

### TD-01 — Modular monolith first

**Decision:** Implement bounded contexts as enforced modules in one repository and release train, with API, worker, and stewardship process entry points.

**Rationale:** Small team, strong transaction requirements, lower operational burden, and no evidence that independent services are needed.

### TD-02 — PostgreSQL authoritative store

**Decision:** Use a dedicated managed PostgreSQL database.

**Rationale:** Relational integrity, effective dating, transactional merge/split operations, mature backup, and transparent query behavior.

### TD-03 — Transactional outbox

**Decision:** Persist publishable facts in an outbox within the same transaction as domain state.

**Rationale:** Prevent state/event divergence while allowing transport choice later.

### TD-04 — Managed key vault and HMAC-SHA-256

**Decision:** Use platform-managed key vault and HMAC-SHA-256 for exact-match strong identifier tokens.

**Rationale:** Required by the authoritative specification and avoids raw identifier accumulation.

### TD-05 — Scheduled worker orchestration

**Decision:** Use platform scheduling and resumable worker partitions, not a streaming platform, for v1 resolution.

**Rationale:** Matches required cadence and minimizes complexity.

### TD-06 — HTTP/JSON for interactive contracts

**Decision:** Use versioned HTTP/JSON APIs for stewardship and current/point-in-time mapping queries unless Stream 4 proves another style necessary.

**Rationale:** Broad interoperability, explicit schemas, and operational simplicity.

### TD-07 — Broker-neutral integration events

**Decision:** Define integration-event schemas independently of a broker. Initial transport may be a managed queue/topic if external consumers require events.

**Rationale:** Domain semantics should not be coupled to transport.

### TD-08 — OpenTelemetry-compatible telemetry

**Decision:** Instrument traces, metrics, and structured logs through OpenTelemetry-compatible interfaces.

**Rationale:** Vendor portability and unified correlation.

### TD-09 — Infrastructure as code and immutable artifacts

**Decision:** All environment infrastructure and deployment artifacts are version-controlled and reproducible.

### TD-10 — No distributed cache as authority

**Decision:** Caches may accelerate reads but never own mapping truth or temporal lineage.

## 3. Deferred decisions

Specific cloud provider, language/runtime, web framework, ORM, migration tool, broker, CI platform, and observability backend are deferred to Stream 6/8 selection criteria. Any selection must realize these decisions without changing architecture.

## 4. Replacement criteria

A technology may be replaced when compatibility, security, supportability, and migration evidence demonstrate equal or better conformance. Replacement does not authorize semantic change.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
