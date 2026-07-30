# AI Guardrails and Hallucination Prevention

| Metadata | Value |
|---|---|
| Document ID | BLUTO-AI-GUARD-001 |
| Artifact ID | ART-BLUTO-AI-GUARD-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 9 — Ai Factory |
| Authority Level | RAH-10 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-AI-CONTEXT-001, BLUTO-SEC-ARCH-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

Agents must cite an upstream source for every domain entity, contract, security control, technology decision, environment, and acceptance criterion. Unsupported constructs fail validation.

Unknowns are represented as explicit blockers or proposed decisions, never plausible inventions. Agents may not fabricate existing APIs, tables, files, credentials, source fields, owners, or compliance approvals.

Automated checks scan for prohibited terms and patterns: golden record, fuzzy auto-link, query-time matching, cross-tenant inference, raw tax/government identifiers, direct hometown writes, direct cross-context SQL, disabled audit, and hard-coded secrets.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
