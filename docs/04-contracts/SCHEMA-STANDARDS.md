# Bluto Schema Standards

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-SCHEMA-001 |
| Artifact ID | ART-BLUTO-CONTRACT-SCHEMA-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-DOM-CANONICAL-001, BLUTO-CONTRACT-EVENT-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Naming

Contract field names use `lower_snake_case`; event and command type names use canonical PascalCase. Identifiers retain canonical names such as `party_id`, `source_key`, `tenant_id`, and `match_rule_id`.

## 2. Types

UUIDs use canonical string representation. Timestamps use RFC 3339 UTC with explicit offset. Decimal confidence values use a bounded decimal representation, not binary floating point. Enumerations are closed unless explicitly declared extensible.

## 3. Requiredness and nullability

Required, optional, absent, and null have distinct meaning. A field is nullable only when null is a documented business value. Additive fields default to optional.

## 4. Temporal fields

Effective intervals use `effective_from` inclusive and `effective_to` exclusive/null for open-ended. Processing, assertion, and occurrence times are separately named.

## 5. Validation

Schemas define length, pattern, range, cardinality, uniqueness expectations, enumeration, examples, and prohibited content. Contract validation occurs at ingress and before publication.

## 6. Sensitive data

Schemas carry classification annotations. Raw strong identifiers and HMAC tokens are prohibited from all externally published schemas.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
