# Bluto Threat Model

| Metadata | Value |
|---|---|
| Document ID | BLUTO-SEC-THREAT-001 |
| Artifact ID | ART-BLUTO-SEC-THREAT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 5 — Security |
| Authority Level | RAH-6 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-SEC-ARCH-001, BLUTO-ARCH-VIEWS-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Method

Threats are analyzed by trust boundary and STRIDE category, with identity-specific abuse cases prioritized.

## 2. Priority threats

| Threat | Impact | Primary controls |
|---|---|---|
| Cross-tenant link creation | Disclosure and regulatory incident | tenant authorization, matching scope, constraints, tests, audit |
| Raw identifier leakage | Sensitive-data exposure | memory-only normalization, HMAC, redaction, DLP scans |
| HMAC key compromise | Enterprise-wide token compromise | Tier-0 vault, workload identity, dual control, rotation migration |
| Reviewer abuse | False merge/split | RBAC, separation of duties, evidence, immutable audit |
| Source adapter spoofing | Corrupt mappings | mutual auth, allow-listed endpoints, contract signatures/checks |
| Replay/duplicate command | Duplicate state transitions | idempotency keys, aggregate versions |
| Direct database mutation | History corruption | private access, schema roles, audited break-glass |
| Outbox tampering | Consumer inconsistency | transactional integrity, signatures/checksums, reconciliation |
| Enumeration of Party links | Privacy leakage | authorization, concealment, rate limits |
| AI-generated unsafe implementation | systemic control bypass | AI Factory policies, code review, security tests |

## 3. Abuse cases

Attackers may deliberately supply shared identifiers across tenants, manipulate effective dates, trigger expensive batches, infer relationships from response differences, or exploit stale consumer caches. Each abuse case has prevention, detection, and response controls.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
