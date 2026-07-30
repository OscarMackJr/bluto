# Authentication and Authorization Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-SEC-IAM-001 |
| Artifact ID | ART-BLUTO-SEC-IAM-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 5 — Security |
| Authority Level | RAH-6 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-SEC-ARCH-001, BLUTO-CONTRACT-API-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Authentication

Humans use enterprise SSO with phishing-resistant MFA for privileged roles. Workloads use managed workload identity and short-lived tokens. Static shared credentials are prohibited.

## 2. Authorization roles

- Mapping Reader
- Mapping Bulk Consumer
- Data Steward Reviewer
- Rule Administrator
- Lifecycle Operator
- Security Auditor
- Platform Operator
- Break-Glass Administrator

Roles are composable only through approved policy. Lifecycle and rule activation require stronger privilege than read access.

## 3. Policy inputs

Authorization evaluates subject, workload, tenant scope, operation, resource, environment, reason/evidence, and risk context. Tenant scope is never trusted solely from request payload.

## 4. Separation of duties

A person who authors a high-impact rule cannot solely activate it. A reviewer cannot approve their own privileged override. Break-glass actions require retrospective review.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
