# Bluto Domain Model Overview

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-OVERVIEW-001 |
| Artifact ID | ART-BLUTO-DOM-OVERVIEW-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-S02-README-001, BLUTO-VISION-001, BLUTO-ARCH-PRINCIPLES-001, BLUTO-SCOPE-001, BLUTO-NONGOALS-001, BLUTO-GLOSSARY-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## 1. Purpose

This document defines the top-level domain model for Bluto and establishes the semantic boundaries that all later Stream 2 artifacts must refine without contradiction.

It translates the authoritative Bluto identity-spine architecture into a controlled domain view suitable for downstream architecture, contract, security, testing, DevOps, AI-factory, and autonomous implementation work.

This document is intentionally technology-neutral. It describes domain meaning, ownership, invariants, lifecycles, and relationships. It does not prescribe database products, service topology, programming languages, persistence frameworks, deployment platforms, user-interface technology, or runtime packaging.

## 2. Authority and Precedence

The authoritative source for this document is `BLUTO_IDENTITY_SPINE_v0.1.md`.

The repository README, `BLUTO-S02-README-001`, defines repository-wide authority, precedence, documentation controls, and non-negotiable architectural invariants.

This document must be interpreted consistently with the approved Stream 1 foundation and governance body, including the approved Vision, Architecture Principles, Scope, Non-goals, Glossary, ADR package, Governance package, Decision Log, Architecture Review Checklist, Change Control, and Document Status artifacts.

### 2.1 Baseline evidence note

The supplied `Enterprise_Starter_Kit_Stream1_v1.0.zip` archive contains the approved repository README and authoritative identity-spine specification, plus empty Stream 1 ADR and governance directories. The other approved Stream 1 document bodies are not present in that archive. This document therefore:

- relies on the authoritative specification and approved README for normative content;
- references the approved Stream 1 body by title rather than inventing unavailable document identifiers or text;
- must be revalidated against the complete Stream 1 baseline before Stream 2 is released;
- does not modify, reconstruct, or regenerate any approved Stream 1 artifact.

If a future complete Stream 1 baseline reveals a conflict, the conflict must enter formal change control. Stream 2 may not silently override Stream 1.

## 3. Domain Mission

Bluto provides governed enterprise identity resolution across independently owned source systems.

Its domain purpose is to answer, with durable evidence:

> Which source-system records represented the same enterprise party at a given point in time, under which authorized resolution scope, and by which versioned rule or reviewed decision?

Bluto accomplishes this by assigning stable enterprise party identifiers, recording effective-dated source-key links, preserving merge and split lineage, enforcing tenant-scoped resolution, and retaining sufficient provenance to reproduce and explain every identity assertion.

Bluto does not determine the best name, address, balance, status, or other business attribute for a party. It establishes identity equivalence only.

## 4. Domain Boundary

### 4.1 In-boundary responsibilities

The Bluto domain owns:

- enterprise `party_id` issuance and lifecycle;
- party type and identity lifecycle status;
- mappings from source-system keys to enterprise parties;
- effective dating of identity links;
- match-rule identity and version provenance;
- deterministic match outcomes;
- review and manual decision outcomes;
- merge lineage;
- split and correction lineage;
- resolution-scope enforcement, including tenant boundaries;
- point-in-time identity resolution;
- identity coverage, conflict, and review-queue metrics;
- evidence required to reproduce and audit an identity assertion.

### 4.2 Out-of-boundary responsibilities

The Bluto domain does not own:

- source-system business attributes;
- golden-record or survivorship logic;
- customer, borrower, account, or organization master data;
- semantic models or Cube models;
- cross-domain analytical interpretation;
- business-process state;
- authorization or workforce identity management;
- query-time identity inference;
- ad hoc matching by consuming systems;
- real-time streaming resolution in the initial production slice;
- probabilistic or fuzzy matching in v1;
- organization hierarchy resolution in the chartered v1 scope.

## 5. Core Domain Concepts

The domain is organized around the following concepts.

### 5.1 Party

A **Party** is the durable enterprise identity anchor representing a person or organization for identity-resolution purposes.

A Party is not a business profile. It carries only the minimum information required to identify its enterprise identity lifecycle:

- `party_id`;
- `party_type`;
- resolution scope, represented by `tenant_id` where applicable;
- lifecycle status;
- merge destination where applicable;
- creation and update timestamps.

A Party identifier is stable, globally unique within the Bluto domain, and never reused.

### 5.2 Source Record Identity

