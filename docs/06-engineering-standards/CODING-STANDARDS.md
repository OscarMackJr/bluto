# Bluto Coding Standards

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ENG-CODE-001 |
| Artifact ID | ART-BLUTO-ENG-CODE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 6 — Engineering Standards |
| Authority Level | RAH-7 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ENG-ARCH-001, BLUTO-DOM-UL-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

Use canonical domain names. Functions are small and single-purpose; public behavior is typed and documented; side effects are isolated behind ports; mutable global state is prohibited.

Domain logic must not depend on HTTP, SQL, ORM, broker, clock, randomness, filesystem, or environment APIs directly. Use injected ports.

Concurrency is explicit and bounded. Cancellation and deadlines propagate. All collection processing is deterministic where ordering matters. Monetary or confidence values never use binary float where exact decimal behavior is required.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
