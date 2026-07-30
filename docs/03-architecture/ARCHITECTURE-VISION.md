# Bluto Architecture Vision

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-VISION-001 |
| Artifact ID | ART-BLUTO-ARCH-VISION-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-REF-001, BLUTO-DOM-REVIEW-001, BLUTO-VISION-001, BLUTO-ARCH-PRINCIPLES-001 |
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

This document translates the approved enterprise vision and domain model into a realizable system architecture. It defines the target architectural posture for the first production release and the evolution path that later streams must preserve.

## 2. Architecture vision statement

Bluto shall be a small, governed identity-mapping platform that resolves enterprise Party identity ahead of consumption through deterministic, versioned rules and preserves every assertion as reproducible, effective-dated history.

The initial system shall favor a modular application with explicit bounded-context modules, a scheduled resolution worker, a minimal stewardship surface, and a dedicated relational persistence boundary. Logical modules and contracts shall be strong enough to permit later decomposition, but operational distribution shall occur only when justified by measured scale, ownership, or resilience needs.

## 3. Target outcomes

The architecture shall deliver:

1. **Correct identity before broad coverage.** False merges are treated as disclosure incidents; unresolved candidates are deferred.
2. **Reproducibility.** Every link can be traced to inputs, normalized tokens, rule version, actor, and effective interval.
3. **Historical truth.** Point-in-time resolution survives merge, split, retirement, and rule evolution.
4. **Tenant isolation.** Cross-tenant identity resolution is denied unless an explicit authorization policy exists and is recorded.
5. **Minimal data accumulation.** Bluto persists identifiers, tokens, mappings, lineage, rules, and evidence—not customer profiles.
6. **Controlled evolution.** Stable ports, schemas, and events allow future service decomposition without reworking domain semantics.
7. **Operational legibility.** Business outcomes, queue behavior, failures, and security-relevant actions are observable and auditable.

## 4. Target architecture posture

The v1 target is:

- one Bluto application boundary with separately executable API, worker, and stewardship processes where operationally useful;
- bounded-context modules for Rule Management, Identity Resolution, Identity Lifecycle, Stewardship, and Identity Governance;
- a dedicated PostgreSQL database with context-owned schemas and strict write ownership;
- transactional persistence with an outbox for facts that must be published reliably;
- scheduled incremental batch resolution plus an authorized on-demand trigger;
- read-only source adapters, replica-preferred where available, with load budgets;
- a read contract for hometown that supports current and point-in-time mappings;
- platform key-vault, workload identity, centralized logging, metrics, traces, and immutable audit retention;
- infrastructure and deployment automation specified later by Stream 8.

## 5. Evolution strategy

Evolution shall proceed by evidence:

- scale the worker horizontally only after partition and idempotency behavior is proven;
- separate deployment units only when failure isolation, independent scaling, or ownership warrants it;
- introduce probabilistic matching only through a new data-protection design, compliance review, bounded-context decision, and major release;
- add streaming only if freshness objectives cannot be met by scheduled and on-demand batch;
- preserve all published contract and historical semantics through versioned migration.

## 6. Definition of architectural success

The architecture is successful when a new engineering team or Codex can implement Bluto without inventing domain boundaries, storage ownership, trust boundaries, lifecycle semantics, or integration behavior, and when every production identity assertion remains explainable years later.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