A **Source Record Identity** is the pair of:

- `source_system`; and
- `source_key`.

It identifies a record in a system of record without importing that record's business attributes into Bluto.

Where a source system is tenant-scoped, the source identity is evaluated within the authorized resolution scope. Tenant context is not optional metadata; it is part of the domain's disclosure boundary.

### 5.3 Party Source Link

A **Party Source Link** is an effective-dated assertion that a Source Record Identity belongs to a Party during a defined interval.

Each link records:

- the Party;
- the source-system identity;
- tenant scope;
- the rule or reviewed decision that produced the assertion;
- match method;
- confidence;
- effective period;
- assertion status;
- asserting actor or rule;
- assertion timestamp.

A Party Source Link is evidence-bearing domain history, not a replaceable lookup row.

### 5.4 Match Rule

A **Match Rule** is a versioned, source-controlled decision rule that can produce or contribute to an identity assertion.

The initial rule families are:

1. existing referential-key rules (`det.fk.*`);
2. tokenized strong-identifier exact rules (`det.tax_id.v1`, `det.gov_id.v1`);
3. approved normalized composite-exact rules (`det.name_dob_addr.v1`) in the appropriate delivery slice.

A Match Rule definition includes its normalization procedure, input eligibility, scope restrictions, priority, exact decision semantics, version, and reproducibility requirements.

### 5.5 Match Evidence

**Match Evidence** is the minimum protected evidence necessary to explain why a candidate or link was produced.

For strong identifiers, Bluto stores only HMAC-SHA-256 tokens and associated rule provenance. It does not store readable strong identifiers at rest.

Evidence is classified and exposed according to least privilege. Evidence needed for a reviewer does not become general-purpose business data.

### 5.6 Resolution Candidate

A **Resolution Candidate** is a proposed relationship evaluated by the resolution pipeline but not yet established as an active Party Source Link.

Candidates may result in:

- deterministic acceptance;
- conflict handling;
- review-queue placement;
- rejection;
- deferral;
- no action.

A candidate below the deterministic acceptance threshold must never create an active link automatically.

### 5.7 Review Case

A **Review Case** is the governed unit of human adjudication for unresolved, ambiguous, or conflicting candidates.

A Review Case records:

- candidate identities;
- protected evidence;
- reason for review;
- current disposition;
- reviewer identity;
- decision time;
- decision rationale;
- resulting link or rejection lineage.

The review surface may be minimal in early delivery slices, but the audit record is mandatory.

### 5.8 Resolution Scope

A **Resolution Scope** defines the population within which identity comparison is authorized.

The default scope is the tenant boundary. Cross-tenant comparison or resolution is prohibited unless an explicit, recorded authorization establishes a broader enterprise scope.

Resolution Scope is a first-class domain constraint applied during candidate generation, rule evaluation, review, linking, and query.

### 5.9 Identity Ruleset

An **Identity Ruleset** is the ordered, versioned collection of active Match Rules and associated scope policies used by a resolution run.

The ruleset version is part of answer provenance. A downstream Answer Trace Envelope must be able to identify the Party identifiers and ruleset version used at answer time once the corresponding cross-component contract change is approved.

### 5.10 Resolution Run

A **Resolution Run** is an auditable execution of the batch resolution process against a defined source extraction window and Identity Ruleset.

It records sufficient operational and domain metadata to establish:

- which source snapshots or extraction windows were evaluated;
- which ruleset version was used;
- run scope;
- start and completion state;
- counts of candidates, links, conflicts, and review cases;
- failures and resumability information;
- lineage to resulting assertions.

The initial domain model assumes scheduled incremental runs plus an authorized on-demand trigger. It does not assume streaming resolution.

### 5.11 Merge

A **Merge** is a governed identity correction or consolidation in which one Party is absorbed into a surviving Party.

A merge:

- sets the absorbed Party status to `merged`;
- records `merged_into_party_id`;
- closes affected active links at the merge instant;
- creates new links to the surviving Party from that instant where appropriate;
- preserves all prior identifiers and lineage;
- never deletes or reuses the absorbed Party identifier.

### 5.12 Split

A **Split** is a governed correction of a prior identity assertion that incorrectly grouped source identities.

A split:

- rejects or supersedes affected links while preserving their history;
- creates new Party identifiers for separated entities as required;
- never recycles the original Party identifier;
- preserves enough lineage to explain both the original assertion and the correction.

### 5.13 Point-in-Time Resolution

