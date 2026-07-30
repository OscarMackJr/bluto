# Bluto AI Factory Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-AI-ARCH-001 |
| Artifact ID | ART-BLUTO-AI-ARCH-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 9 — Ai Factory |
| Authority Level | RAH-10 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-DEPENDENCY-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Objective

Define a controlled factory through which Codex and other agents consume the Starter Kit, generate implementation artifacts, validate them, and present evidence for human-governed acceptance.

## 2. Factory stages

Context assembly → work-package selection → plan generation → artifact generation → static validation → contract/security/test validation → architecture conformance → human gate where required → merge candidate → provenance record.

## 3. Principle

AI is an implementation worker, not an architecture authority. It may realize approved decisions, identify ambiguity, and propose ADRs, but it may not silently invent or override architecture.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
