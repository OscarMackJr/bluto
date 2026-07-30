# Bluto Architectural Constraints

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-CONSTRAINT-001 |
| Artifact ID | ART-BLUTO-ARCH-CONSTRAINT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-DRIVER-001, BLUTO-ARCH-REF-001, BLUTO-DOM-AGGREGATE-001, BLUTO-DOM-CANONICAL-001, BLUTO-DOM-REVIEW-001 |
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

This document records mandatory constraints. They are design inputs, not preferences.

## 2. Domain constraints

- Party is an identity construct, not a customer profile.
- A source record has at most one active Party Source Link at an instant.
- `party_id` is immutable and never reused.
- merge, split, retirement, and link changes preserve prior identifiers and effective-dated history.
- probabilistic and fuzzy auto-linking are outside v1.
- identity is resolved before downstream consumption.

## 3. Data constraints

- raw government, tax, or equivalent strong identifiers shall not be stored, logged, cached, or included in events.
- strong-identifier matching uses HMAC-SHA-256 tokens generated with a key stored in the platform key vault.
- tenant scope is persisted on Party and link data and enforced on reads and writes.
- source-owned attributes such as names, addresses, balances, and statuses are not persisted as canonical Party attributes.
- the store must support point-in-time resolution from effective dates without reconstruction from mutable logs.

## 4. Runtime constraints

- v1 resolution is scheduled incremental batch with an on-demand trigger.
- source access is read-only, replica-preferred, resumable, and governed by a source-load budget.
- processing must be idempotent at batch, candidate, and state-transition levels.
- a failed aggregate transition leaves authoritative state unchanged.
- publication of externally visible facts must be atomic with authoritative state or use a transactionally coupled outbox.

## 5. Integration constraints

- hometown reads Bluto through a versioned, contract-validated interface.
- hometown does not write Bluto and does not infer identity in Cube models.
- external consumers never access internal context tables directly.
- internal contexts do not write another context's schema.
- event transport is not part of domain semantics.

## 6. Security constraints

- all workloads use managed workload identity where available.
- Tier-0 key material is never exposed to application logs, database configuration, or repository artifacts.
- reviewer and administrative actions require strong authentication, authorization, and immutable audit.
- cross-tenant authorization must be explicit, narrow, time-bounded where possible, and recorded.
- production support access follows least privilege and break-glass governance.

## 7. Deployment constraints

- the initial topology must be operable by a small team.
- environment configuration is externalized and immutable per release.
- schema migration is automated, ordered, reversible where feasible, and compatible with rolling deployment rules.
- production data is never copied into lower environments without approved de-identification.

## 8. Documentation and Codex constraints

Codex shall not invent:

- new bounded contexts, aggregates, repositories, or persisted business attributes;
- direct database integration between components;
- unversioned APIs or events;
- probabilistic matching;
- cross-tenant matching policy;
- technology substitutions that contradict approved ADRs.

Any such need is an architecture change request.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