**Point-in-Time Resolution** determines which Party a Source Record Identity belonged to at a specified instant.

It is a primary domain capability, not an archival convenience. Effective dating alone must support the answer without rewriting history.

## 6. Bounded Contexts

Stream 2 will refine the following bounded contexts. These contexts describe semantic ownership and do not yet prescribe deployable services.

### 6.1 Party Registry Context

Owns:

- Party creation;
- Party type;
- Party lifecycle status;
- non-reusable Party identifier policy;
- merge destination references;
- retirement semantics.

Does not own source-system business attributes.

### 6.2 Identity Linking Context

Owns:

- Party Source Links;
- link lifecycle;
- uniqueness and temporal constraints;
- assertion provenance;
- point-in-time link resolution.

### 6.3 Matching Policy Context

Owns:

- Match Rule definitions;
- Identity Ruleset versioning;
- rule priority;
- normalization specifications;
- deterministic decision semantics;
- rule reproducibility.

It does not own secrets. Cryptographic key custody remains within the platform key-management boundary.

### 6.4 Resolution Operations Context

Owns:

- Resolution Runs;
- candidate generation;
- authorized source extraction windows;
- ordered rule execution;
- conflict detection;
- production of deterministic assertions;
- operational resolution metrics.

### 6.5 Stewardship Context

Owns:

- Review Cases;
- reviewer decisions;
- accept, reject, and defer dispositions;
- decision evidence and rationale;
- stewardship audit history.

Business ownership of the steward role remains an open organizational decision and must be resolved before the v1.1 review workflow is considered operationally complete.

### 6.6 Identity History Context

Owns:

- merge commands and lineage;
- split commands and lineage;
- effective-dated corrections;
- preservation of historical truth;
- point-in-time identity interpretation.

This context may be implemented with the Party Registry and Identity Linking contexts, but its semantics remain explicit.

## 7. Aggregate Boundaries

The following preliminary aggregates will be elaborated later in Stream 2.

### 7.1 Party Aggregate

**Aggregate root:** Party

Protects:

- Party identifier immutability;
- valid lifecycle transitions;
- merge destination validity;
- non-reuse of identifiers;
- consistency of Party type and scope.

### 7.2 Party Link Aggregate

**Aggregate root:** Party Source Link or a domain-specific Link Set root to be decided by ADR.

Protects:

- at most one active link for a source identity at an instant;
- valid effective intervals;
- allowed status transitions;
- assertion provenance completeness;
- tenant-scope consistency with the linked Party.

The exact transactional aggregate boundary is intentionally not finalized here because concurrency, persistence, and command-model implications require an ADR in Stream 3.

### 7.3 Match Rule Aggregate

**Aggregate root:** Match Rule Definition

Protects:

- immutable published rule versions;
- valid rule identifiers;
- explicit normalization and matching semantics;
- activation and retirement history;
- inclusion in a versioned Identity Ruleset.

### 7.4 Review Case Aggregate

**Aggregate root:** Review Case

Protects:

- single authoritative disposition;
- evidence immutability after decision;
- reviewer attribution;
- decision rationale;
- linkage between the decision and resulting assertion or rejection.

### 7.5 Resolution Run Aggregate

**Aggregate root:** Resolution Run

Protects:

- run identity;
- ruleset pinning;
- scope pinning;
- lifecycle transitions;
- reproducible execution metadata;
- summarized outcomes and failure state.

## 8. Domain Invariants

The following invariants are mandatory across every bounded context and later implementation.

### INV-DOM-001 — Identity mapping only

Bluto must not persist authoritative business attributes or computed golden-record values.

### INV-DOM-002 — Non-reusable Party identifiers

A `party_id` must never be reassigned to another Party or recycled after merge, split, retirement, or correction.

### INV-DOM-003 — Single active ownership

At any instant, a Source Record Identity may have no more than one active Party Source Link within its authorized Resolution Scope.

### INV-DOM-004 — Effective-dated history

Identity links and corrections must preserve valid effective periods sufficient for point-in-time resolution.

### INV-DOM-005 — Complete assertion provenance

Every active, superseded, or rejected identity assertion must identify the Match Rule or human decision that produced it and the actor or rule responsible.

### INV-DOM-006 — Deterministic automatic linking

Only an approved deterministic rule may create an automatic active link in the v1 architecture.

### INV-DOM-007 — Ambiguity does not auto-link

Ambiguous, conflicting, ineligible, or sub-threshold candidates must not create an active link automatically.

### INV-DOM-008 — Protected strong identifiers

