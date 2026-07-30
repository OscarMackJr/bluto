# Repository Organization Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-BASE-GOV-003 |
| Artifact ID | ART-BLUTO-BASE-GOV-003-v2.0.0 |
| Version | 2.0.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline A — Repository Constitution |
| Stream | Repository-wide |
| Authority Level | RAH-1 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Canonical structure

The root contains `00-baseline`, `docs/01-foundation-governance` through `docs/10-project-management`, `repository`, `tools`, and `releases`. Normative documents, generated registries, evidence, and release packages remain separated.

Each stream contains a README, controlled documents, reviews, release materials, and assets. Every controlled artifact has one canonical source location. Duplicate authoritative copies, mixed draft/baselined release content, implementation code in Baseline A, and undocumented top-level directories are prohibited.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
