# Bluto Logical Architecture

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-LOGICAL-001 |
| Artifact ID | ART-BLUTO-ARCH-LOGICAL-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-REF-001, BLUTO-ARCH-QAS-001, BLUTO-DOM-BC-001, BLUTO-DOM-AGGREGATE-001, BLUTO-DOM-REVIEW-001 |
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

This document defines logical modules, dependency direction, and application responsibilities without prescribing deployment count.

## 2. Logical layers

```text
Interfaces and Adapters
        ↓
Application Services
        ↓
Domain Modules
        ↓
Ports
        ↓
Infrastructure Adapters
```

Dependencies point inward. Domain modules do not depend on frameworks, transport libraries, databases, or source-system SDKs.

## 3. Logical modules

| Module | Responsibilities | May depend on |
|---|---|---|
| Rule Management | Rule lifecycle, version validation, activation | Shared Kernel |
| Identity Resolution | Candidate evaluation, Party/link commands, deferral | Rule Management public port; Shared Kernel |
| Identity Lifecycle | Merge, split, retirement, point-in-time semantics | Party public port; Shared Kernel |
| Stewardship | Review queue, evidence, reviewer decisions | Resolution command port; Shared Kernel |
| Identity Governance | Audit evidence, policy signals, metrics projections | Published facts only |
| Source Adapters | Read source candidate data and normalize source contracts | Application ingestion ports |
| Hometown Adapter | Serve governed mapping queries | Query ports |
| Platform Adapters | Persistence, outbox, scheduling, key vault, telemetry | Domain/application ports |

## 4. Application services

Application services orchestrate use cases but do not contain domain invariants:

- `RunIncrementalResolution`
- `TriggerResolution`
- `CreateOrLinkParty`
- `OpenReviewCase`
- `RecordReviewDecision`
- `MergeParties`
- `SplitParty`
- `RetireParty`
- `ResolveCurrentParty`
- `ResolvePartyAtTime`
- `PublishGovernanceEvidence`

## 5. Public ports

Each module exposes explicit command, query, repository, clock, identity, tokenization, audit, and event-publication ports. Ports use canonical domain identifiers and value objects, not persistence records.

## 6. Dependency rules

- Source adapters may call ingestion application ports only.
- Resolution may read active rule versions through a Rule Management port but may not query rule tables.
- Stewardship may submit reviewed decisions through Resolution commands; it may not edit Party data directly.
- Lifecycle operations use a coordinated application transaction and aggregate ports; they do not perform table-level updates.
- Governance consumes published facts and audit envelopes; it does not become an alternate transactional model.
- Hometown receives query DTOs defined in Stream 4, never domain aggregates.

## 7. Initial modularity decision

The initial implementation shall use a modular monolith codebase with independently executable API, worker, and stewardship entry points. This balances operational simplicity with architectural boundaries. Modules must be testable in isolation and communicate through defined in-process ports or versioned messages. Separate services require an ADR supported by scaling, resilience, or ownership evidence.

## 8. Shared Kernel

The Shared Kernel is intentionally small:

- Party Identifier
- Tenant Identifier
- Source System
- Source Key
- Rule Identifier and Version
- Effective Date Range
- Correlation and Causation Identifier
- common domain error categories

Adding shared concepts requires architecture review.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