Readable strong identifiers must not be persisted at rest in the Bluto data store. Only approved tokens and protected provenance may be retained.

### INV-DOM-009 — Tenant isolation by default

Identity comparison and linking across tenant boundaries are prohibited without explicit, recorded authorization.

### INV-DOM-010 — Historical truth survives correction

Merge, split, retirement, rejection, and supersession must not erase the prior state required to explain historical answers.

### INV-DOM-011 — Rules are versioned and reproducible

A published Match Rule version is immutable. Re-execution against the same eligible inputs and conditions must reproduce its deterministic outcome.

### INV-DOM-012 — Downstream read-only ownership

Hometown and other consumers may read governed Bluto identity mappings through approved contracts but may not write links or infer identity independently.

### INV-DOM-013 — Resolution occurs ahead of consumption

The authoritative identity relationship must be established by the governed resolution process before downstream cross-domain consumption.

### INV-DOM-014 — Scope is evidence-bearing

Any authorization that permits resolution beyond the default tenant boundary must be explicit, durable, reviewable, and attributable.

## 9. Lifecycle Models

### 9.1 Party lifecycle

The authoritative Party statuses are:

- `active`;
- `merged`;
- `retired`.

Permitted conceptual transitions:

```text
[new] -> active
active -> merged
active -> retired
```

A merged or retired Party identifier remains reserved permanently.

Reactivation semantics are not defined by the authoritative specification and must not be invented without an ADR.

### 9.2 Party Source Link lifecycle

The authoritative link statuses are:

- `active`;
- `superseded`;
- `rejected`.

Permitted conceptual transitions:

```text
[candidate] -> active
[candidate] -> rejected
active -> superseded
active -> rejected       # only through governed correction with retained lineage
```

Direct deletion of historical links is prohibited except where a separately approved legal-data-removal policy requires a controlled cryptographic or physical erasure process. No such policy is established in the current architecture.

### 9.3 Review Case lifecycle

The minimum conceptual lifecycle is:

```text
open -> accepted
open -> rejected
open -> deferred
deferred -> accepted
deferred -> rejected
```

Additional operational states such as assigned, in-review, expired, or escalated may be defined later, but must not weaken the durable decision audit.

### 9.4 Resolution Run lifecycle

The minimum conceptual lifecycle is:

```text
planned -> running -> completed
                 \-> failed
failed -> resumed | abandoned
```

Exact retry and resume semantics belong to later architecture and DevOps streams.

## 10. Domain Relationships

At the conceptual level:

```text
Party
  1  <---- 0..* Party Source Link

Source Record Identity
  1  <---- 0..* historical Party Source Link

Match Rule
  1  <---- 0..* Party Source Link assertions

Identity Ruleset
  1  <---- 1..* Match Rule versions

Resolution Run
  1  ----> 1 Identity Ruleset version
  1  ----> 1 Resolution Scope
  1  ----> 0..* Resolution Candidate

Resolution Candidate
  0..1 ----> 1 Review Case
  0..1 ----> 1 resulting Party Source Link

Review Case
  0..1 ----> 1 resulting Party Source Link
  0..1 ----> 1 rejection record

Party Merge
  1 ----> 1 absorbed Party
  1 ----> 1 surviving Party

Party Split
  1 ----> 1 prior Party context
  1 ----> 1..* resulting Parties or corrected link sets
```

These relationships express domain semantics only. Foreign-key placement, event publication, transactional consistency, and service ownership are deferred to the Architecture and Contracts streams.

## 11. Domain Commands

The preliminary command vocabulary is:

- `RegisterParty`;
- `RetireParty`;
- `ProposePartySourceLink`;
- `AssertDeterministicPartySourceLink`;
- `AcceptReviewCase`;
- `RejectReviewCase`;
- `DeferReviewCase`;
- `SupersedePartySourceLink`;
- `MergeParties`;
- `SplitParty`; 
- `AuthorizeResolutionScope`;
- `PublishMatchRuleVersion`;
- `PublishIdentityRuleset`;
- `StartResolutionRun`;
- `CompleteResolutionRun`;
- `FailResolutionRun`.

Command names are conceptual and may change during detailed domain modeling. Their business meaning must remain stable once approved.

## 12. Domain Events

The preliminary event vocabulary is:

