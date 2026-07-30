# Repository Architecture Dependency Graph

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-DEPENDENCY-001 |
| Artifact ID | ART-BLUTO-ARCH-DEPENDENCY-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-VISION-001, BLUTO-ARCH-PRINCIPLES-001, BLUTO-DOM-REVIEW-001, BLUTO-ARCH-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing references

This document is subordinate to the authoritative Bluto Enterprise Identity Spine v0.1 and the immutable Stream 1 and Stream 2 baselines. It preserves the following upstream invariants:

- Bluto owns identity mapping, stable Party identifiers, link lineage, rules, confidence, review decisions, and merge/split history.
- Bluto does not own source-system business attributes and shall not become a golden-record or master-data store.
- v1 resolution is deterministic, incremental batch, and tenant-scoped by default.
- raw strong identifiers are never persisted in Bluto; matching uses HMAC-SHA-256 tokens created with a Tier-0 key held outside the database.
- Party identifiers are never reused, and historical resolution is effective-dated and reproducible.
- hometown consumes Bluto mappings read-only and never performs ad hoc identity matching.


## 1. Purpose

This document establishes the normative dependency and consumption order for the Enterprise Starter Kit.

## 2. Canonical dependency graph

```text
Vision
    ↓
Architecture Principles
    ↓
Domain Model
    ↓
Architecture
    ↓
Contracts
    ↓
Security
    ↓
Engineering
    ↓
Testing
    ↓
DevOps
    ↓
AI Factory
```

Project Management governs execution across the stack but does not sit between architecture layers or redefine them.

## 3. Dependency rules

| Layer | Consumes | May refine | May not redefine |
|---|---|---|---|
| Domain Model | Vision and principles | business semantics | governance authority |
| Architecture | Domain Model | realization and topology | domain concepts/invariants |
| Contracts | Architecture | interface schemas and compatibility | ownership/topology |
| Security | Architecture and Contracts | controls and trust enforcement | contract semantics |
| Engineering | All above | implementation conventions | architecture or controls |
| Testing | All above | verification strategy | expected behavior |
| DevOps | All above | delivery and operations | implementation semantics |
| AI Factory | Entire baseline | autonomous workflow | any upstream decision |

## 4. Freeze levels

- **Level 1 — Stream 1:** Foundation and Governance, immutable baseline.
- **Level 2 — Stream 2:** Domain Model, immutable baseline.
- **Level 3 — Stream 3:** Architecture, immutable baseline after release.
- **Level 4 — Streams 4–9:** realization controls, each immutable after release.

A downstream stream may identify a defect upstream, but correction requires the approved ADR and change-control process. Silent reinterpretation is prohibited.

## 5. Codex consumption order

Codex shall read repository artifacts in dependency order. It shall:

1. load controlling authority and document status;
2. identify domain vocabulary and invariants;
3. identify architecture boundaries and decisions;
4. load contracts and security controls;
5. apply engineering and testing standards;
6. use DevOps constraints for packaging and deployment;
7. execute only through AI Factory work packages and validation gates.

## 6. Change impact

A change to an upstream layer triggers traceability analysis for every downstream layer. The change request must identify affected documents, contracts, controls, tests, pipelines, prompts, and implementation artifacts.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
