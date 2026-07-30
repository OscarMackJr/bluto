# Bluto Integration Event Contracts

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-EVENT-001 |
| Artifact ID | ART-BLUTO-CONTRACT-EVENT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-DOM-EVENT-001, BLUTO-ARCH-INTEGRATION-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. Event envelope

Every published integration event includes:

- globally unique `event_id`;
- event type and semantic version;
- occurred-at and published-at UTC timestamps;
- aggregate identifier and aggregate version;
- correlation and causation identifiers;
- tenant scope where authorized;
- producer name and release version;
- payload schema reference;
- data-classification label.

## 2. Published event catalogue

- `PartyCreated.v1`
- `SourceLinkEstablished.v1`
- `SourceLinkSuperseded.v1`
- `PartyMerged.v1`
- `PartySplit.v1`
- `PartyRetired.v1`
- `ReviewCaseOpened.v1`
- `ReviewDecisionRecorded.v1`
- `RuleVersionPublished.v1`
- `RuleActivated.v1`
- `ResolutionBatchCompleted.v1`
- `PolicyViolationDetected.v1`

Internal domain events may be richer. Integration events expose only consumer-required facts and never raw strong identifiers, HMAC tokens, source payloads, reviewer personal details beyond governed identifiers, or internal table keys.

## 3. Delivery semantics

Delivery is at least once. Consumers deduplicate by `event_id`, tolerate delayed and out-of-order events across aggregates, and do not infer absence from delayed delivery. Ordering is guaranteed only per aggregate partition where transport supports it.

## 4. Evolution

Fields may be added as optional. Existing field meaning, type, requiredness, or enumeration meaning may not change within a major version. New major versions run in parallel through an announced migration window.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