- `PartyRegistered`;
- `PartyRetired`;
- `PartyMerged`;
- `PartySplit`;
- `PartySourceLinkAsserted`;
- `PartySourceLinkSuperseded`;
- `PartySourceLinkRejected`;
- `ResolutionCandidateCreated`;
- `ResolutionConflictDetected`;
- `ReviewCaseOpened`;
- `ReviewCaseAccepted`;
- `ReviewCaseRejected`;
- `ReviewCaseDeferred`;
- `MatchRuleVersionPublished`;
- `IdentityRulesetPublished`;
- `ResolutionScopeAuthorized`;
- `ResolutionRunStarted`;
- `ResolutionRunCompleted`;
- `ResolutionRunFailed`.

These events are domain facts. Whether they are persisted, published externally, or implemented as integration events is a later architectural and contractual decision.

## 13. Domain Services

Where behavior does not naturally belong to one aggregate, later design may define domain services such as:

- **Candidate Generation Service** — derives eligible comparison candidates within a Resolution Scope;
- **Deterministic Matching Service** — applies an ordered Identity Ruleset;
- **Identity Resolution Service** — determines the authoritative Party for a source identity at a point in time;
- **Merge Validation Service** — verifies merge preconditions and scope compatibility;
- **Split Planning Service** — computes the correction plan while preserving lineage;
- **Resolution Scope Policy** — decides whether records may be compared or linked;
- **Rule Reproduction Service** — replays a rule version against preserved or re-extracted inputs for audit.

These are domain responsibilities, not commitments to network services or classes.

## 14. Data Classification and Privacy Semantics

The domain contains identity-linkage data whose sensitivity may exceed that of any individual source key because the mapping reveals cross-system relationships.

Accordingly:

- Party Source Links are governed identity data;
- tenant-crossing relationships are disclosure-sensitive;
- strong-identifier tokens remain sensitive even though they are not readable identifiers;
- the HMAC key is a Tier-0 platform secret and is outside the Bluto domain store;
- review evidence must be minimized and access-controlled;
- auditability does not justify unrestricted evidence retention;
- security and retention specifics are deferred to Stream 5 but may not weaken these semantics.

## 15. Consistency and Concurrency Concerns

The domain requires explicit handling of concurrency risks, including:

- two resolution workers attempting to link the same Source Record Identity;
- a review decision racing with a deterministic rule result;
- a merge occurring while new links are being asserted;
- a split occurring while downstream consumers resolve current identity;
- ruleset activation changing during a Resolution Run;
- cross-tenant authorization changing during candidate generation;
- replay or retry producing duplicate assertions.

Later architecture must provide controls that preserve the domain invariants under these conditions. Database uniqueness alone may be necessary but is not sufficient; commands, idempotency, temporal semantics, and audit behavior must be defined together.

## 16. Query Responsibilities

The domain must support, at minimum:

- resolve the current Party for a Source Record Identity;
- resolve the Party for a Source Record Identity at a specified instant;
- list current source links for a Party;
- list historical source links for a Party;
- trace a Party through merge lineage;
- explain why a link exists;
- identify the Match Rule or reviewer decision behind a link;
- retrieve the Identity Ruleset version used by a Resolution Run;
- identify unresolved conflicts and pending Review Cases;
- report coverage and conflict metrics by source and scope.

Detailed query contracts and authorization requirements are deferred to Streams 4 and 5.

## 17. Initial Delivery-Slice Alignment

### 17.1 v1 before proof of concept

The domain model must support:

- Party and Party Source Link;
- referential-key rules;
- tokenized strong-identifier matching;
- tenant-scoped resolution;
- effective dating;
- seeded CRM, ledger, and Intrepid resolution demonstration;
- tenant-boundary enforcement test;
- ruleset and assertion provenance.

### 17.2 v1.1

The domain model expands to support:

- composite-exact rules;
- Review Cases and auditable decisions;
- coverage metrics;
- queue-depth and conflict metrics.

### 17.3 Deferred v2

The following are not chartered and require new architecture and privacy decisions:

- probabilistic matching;
- fuzzy matching;
- machine-learned thresholds;
- privacy-preserving approximate comparison;
- organization hierarchy resolution.

## 18. Requirements Traceability

