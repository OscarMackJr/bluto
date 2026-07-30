# AI Agent Roles and Responsibilities

| Metadata | Value |
|---|---|
| Document ID | BLUTO-AI-ROLE-001 |
| Artifact ID | ART-BLUTO-AI-ROLE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 9 — Ai Factory |
| Authority Level | RAH-10 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-AI-ARCH-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

Defined roles:

- Context Assembler: selects authoritative references.
- Planner: creates bounded implementation plan.
- Test Author: writes failing tests from Stream 7.
- Implementer: writes minimal code to satisfy tests and standards.
- Contract Validator: checks API/event/schema compatibility.
- Security Validator: checks controls and unsafe patterns.
- Architecture Validator: checks dependencies and traceability.
- Documentation Maintainer: updates implementation docs only.
- Release Evidence Agent: produces provenance and gate summary.

No single agent self-approves high-risk changes.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
