# AI Code Generation Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-AI-CODE-001 |
| Artifact ID | ART-BLUTO-AI-CODE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 9 — Ai Factory |
| Authority Level | RAH-10 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-AI-PROMPT-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

Agents generate tests before production code, preserve module boundaries, use canonical names, isolate side effects behind ports, and implement only published contracts.

Generated code must include architecture trace references, typed errors, tenant context, correlation, audit hooks, no-sensitive-data logging, idempotency where required, and deterministic tests.

Agents may not generate direct cross-schema writes, raw identifier persistence, unversioned endpoints/events, probabilistic matching, shared credentials, or bypass flags.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
