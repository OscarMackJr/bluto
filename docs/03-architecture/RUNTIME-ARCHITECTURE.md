# Bluto Runtime Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-RUNTIME-001 |
| Artifact ID | ART-BLUTO-ARCH-RUNTIME-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-LOGICAL-001, BLUTO-ARCH-PHYSICAL-001, BLUTO-DOM-STATE-001, BLUTO-DOM-EVENT-001, BLUTO-DOM-REVIEW-001 |
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

This document defines execution flows, transaction boundaries, concurrency behavior, and failure handling.

## 2. Scheduled resolution flow

1. Scheduler creates a Batch Run with correlation identifier, source, tenant scope, watermark, ruleset version, and load budget.
2. Source adapter reads a bounded page from the approved replica or endpoint.
3. Worker normalizes candidate values in memory.
4. Tokenization port obtains HMAC capability from Key Vault-backed infrastructure; raw strong identifiers are discarded immediately after token creation.
5. Resolution application service evaluates approved deterministic rules in priority order.
6. A transaction creates or updates Party/link state, records provenance, and writes outbox facts.
7. Ambiguous or conflicting outcomes create a Review Case and no active link.
8. Watermark advances only after the page is committed.
9. Outbox publisher delivers versioned facts; duplicate publication is safe.
10. Metrics record coverage, conflicts, queue depth, duration, and errors.

## 3. Query flow

Current and point-in-time queries authenticate the caller, authorize tenant scope, validate canonical identifiers, read a consistent mapping projection, resolve merge chains, and return a versioned response with provenance references. Queries never call source systems and never infer identity.

## 4. Review flow

A reviewer retrieves a redacted evidence package, records accept/reject/defer with rationale, and submits a command. The Review Aggregate records the immutable decision. Accepted outcomes invoke an authorized Resolution command in a separate explicit transaction with correlation to the review decision.

## 5. Merge and split flow

Lifecycle commands require elevated authorization and precondition validation. Merge:

- locks or version-checks affected Party aggregates;
- closes absorbed active links at the merge instant;
- creates successor links to the surviving Party;
- records merge lineage and outbox facts atomically.

Split:

- rejects affected historical assertions without deleting them;
- mints new Party identifiers;
- establishes new effective-dated links;
- records lineage and facts atomically.

## 6. Transaction boundaries

Atomic boundaries include:

- Party creation plus initial link and outbox;
- link supersession plus successor link and outbox;
- Review decision persistence;
- merge/split state and lineage update plus outbox;
- rule-version publication;
- batch-page watermark advancement after all page writes.

Source extraction and entire batch execution are not one database transaction.

## 7. Concurrency and idempotency

- aggregate versions detect conflicting writes;
- idempotency keys are derived from source, tenant, source key, input version, rule version, and operation;
- unique constraints enforce one active link per source key and tenant scope;
- workers claim partitions with leases;
- expired leases are recoverable;
- retries use bounded exponential backoff and dead-letter governance for persistent failures.

## 8. Failure behavior

Failures are classified as validation, authorization, invariant, dependency, transient infrastructure, or unrecoverable data conflict. No failure silently creates partial identity state. Unsafe candidates are quarantined for review or operations. Batch progress and evidence allow exact restart.

## 9. Time semantics

All authoritative timestamps use UTC. Business effective time is distinct from processing time and assertion time. The clock is injected through a port for deterministic testing. Point-in-time queries use half-open intervals `[effective_from, effective_to)`.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
