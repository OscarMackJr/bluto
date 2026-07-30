# Cryptography and Secrets Management

| Metadata | Value |
|---|---|
| Document ID | BLUTO-SEC-CRYPTO-001 |
| Artifact ID | ART-BLUTO-SEC-CRYPTO-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 5 — Security |
| Authority Level | RAH-6 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-SEC-ARCH-001, BLUTO-ARCH-TECH-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. HMAC controls

Strong identifiers are normalized in memory and tokenized with HMAC-SHA-256. The key is Tier-0, non-exportable where supported, versioned, and accessed only by authorized worker identities.

## 2. Key rotation

Rotation is a planned migration: introduce new key version, dual-tokenize controlled input, recompute stored tokens from source systems, validate parity, cut over rules, and retire prior key according to policy. Raw identifiers are never recovered from Bluto.

## 3. Other secrets

Database credentials, API client secrets, signing keys, and certificates are vault-managed, short-lived where possible, and scoped per workload. Secrets never appear in source, images, environment dumps, logs, or support bundles.

## 4. Encryption

TLS protects all network traffic. Databases, backups, queues, and evidence archives use managed encryption at rest. Higher-assurance customer-managed keys are applied where classification requires.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
