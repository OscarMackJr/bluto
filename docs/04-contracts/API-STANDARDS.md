# Bluto API Standards

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-API-001 |
| Artifact ID | ART-BLUTO-CONTRACT-API-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-CONTRACT-ARCH-001, BLUTO-ARCH-CROSSCUT-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Resource and operation rules

APIs use nouns for resources and explicit action endpoints only for domain commands that do not map cleanly to resource state. Canonical identifiers appear in paths only when disclosure and tenancy rules permit. Tenant scope is derived from authenticated authorization context or an explicitly validated tenant field.

## 2. Versioning

Major versions appear in the base path, such as `/v1`. Additive fields do not require a major version. Removing, renaming, retyping, changing requiredness, changing enumeration meaning, or weakening authorization is breaking.

## 3. Required headers

- correlation identifier;
- idempotency key for retryable commands;
- content type and accepted schema;
- conditional version where optimistic concurrency applies.

## 4. Pagination and filtering

Cursor pagination is mandatory for unbounded collections. Cursors are opaque, signed or integrity protected, and tenant scoped. Filters are allow-listed and indexed.

## 5. Mapping API examples

- `GET /v1/mappings/{source_system}/{source_key}`
- `GET /v1/mappings/{source_system}/{source_key}/at/{timestamp}`
- `GET /v1/parties/{party_id}/links`
- `GET /v1/link-changes?after=<cursor>`

Responses include canonical identifiers, status, effective interval, ruleset version, and provenance reference, but never HMAC tokens or source-owned attributes.

## 6. Command APIs

Merge, split, retire, review decision, rule activation, and on-demand resolution commands require idempotency, explicit authorization, request reason, optimistic version, and audit correlation.

## 7. HTTP semantics

200/201 indicate successful retrieval or creation; 202 is reserved for accepted asynchronous jobs; 204 for successful no-body commands; 400 invalid syntax; 401 unauthenticated; 403 unauthorized scope; 404 absent or intentionally concealed; 409 invariant/version conflict; 422 valid syntax but invalid domain transition; 429 load budget; 503 dependency unavailable.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
