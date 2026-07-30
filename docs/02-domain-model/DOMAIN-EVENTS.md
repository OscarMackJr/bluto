# Bluto Domain Events

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-EVENT-001 |
| Artifact ID | ART-BLUTO-DOM-EVENT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-AGGREGATE-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

Identity Resolution emits PartyCreated, SourceLinkEstablished, SourceLinkSuperseded, IdentityResolutionCompleted, and IdentityResolutionDeferred. Lifecycle emits PartyMerged, PartySplit, PartyRetired, and IdentityHistoryUpdated. Rule Management emits RuleCreated, RuleVersionPublished, RuleActivated, and RuleRetired. Stewardship emits ReviewCaseOpened and ReviewDecisionRecorded. Governance emits PolicyViolationDetected and AuditRecordCreated.

Events are immutable completed business facts, versioned independently from transport, ordered only within an aggregate, duplicate-tolerant, and free of raw strong identifiers and internal persistence structure.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
