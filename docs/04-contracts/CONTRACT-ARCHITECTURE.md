# Bluto Contract Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-ARCH-001 |
| Artifact ID | ART-BLUTO-CONTRACT-ARCH-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-REVIEW-001, BLUTO-ARCH-TRACE-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Purpose

Define the contract surfaces through which humans, workloads, bounded-context modules, source adapters, hometown, and event consumers interact with Bluto.

## 2. Contract classes

| Class | Purpose | Primary format |
|---|---|---|
| Interactive API | Mapping queries, stewardship, administration, authorized triggers | HTTP/JSON with OpenAPI |
| Batch ingestion | Source candidate extraction into canonical candidate envelopes | Adapter-specific input plus canonical internal DTO |
| Command | State-changing intent at application boundaries | Versioned command schema |
| Query | Side-effect-free retrieval | Versioned request/response schema |
| Integration event | Completed business fact for external consumers | Versioned event envelope |
| Repository port | Persistence boundary for aggregates | Language-neutral operation specification |
| Operational contract | Health, readiness, metrics, jobs, and support actions | HTTP/metrics/job metadata |

## 3. Contract authority

Canonical data definitions control meaning. Architecture controls ownership and direction. Contracts control serialization, compatibility, error semantics, and consumer obligations. Persistence records and domain objects are never public contracts.

## 4. Surface inventory

- Mapping Query API
- Point-in-Time Resolution API
- Batch Change Feed
- Stewardship Query and Decision API
- Rule Administration API
- Lifecycle Command API
- Resolution Trigger API
- Source Adapter Candidate Contract
- Integration Event Catalogue
- Health and Operational Contract

## 5. General requirements

Every contract has an owner, version, schema, examples, compatibility policy, security classification, authorization rule, idempotency rule, error model, SLO, and test suite. Undocumented interfaces are prohibited.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
