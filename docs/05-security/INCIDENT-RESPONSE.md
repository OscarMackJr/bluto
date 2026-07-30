# Bluto Security Incident Response

| Metadata | Value |
|---|---|
| Document ID | BLUTO-SEC-IR-001 |
| Artifact ID | ART-BLUTO-SEC-IR-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 5 — Security |
| Authority Level | RAH-6 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-SEC-AUDIT-001, BLUTO-ARCH-DEPLOY-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Identity-specific incidents

False merge, unauthorized cross-tenant link, raw identifier exposure, key compromise, audit loss, reviewer abuse, source compromise, and unauthorized data export are named incident types.

## 2. Response

Detect and classify; contain affected rules, tenants, credentials, or jobs; preserve evidence; assess historical mappings; revoke or supersede unsafe links through domain operations; notify required parties; recover; and complete root-cause and control remediation.

## 3. Key compromise

Suspend affected tokenization, revoke workload access, initiate planned retokenization, assess token exposure, rotate dependent signing material, and prohibit ad hoc in-place token replacement.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
