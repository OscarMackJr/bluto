# Bluto Deployment Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-DEPLOY-001 |
| Artifact ID | ART-BLUTO-ARCH-DEPLOY-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-PHYSICAL-001, BLUTO-ARCH-DATA-001, BLUTO-ARCH-QAS-001, BLUTO-DOM-REVIEW-001 |
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

This document defines environment topology, deployable units, isolation, scaling, release safety, and recovery posture without binding to a specific cloud.

## 2. Deployable units

The v1 release contains:

- `bluto-api`
- `bluto-worker`
- `bluto-stewardship`
- database migration job
- outbox publisher, embedded in worker or separately executable
- scheduled reconciliation and maintenance jobs

All deployables are built from one versioned source baseline and share domain modules while having least-privilege identities and resource profiles.

## 3. Environment topology

Environments are Development, Integration, Preproduction, and Production. Production has isolated accounts/subscriptions/projects, network boundaries, keys, databases, telemetry, and access roles. Preproduction mirrors production topology with synthetic or approved de-identified data.

## 4. High availability

- API instances run across at least two failure zones where supported.
- Worker partitions are lease-based and restartable.
- PostgreSQL uses managed high availability and point-in-time recovery.
- Key Vault and telemetry use regionally resilient managed services.
- scheduled jobs are single-active through distributed lease or platform scheduling guarantees.

## 5. Scaling

API scales on request concurrency and latency. Worker scales on backlog, batch duration, and source budgets. Stewardship scales on reviewer load. Database scaling begins vertically with measured index and query tuning; partitioning is introduced only with evidence.

## 6. Release model

Deployments use immutable artifacts, signed provenance, automated migration gates, smoke tests, and progressive exposure. Compatible database expansion precedes code deployment; contraction follows verified adoption. Rollback is permitted only where schema and event compatibility remain safe; otherwise forward recovery is required.

## 7. Network and identity

All service-to-service communication uses authenticated private endpoints where available. Each deployable has a distinct workload identity. Database roles and key-vault permissions are scoped per process. No shared human credentials are used.

## 8. Disaster recovery

The initial target is same-region high availability plus cross-region encrypted backup recovery. Stream 8 validates whether warm standby is required. Recovery exercises must prove historical queries, outbox consistency, and tenant isolation after restoration.

## 9. Infrastructure ownership

Stream 8 will codify infrastructure as code, pipeline implementation, monitoring, backup operations, and runbooks. This document controls topology and non-negotiable deployment properties.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
