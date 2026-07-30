# Bluto Engineering Architecture Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ENG-ARCH-001 |
| Artifact ID | ART-BLUTO-ENG-ARCH-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 6 — Engineering Standards |
| Authority Level | RAH-7 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Objective

Translate approved architecture, contracts, and security into enforceable implementation conventions. Engineering choices may improve implementation quality but may not alter domain semantics or trust boundaries.

## 2. Codebase structure

One repository with modules aligned to Rule Management, Identity Resolution, Identity Lifecycle, Stewardship, Identity Governance, application services, ports, adapters, and process entry points. Dependency direction is checked automatically.

## 3. Required properties

Deterministic behavior, explicit types, immutable domain values, dependency injection at ports, transaction boundaries in application services, no framework leakage into domain modules, and reproducible builds.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
