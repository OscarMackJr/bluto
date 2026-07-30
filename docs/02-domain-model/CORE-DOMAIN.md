# Bluto Core Domain

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-CORE-001 |
| Artifact ID | ART-BLUTO-DOM-CORE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-OVERVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

The core domain is enterprise identity resolution, continuity, lineage, and governance. It owns stable Party identity, source links, deterministic resolution outcomes, rule provenance, review decisions, effective dating, merge/split history, and point-in-time reconstruction.

Supporting capabilities are rule management, stewardship, governance evidence, metrics, and source adaptation. Customer management, source attributes, analytics, workflow orchestration, and master data remain outside the core domain.

Core invariants: Party ID never changes or repeats; one source record has at most one active link at an instant within the canonical source/tenant scope; ambiguous candidates never auto-link; history is append-preserving; business attributes remain in systems of record.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
