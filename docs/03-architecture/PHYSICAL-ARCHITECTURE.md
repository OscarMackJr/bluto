# Bluto Physical Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-PHYSICAL-001 |
| Artifact ID | ART-BLUTO-ARCH-PHYSICAL-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-LOGICAL-001, BLUTO-ARCH-CONSTRAINT-001, BLUTO-DOM-REPOSITORY-001, BLUTO-DOM-REVIEW-001 |
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

This document maps logical responsibilities to physical runtime and persistence elements for v1.

## 2. Physical elements

| Element | Purpose |
|---|---|
| Bluto API process | Mapping queries, authorized commands, on-demand triggers, administration endpoints |
| Resolution Worker process | Scheduled extraction, normalization, tokenization, rule execution, state transitions |
| Stewardship Web/API process | Minimal reviewer queue and decision surface |
| PostgreSQL | Authoritative Party, link, rule, review, lifecycle, outbox, and audit-index persistence |
| Object archive | Long-retention signed evidence exports where required; never a business-state authority |
| Scheduler | Starts incremental jobs and maintenance operations |
| Key Vault | Stores HMAC and application secrets; accessed through workload identity |
| Telemetry platform | Central metrics, traces, sanitized logs, and alerts |
| Source connectors | Read-only adapters for Nexus CRM, Intrepid, and ledger |
| Hometown connector | Governed read interface or replicated contract view |

## 3. Persistence layout

A dedicated Bluto PostgreSQL database is the v1 recommendation. Context-owned schemas are:

- `rule_mgmt`
- `identity_resolution`
- `identity_lifecycle`
- `stewardship`
- `identity_governance`
- `integration_outbox`

Database roles enforce schema write ownership. Cross-schema reads occur only through approved views or application ports. Foreign keys may enforce integrity inside a context; cross-context coupling is minimized and documented.

## 4. Process topology

API, worker, and stewardship entry points may share build artifacts and modules while running as distinct processes. This permits independent scaling and fault isolation without creating independent service ownership prematurely.

## 5. Network boundaries

- source connectors originate from approved private network paths;
- public internet ingress is not required for source ingestion;
- stewardship access is restricted to enterprise identity and authorized reviewer roles;
- hometown access uses an authenticated private service or governed data interface;
- database and key vault are not internet accessible;
- administrative access traverses approved bastion or zero-trust controls.

## 6. Capacity posture

The worker scales by partitioning source-system/tenant work units. Concurrency is constrained by source-load budgets and database contention. API scale is independent from worker scale. Initial sizing is conservative; Stream 8 establishes measured capacity plans.

## 7. Physical anti-patterns

Prohibited:

- shared database credentials across processes;
- direct hometown database access to mutable internal tables;
- local filesystem as authoritative queue or evidence storage;
- raw source exports retained in Bluto;
- synchronous dependency on source systems for consumer mapping queries;
- one database per bounded context in v1 without an approved operational justification.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