| Domain concern | Authoritative requirement | Domain response |
|---|---|---|
| One enterprise identity across sources | REQ-ID-01 | Party and effective Party Source Links provide one stable identity anchor. |
| Reproducible assertions | REQ-ID-02 | Match Rule versions, Identity Rulesets, Resolution Runs, and assertion provenance are explicit concepts. |
| No sub-threshold automatic linking | REQ-ID-03 | Resolution Candidate and Review Case separate uncertain proposals from active links. |
| Historical truth after merge | REQ-ID-04 | Merge, Split, effective dating, and point-in-time resolution preserve prior identity state. |
| No readable strong identifiers at rest | REQ-ID-05 | Match Evidence permits HMAC tokens only for strong identifiers in the Bluto store. |
| Tenant-boundary enforcement | REQ-ID-06 | Resolution Scope is a first-class invariant and policy boundary. |
| No identifier reuse | REQ-ID-07 | Party Aggregate permanently reserves every issued `party_id`. |

## 19. Open Domain Decisions

The following decisions remain open and must not be silently resolved by implementation:

1. **Party Link aggregate boundary.** Determine whether source-link uniqueness is protected by individual link aggregates, a source-identity aggregate, or a Party link-set aggregate.
2. **Cross-tenant authorization model.** Define the authoritative entity, lifecycle, approver, evidence, and revocation semantics for broader Resolution Scopes.
3. **Review stewardship ownership.** Name the accountable business role for adjudicating Review Cases before v1.1.
4. **Source-system referential-key availability.** Inspect ledger and CRM to determine existing deterministic coverage.
5. **Merge authority and approval threshold.** Define which role may authorize merges and whether high-impact merges require dual control.
6. **Split planning semantics.** Define whether split creates only new Parties, reuses unaffected Parties, and how corrections are represented atomically.
7. **Retirement semantics.** Clarify when a Party is retired instead of left active with no current links.
8. **Ruleset activation semantics.** Define publication, effective time, rollback, and concurrent-run behavior.
9. **Evidence retention.** Define minimum and maximum retention for candidate and review evidence without expanding Bluto into a business-attribute store.
10. **Answer Trace Envelope v0.2.** Complete the two-team contract change for Party identifiers and ruleset version provenance.

Each resolved architectural decision must be recorded through the approved ADR and Decision Log processes.

## 20. Stream 2 Document Plan

This overview establishes the dependency base for the remaining Stream 2 documents. The proposed document sequence is:

1. Domain Model Overview — this document;
2. Core Domain and Subdomain Classification;
3. Bounded Context Catalogue;
4. Ubiquitous Language;
5. Entity and Aggregate Catalogue;
6. Value Object Catalogue;
7. Domain Invariants;
8. Domain Commands and Events;
9. Identity Lifecycle and State Models;
10. Match Rule Domain Model;
11. Resolution Scope and Tenancy Model;
12. Merge, Split, and Historical Truth Model;
13. Stewardship and Review Domain Model;
14. Canonical Data Definitions;
15. Domain Traceability Matrix;
16. Domain Model Review Checklist;
17. Stream 2 Document Status and Release Certification.

Each document must reference this overview and all previously approved Stream 2 documents on which it depends.

## 21. Review and Acceptance Criteria

This document is acceptable only when reviewers confirm that it:

- preserves Bluto's identity-only boundary;
- includes all authoritative data-model concepts;
- treats tenant scope as a domain disclosure boundary;
- preserves immutable historical truth;
- supports deterministic v1 without prematurely introducing probabilistic matching;
- distinguishes domain semantics from implementation architecture;
- identifies unresolved decisions rather than inventing them;
- provides sufficient structure for the remaining Stream 2 documents;
- contains no contradiction with the authoritative identity-spine specification or approved README.

## 22. References and Dependencies

### Normative references

- `BLUTO-S02-README-001` — Bluto Dark Factory Enterprise Starter Kit README.
- `BLUTO_IDENTITY_SPINE_v0.1.md` — authoritative Bluto identity-spine architecture specification.
- Approved Stream 1 Vision.
- Approved Stream 1 Architecture Principles.
- Approved Stream 1 Scope.
- Approved Stream 1 Non-goals.
- Approved Stream 1 Glossary.
- Approved Stream 1 ADR package.
- Approved Stream 1 Governance package.
- Approved Stream 1 Decision Log.
- Approved Stream 1 Architecture Review Checklist.
- Approved Stream 1 Change Control.
- Approved Stream 1 Document Status standard.

### Downstream dependencies

All later Stream 2 documents and Streams 3–10 must treat this document as the top-level approved domain model after it reaches Approved status.

## 23. Change History

| Version | Date | Status | Change | Author |
|---|---|---|---|---|
| 0.1.0-draft | 2026-07-30 | Draft for Review | Initial Stream 2 domain model overview derived from the authoritative identity-spine specification and Stream 1 README. | Bluto Domain Architecture |

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
