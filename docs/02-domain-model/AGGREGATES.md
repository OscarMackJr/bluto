# Bluto Aggregates

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-AGGREGATE-001 |
| Artifact ID | ART-BLUTO-DOM-AGGREGATE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-ENTITY-001, BLUTO-DOM-VALUEOBJECT-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

**Party Aggregate** root Party contains source links and enforces immutable ID, one active link per canonical source key/scope, effective dating, and no business attributes. **Rule Aggregate** root Rule Definition contains immutable versions and activation state. **Review Aggregate** root Review Case contains candidate evidence and terminal decisions. **Merge Aggregate** root Merge Record coordinates merge/split lineage and historical correctness.

External access occurs through aggregate roots. Cross-aggregate references use identifiers. Transactions preserve one aggregate unless an approved lifecycle orchestration explicitly coordinates multiple affected Party aggregates with atomic lineage and outbox state.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
