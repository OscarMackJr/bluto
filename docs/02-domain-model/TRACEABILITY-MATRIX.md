# Domain Traceability Matrix

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-TRACE-001 |
| Artifact ID | ART-BLUTO-DOM-TRACE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-CANONICAL-001, BLUTO-DOM-REPOSITORY-001, BLUTO-DOM-STATE-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

| Requirement | Capability | Aggregate | Event | Repository | State |
|---|---|---|---|---|---|
| REQ-ID-01 stable Party | Identity Resolution | Party | PartyCreated | PartyRepository | Party lifecycle |
| REQ-ID-02 reproducibility | Rule Management | Rule | RuleVersionPublished | RuleRepository | Rule lifecycle |
| REQ-ID-03 ambiguity review | Stewardship | Review | ReviewCaseOpened | ReviewRepository | Review lifecycle |
| REQ-ID-04 historical truth | Identity Lifecycle | Merge | PartyMerged/PartySplit | MergeRepository | Lifecycle model |
| REQ-ID-05 no raw identifiers | Token value semantics | Party | none external | PartyRepository | invariant |
| REQ-ID-06 tenant isolation | all contexts | Party/Review | policy finding | all repositories | all transitions |
| REQ-ID-07 ID non-reuse | Identity Resolution | Party | PartyCreated | PartyRepository | Party lifecycle |

Every downstream component, contract, control, test, deployment artifact, and AI work package must trace to an entry in this model.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
