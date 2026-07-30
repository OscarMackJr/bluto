# Bluto Contract Versioning and Compatibility Policy

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-COMPAT-001 |
| Artifact ID | ART-BLUTO-CONTRACT-COMPAT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-CHANGE-001, BLUTO-CONTRACT-SCHEMA-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Compatibility promise

A published major contract version remains compatible for its supported lifetime. Producers are responsible for backward compatibility; consumers are responsible for tolerant reading within documented extension points.

## 2. Change classification

Patch: documentation/example correction with no schema change. Minor: additive optional field, new endpoint, new event type, or new enum only where extensible. Major: removal, rename, type change, semantic change, requiredness change, auth change, ordering guarantee change, or identifier change.

## 3. Lifecycle

Proposed → Draft → In Review → Published → Deprecated → Retired. Deprecation identifies replacement, migration guide, consumer inventory, telemetry, and sunset date. Retirement requires evidence that no authorized consumer remains.

## 4. Compatibility tests

Provider and consumer contract tests run in CI. The currently released producer must pass against active consumer expectations; the candidate producer must pass against the current and next schema set.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
