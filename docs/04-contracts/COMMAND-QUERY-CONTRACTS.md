# Bluto Command and Query Contracts

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-CQ-001 |
| Artifact ID | ART-BLUTO-CONTRACT-CQ-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-CONTRACT-API-001, BLUTO-DOM-STATE-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Command envelope

Every command contains:

- `command_id`
- `command_type`
- `schema_version`
- `correlation_id`
- optional `causation_id`
- actor/workload identity reference
- tenant scope
- requested-at timestamp
- idempotency key
- expected aggregate version where applicable
- reason/evidence reference for privileged actions
- command payload

Commands are intent, may fail, and are never treated as completed facts.

## 2. Command catalogue

`RunIncrementalResolution`, `TriggerResolution`, `RecordReviewDecision`, `MergeParties`, `SplitParty`, `RetireParty`, `PublishRuleVersion`, `ActivateRuleVersion`, and `RetireRuleVersion`.

Each command maps to one application service and one primary aggregate boundary. Cross-aggregate orchestration is explicit and auditable.

## 3. Query envelope

Queries contain query type, schema version, correlation identifier, tenant scope, paging/filter criteria, and requested effective time. Queries are side-effect free.

## 4. Query catalogue

`ResolveCurrentParty`, `ResolvePartyAtTime`, `GetPartyLinks`, `ListLinkChanges`, `GetReviewCase`, `ListReviewQueue`, `GetRuleVersion`, `GetBatchRun`, and `GetAuditEvidence`.

## 5. Response rules

Responses distinguish not found, no active mapping, merged identity, retired identity, unauthorized scope, stale projection, and dependency degradation. Null never substitutes for an undocumented outcome.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
