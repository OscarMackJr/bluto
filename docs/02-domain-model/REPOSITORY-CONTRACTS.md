# Bluto Repository Contracts

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-REPOSITORY-001 |
| Artifact ID | ART-BLUTO-DOM-REPOSITORY-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-AGGREGATE-001, BLUTO-DOM-EVENT-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

PartyRepository persists and retrieves complete Party aggregates, active and historical links, and source-key resolution. RuleRepository preserves immutable rule versions and activation. ReviewRepository stores cases and decisions and supports queue queries. MergeRepository records lineage and point-in-time reconstruction.

Repositories expose domain operations, not tables or generic CRUD. They preserve aggregate consistency, tenant scope, optimistic versioning, auditability, and historical reconstruction. Cross-context repositories never access another context's persistence directly.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
