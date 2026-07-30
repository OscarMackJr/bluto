# Bluto Security Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-SEC-ARCH-001 |
| Artifact ID | ART-BLUTO-SEC-ARCH-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 5 — Security |
| Authority Level | RAH-6 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-DEPLOY-001, BLUTO-CONTRACT-ARCH-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Security objectives

Protect identity mappings from unauthorized disclosure or mutation; prevent cross-tenant inference; keep raw strong identifiers and key material outside Bluto persistence; preserve attributable, immutable evidence; and maintain safe degraded behavior.

## 2. Trust zones

- source-system zone;
- Bluto private application zone;
- restricted data zone containing PostgreSQL;
- Tier-0 key-management zone;
- stewardship user zone;
- hometown consumer zone;
- centralized operations and telemetry zone.

Every crossing is authenticated, authorized, encrypted, logged, rate-limited where applicable, and contract validated.

## 3. Control model

Defense in depth combines workload identity, network segmentation, policy-based authorization, tenant context validation, schema-level database roles, encryption, key-vault custody, immutable audit, secure delivery, and continuous verification.

## 4. Security invariants

No raw strong identifiers at rest or in logs/events; no shared human credentials; no implicit cross-tenant scope; no direct consumer database access; no unaudited privileged identity change; no production data in lower environments without approved transformation.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
