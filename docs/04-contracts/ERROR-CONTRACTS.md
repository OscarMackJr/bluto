# Bluto Error Contract Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-ERROR-001 |
| Artifact ID | ART-BLUTO-CONTRACT-ERROR-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-CROSSCUT-001, BLUTO-CONTRACT-API-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Error envelope

Errors contain stable error code, category, safe message, correlation identifier, retryability, optional field violations, and support reference. They never contain stack traces, SQL, source records, tokens, secret references, or internal topology.

## 2. Stable categories

- `REQUEST_INVALID`
- `AUTHENTICATION_REQUIRED`
- `SCOPE_FORBIDDEN`
- `RESOURCE_NOT_FOUND`
- `IDENTITY_CONFLICT`
- `REVIEW_REQUIRED`
- `STATE_TRANSITION_INVALID`
- `VERSION_CONFLICT`
- `LOAD_BUDGET_EXCEEDED`
- `DEPENDENCY_UNAVAILABLE`
- `TRANSIENT_FAILURE`
- `INTERNAL_FAILURE`

## 3. Retry rules

Only errors explicitly marked retryable may be automatically retried. A retry requires the same idempotency key for commands. Domain conflicts and authorization failures are never retried automatically.

## 4. Concealment

Where existence itself is sensitive, the API returns the approved concealed response rather than distinguishing absent from unauthorized.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
