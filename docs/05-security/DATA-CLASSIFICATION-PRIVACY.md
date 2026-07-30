# Data Classification and Privacy Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-SEC-DATA-001 |
| Artifact ID | ART-BLUTO-SEC-DATA-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 5 — Security |
| Authority Level | RAH-6 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-DOM-CANONICAL-001, BLUTO-CONTRACT-SCHEMA-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Classes

- Internal: operational metadata without identity linkage.
- Confidential: Party identifiers, source keys, rules, metrics.
- Restricted: HMAC tokens, tenant-linked mappings, review evidence, merge/split lineage, audit records.
- Tier-0 Secret: HMAC key material and equivalent cryptographic roots.

## 2. Handling

Restricted data requires private storage, least privilege, encryption, audit, controlled export, and approved retention. Tier-0 secrets remain in vault custody.

## 3. Minimization

Bluto does not persist names, addresses, balances, or source profiles. Stewardship evidence is minimized, masked, and time-bounded where possible.

## 4. Privacy operations

Access, correction, retention, and deletion requests are coordinated with source-system owners. Historical evidence is altered only where law and records policy require, with tombstone lineage preserving auditability.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
