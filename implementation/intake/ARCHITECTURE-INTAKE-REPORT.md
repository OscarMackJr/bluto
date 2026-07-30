# Architecture Intake Report

## Scope

This intake is repository intake only. It records authoritative sources, implementation constraints, unresolved decisions, and a proposed work breakdown. It does not select a programming language, framework, ORM, cloud provider, broker, CI platform, or observability backend where the certified baseline defers those choices. Controlling authority: `BLUTO-REPO-README-001` sections "Repository status", "Required reading order", and "Machine registry"; `BLUTO-BASE-GOV-001` sections "Mission", "Constitutional principles", and "Repository Authority Hierarchy"; `BLUTO-AI-CONTEXT-001` section "Governing rules".

## Authoritative Documents And Precedence

The top semantic authority is the Bluto Enterprise Identity Spine, treated by downstream documents as the authoritative architecture specification. It defines the identity-only scope, data model, tenant risk, merge/split semantics, resolution pipeline, guardrails, requirements, delivery slices, and open decisions. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 1 through 13; `BLUTO-GOV-DECISION-001` decision `DEC-002`; `BLUTO-BASE-GOV-001` section "Repository Authority Hierarchy".

Repository Baseline A is the constitutional authority below the Architecture Specification and governs identity, metadata, authority, dependencies, validation, evidence, certification, audit, and publication. Controlled Markdown remains normative over generated registry projections. Controlling authority: `BLUTO-BASE-GOV-001` sections "Mission", "Constitutional principles", "Repository Authority Hierarchy", and "Reading order"; `BLUTO-REPO-README-001` sections "Machine registry" and "Document control".

Streams 1 through 9 specialize authority in this order: Foundation and Governance, Domain Model, Architecture, Contracts, Security, Engineering Standards, Testing, DevOps, and AI Factory. Lower authority may specialize but may not redefine higher authority. Controlling authority: `BLUTO-BASE-GOV-001` section "Repository Authority Hierarchy"; `repository/constitution/repository-baseline.yaml` keys `rah` and `principles`; `BLUTO-AI-CONTEXT-001` section "Governing rules".

Machine-readable `/repository` artifacts are generated projections for LIR, RGTM, gates, validation, evidence, schemas, manifest, audit, and metrics; they are inspected but not treated as overriding controlled Markdown. Controlling authority: `BLUTO-REPO-README-001` section "Machine registry"; `repository/lir/logical-identity-registry.yaml`; `repository/rgtm/repository-governance-traceability.yaml`; `repository/metrics/repository-health.yaml`.

## Immutable Domain Invariants

Bluto is identity mapping only. It must not persist source-owned business attributes, computed "best" values, or golden-record data. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 1, 2, 10, and 12; `BLUTO-DOM-OVERVIEW-001` sections 4.1, 4.2, and `INV-DOM-001`; `BLUTO-NONGOALS-001` document body.

Party identifiers are stable, opaque, globally unique within Bluto, and never reused after merge, split, retirement, or correction. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 3.1, 7, 10, and `REQ-ID-07`; `BLUTO-DOM-OVERVIEW-001` sections 5.1, 8 `INV-DOM-002`, and 9.1.

A source record may have at most one active Party Source Link at any instant within its authorized resolution scope; effective-dated history must support point-in-time resolution without rewriting history. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 3.2 and 7; `BLUTO-DOM-OVERVIEW-001` section 8 `INV-DOM-003`, `INV-DOM-004`, and `INV-DOM-010`; `BLUTO-DOM-VALUEOBJECT-001` document body.

Automatic v1 linking is deterministic only. Ambiguous, conflicting, ineligible, or sub-threshold candidates must not create active links automatically. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 4, 8, 10, and `REQ-ID-03`; `BLUTO-DOM-OVERVIEW-001` section 8 `INV-DOM-006` and `INV-DOM-007`; `BLUTO-ARCH-PRINCIPLES-001` principles 2 and 3.

Readable strong identifiers must never be persisted at rest, logged, or published. Matching uses HMAC-SHA-256 tokens, with Tier-0 key custody outside the Bluto database. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 5, 10, and `REQ-ID-05`; `BLUTO-DOM-OVERVIEW-001` section 8 `INV-DOM-008`; `BLUTO-SEC-CRYPTO-001` sections 1 and 2; `BLUTO-SEC-ARCH-001` section 4.

Tenant isolation is default. Cross-tenant resolution requires explicit, recorded authorization and fails closed if scope is missing, ambiguous, expired, or conflicting. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 6, 10, and `REQ-ID-06`; `BLUTO-DOM-OVERVIEW-001` section 8 `INV-DOM-009` and `INV-DOM-014`; `BLUTO-SEC-TENANT-001` sections 1 through 4.

Every identity assertion must carry rule or human-decision provenance and remain reproducible from the versioned ruleset or review decision. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 4, 7, 10, and `REQ-ID-02`; `BLUTO-DOM-OVERVIEW-001` section 8 `INV-DOM-005` and `INV-DOM-011`; `BLUTO-DOM-STATE-001` document body.

Hometown and other consumers are read-only. They must use governed mappings and may not infer identity through ad hoc matching. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 2, 8, and 10; `BLUTO-DOM-OVERVIEW-001` section 8 `INV-DOM-012` and `INV-DOM-013`; `BLUTO-CONTRACT-CONSUMER-001` sections 1, 2, and 5.

## Bounded Contexts And Aggregate Ownership

Rule Management owns rule definitions, immutable versions, validation, activation, and retirement. Its aggregate is the Rule Aggregate rooted at Rule Definition, with `RuleRepository` as its repository port. Controlling authority: `BLUTO-DOM-BC-001` document body; `BLUTO-DOM-AGGREGATE-001` document body; `BLUTO-ARCH-REF-001` sections 4.2 and 6.

Identity Resolution owns Party creation, deterministic matching, source links, and deferral. Its aggregate is the Party Aggregate rooted at Party, containing Party Source Links as governed members; `PartyRepository` persists complete Party aggregates and link history. Controlling authority: `BLUTO-DOM-BC-001` document body; `BLUTO-DOM-AGGREGATE-001` document body; `BLUTO-DOM-REPOSITORY-001` document body; `BLUTO-ARCH-REF-001` sections 4.3 and 6.

Identity Lifecycle owns merge, split, retirement, lineage, and temporal reconstruction. Its aggregate is the Merge Aggregate rooted at Merge Record, with `MergeRepository` as the lineage and point-in-time reconstruction port. Controlling authority: `BLUTO-DOM-BC-001` document body; `BLUTO-DOM-AGGREGATE-001` document body; `BLUTO-DOM-STATE-001` document body; `BLUTO-ARCH-REF-001` sections 4.4 and 6.

Stewardship owns review cases, evidence, reviewer decisions, and immutable dispositions. Its aggregate is the Review Aggregate rooted at Review Case, with `ReviewRepository` for cases, decisions, and queue queries. Controlling authority: `BLUTO-DOM-BC-001` document body; `BLUTO-DOM-AGGREGATE-001` document body; `BLUTO-DOM-REPOSITORY-001` document body; `BLUTO-ARCH-REF-001` sections 4.5 and 6.

Identity Governance owns audit evidence, policy findings, compliance projections, and operational metrics. Its aggregate boundary is not definitively established; `GovernanceRepository` is explicitly provisional and must be confirmed or replaced before becoming a contract. Controlling authority: `BLUTO-DOM-BC-001` document body; `BLUTO-ARCH-REF-001` sections 4.6, 6, and 11.

Contexts are semantic boundaries, not mandatory deployable services. They interact through public ports and domain/integration events. Direct cross-context persistence writes and cyclic dependencies are prohibited. Controlling authority: `BLUTO-DOM-BC-001` document body; `BLUTO-ARCH-REF-001` sections 2, 5, 7, and 10; `BLUTO-ENG-ARCH-001` sections 2 and 3.

## Approved Architecture Topology

The initial implementation topology is a modular monolith codebase with independently executable API, worker, and stewardship entry points. Separate services require an ADR supported by scaling, resilience, or ownership evidence. Controlling authority: `BLUTO-ARCH-LOGICAL-001` section 7; `BLUTO-ARCH-TECH-001` section 2 `TD-01`; `BLUTO-ARCH-REF-001` section 7.

The v1 physical topology contains `bluto-api`, `bluto-worker`, `bluto-stewardship`, a database migration job, an outbox publisher embedded in the worker or separately executable, and scheduled reconciliation and maintenance jobs. Controlling authority: `BLUTO-ARCH-DEPLOY-001` section 2; `BLUTO-ARCH-PHYSICAL-001` sections 2 and 4.

The authoritative data store is a dedicated managed PostgreSQL database. The v1 persistence layout uses context-owned schemas `rule_mgmt`, `identity_resolution`, `identity_lifecycle`, `stewardship`, `identity_governance`, and `integration_outbox`; cross-schema writes are constrained by ownership and roles. Controlling authority: `BLUTO-ARCH-TECH-001` section 2 `TD-02`; `BLUTO-ARCH-DATA-001` sections 2 and 3; `BLUTO-ARCH-PHYSICAL-001` section 3.

State changes that publish facts must use a transactional outbox so domain state and publishable facts commit atomically. Integration-event schemas are broker-neutral; no broker is selected by the baseline. Controlling authority: `BLUTO-ARCH-TECH-001` section 2 `TD-03` and `TD-07`; `BLUTO-ARCH-RUNTIME-001` sections 2, 6, and 7; `BLUTO-ARCH-INTEGRATION-001` section 5.

Resolution is scheduled incremental batch with an authorized on-demand trigger. It is not a streaming-resolution or query-time-matching service. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 2, 8, and 12; `BLUTO-SCOPE-001` document body; `BLUTO-ARCH-TECH-001` section 2 `TD-05`; `BLUTO-ARCH-RUNTIME-001` sections 2 and 3.

Telemetry must be OpenTelemetry-compatible at the interface level, but the observability backend/vendor is deferred. Controlling authority: `BLUTO-ARCH-TECH-001` section 2 `TD-08` and section 3; `BLUTO-ENG-OBS-001` document body; `BLUTO-OPS-OBS-001` document body.

Infrastructure must be declared as code, with immutable artifacts and reproducible releases, but cloud provider and CI platform remain deferred. Controlling authority: `BLUTO-ARCH-TECH-001` section 2 `TD-09` and section 3; `BLUTO-OPS-INFRA-001` document body; `BLUTO-OPS-BUILD-001` document body.

## Contract Surfaces

The approved contract classes are Interactive API, Batch ingestion, Command, Query, Integration event, Repository port, and Operational contract. Persistence records and domain objects are never public contracts. Controlling authority: `BLUTO-CONTRACT-ARCH-001` sections 2 and 3.

The surface inventory is Mapping Query API, Point-in-Time Resolution API, Batch Change Feed, Stewardship Query and Decision API, Rule Administration API, Lifecycle Command API, Resolution Trigger API, Source Adapter Candidate Contract, Integration Event Catalogue, and Health and Operational Contract. Controlling authority: `BLUTO-CONTRACT-ARCH-001` section 4; `BLUTO-CONTRACT-TRACE-001` section 1.

Interactive APIs use versioned HTTP/JSON with major versions in the base path, required correlation headers, idempotency keys for retryable commands, cursor pagination for unbounded collections, and stable HTTP semantics. Controlling authority: `BLUTO-ARCH-TECH-001` section 2 `TD-06`; `BLUTO-CONTRACT-API-001` sections 1 through 7.

The present OpenAPI projection defines `resolveCurrentParty`, `resolvePartyAtTime`, and `recordReviewDecision` operations and their limited response/request schemas. Controlling authority: `docs/04-contracts/schemas/bluto-v1.openapi.yaml`; `BLUTO-CONTRACT-API-001` sections 5 and 6.

The command catalogue is `RunIncrementalResolution`, `TriggerResolution`, `RecordReviewDecision`, `MergeParties`, `SplitParty`, `RetireParty`, `PublishRuleVersion`, `ActivateRuleVersion`, and `RetireRuleVersion`; each command requires the documented command envelope. Controlling authority: `BLUTO-CONTRACT-CQ-001` sections 1 and 2; `docs/04-contracts/schemas/merge-parties.v1.schema.json`.

The query catalogue is `ResolveCurrentParty`, `ResolvePartyAtTime`, `GetPartyLinks`, `ListLinkChanges`, `GetReviewCase`, `ListReviewQueue`, `GetRuleVersion`, `GetBatchRun`, and `GetAuditEvidence`; responses distinguish absent, unauthorized, stale, degraded, merged, and retired outcomes. Controlling authority: `BLUTO-CONTRACT-CQ-001` sections 3 through 5.

Published integration events include `PartyCreated.v1`, `SourceLinkEstablished.v1`, `SourceLinkSuperseded.v1`, `PartyMerged.v1`, `PartySplit.v1`, `PartyRetired.v1`, `ReviewCaseOpened.v1`, `ReviewDecisionRecorded.v1`, `RuleVersionPublished.v1`, `RuleActivated.v1`, `ResolutionBatchCompleted.v1`, and `PolicyViolationDetected.v1`; delivery is at least once and ordering is only per aggregate partition where supported. Controlling authority: `BLUTO-CONTRACT-EVENT-001` sections 1 through 4; `docs/04-contracts/schemas/source-link-established.v1.schema.json`.

Contract field names use `lower_snake_case`, timestamps are RFC 3339 UTC, confidence values use bounded decimals, enumerations are closed unless declared extensible, effective intervals are `[effective_from,effective_to)`, and external schemas must not expose raw strong identifiers or HMAC tokens. Controlling authority: `BLUTO-CONTRACT-SCHEMA-001` sections 1 through 6.

## Security Constraints

Every trust-zone crossing must be authenticated, authorized, encrypted, logged, rate-limited where applicable, and contract validated. Controlling authority: `BLUTO-SEC-ARCH-001` sections 2 and 3.

Humans use enterprise SSO with phishing-resistant MFA for privileged roles. Workloads use managed workload identity and short-lived tokens. Static shared credentials are prohibited. Controlling authority: `BLUTO-SEC-IAM-001` section 1.

Authorization roles are Mapping Reader, Mapping Bulk Consumer, Data Steward Reviewer, Rule Administrator, Lifecycle Operator, Security Auditor, Platform Operator, and Break-Glass Administrator; policy inputs include subject, workload, tenant scope, operation, resource, environment, reason/evidence, and risk context. Controlling authority: `BLUTO-SEC-IAM-001` sections 2 and 3.

Separation of duties is required for high-impact rule activation, privileged reviewer overrides, and break-glass actions. Controlling authority: `BLUTO-SEC-IAM-001` section 4; `BLUTO-SEC-AUDIT-001` section 1.

Restricted data handling applies to HMAC tokens, tenant-linked mappings, review evidence, merge/split lineage, and audit records; Tier-0 secret handling applies to HMAC key material. Controlling authority: `BLUTO-SEC-DATA-001` sections 1 and 2; `BLUTO-SEC-CRYPTO-001` sections 1 through 4.

Priority threats include cross-tenant link creation, raw identifier leakage, HMAC key compromise, reviewer abuse, source adapter spoofing, replay/duplicate commands, direct database mutation, outbox tampering, Party-link enumeration, and AI-generated unsafe implementation. Controlling authority: `BLUTO-SEC-THREAT-001` sections 2 and 3.

## Required Test Gates

Repository-level quality gates are Metadata Validation, Identity Validation, Cross-Reference Validation, Dependency Validation, Authority Validation, Traceability Validation, Release Validation, Evidence Validation, Certification Validation, and Repository Audit. Controlling authority: `repository/validation/quality-gates.yaml`; `repository/validation/execution-order.yaml`; `BLUTO-BASE-QG-001` document.

Validation standards map `GATE-01` through `GATE-06` to `BLUTO-BASE-VAL-001` through `BLUTO-BASE-VAL-006`. Controlling authority: `repository/validation/validation-standards.yaml`; `repository/rgtm/repository-governance-traceability.yaml`.

Implementation release gates require critical requirement tests, no unresolved critical/high security finding, contract compatibility, migration and rollback/forward-recovery, zero cross-tenant and raw-identifier violations, performance within approved budgets, current restore evidence, and complete traceability. Controlling authority: `BLUTO-TEST-GATE-001` document body; `BLUTO-TEST-STRATEGY-001` document body.

Domain tests must cover aggregate invariants, value-object validation, command preconditions, transitions, event emission, and error outcomes using fake clocks, deterministic identifiers, and in-memory ports. Controlling authority: `BLUTO-TEST-DOMAIN-001` document body.

Contract tests must validate OpenAPI, JSON, and event schema examples and error outcomes, plus provider/consumer compatibility for supported versions. Controlling authority: `BLUTO-TEST-CONTRACT-001` document body; `BLUTO-CONTRACT-COMPAT-001` sections 1 through 4.

Security tests must cover authentication, authorization, cross-tenant negatives, concealed existence, reviewer separation, break-glass audit, secret scanning, raw-identifier scans, token/key isolation, dependency scanning, infrastructure policy, abuse limits, and penetration focus areas. Controlling authority: `BLUTO-TEST-SEC-001` document body; `BLUTO-SEC-SDLC-001` document body.

Acceptance tests must include the seeded CRM/ledger/Intrepid demo, ambiguous review case, merge history, split correction, no-readable-identifier audit, cross-tenant collision, and ID non-reuse exercise. Controlling authority: `BLUTO-TEST-ACCEPT-001` document body; `BLUTO_IDENTITY_SPINE_v0.1` section 11.

Performance and resilience tests must measure API latency, batch throughput, source backpressure, queue depth, database contention, temporal query plans, outbox lag, failover, backup restore, duplicate events/pages, and degraded dependencies. Controlling authority: `BLUTO-TEST-PERF-001` document body; `BLUTO-TEST-INTEGRATION-001` document body.

## Deployment And Operational Constraints

Environments are Development, Integration, Preproduction, and Production; Production is isolated by account/subscription/project, network, keys, database, telemetry, and access roles. Controlling authority: `BLUTO-ARCH-DEPLOY-001` section 3; `BLUTO-OPS-INFRA-001` document body.

Production deployables use distinct workload identities, private endpoints where available, scoped database roles and key-vault permissions, and no shared human credentials. Controlling authority: `BLUTO-ARCH-DEPLOY-001` section 7; `BLUTO-ARCH-PHYSICAL-001` section 5; `BLUTO-SEC-IAM-001` section 1.

Deployments use immutable artifacts, signed provenance, automated migration gates, smoke tests, progressive exposure, and compatible expand-migrate-contract database change sequencing. Controlling authority: `BLUTO-ARCH-DEPLOY-001` section 6; `BLUTO-OPS-BUILD-001` document body; `BLUTO-OPS-RELEASE-001` document body.

Operational SLOs are 99.9% mapping-read availability, P95 mapping query at or below 250 ms inside the service boundary, scheduled batch completion within the approved window, RPO at or below 15 minutes, and RTO at or below 4 hours. Correctness, tenant isolation, and raw-data controls cannot be traded for error budget. Controlling authority: `BLUTO-OPS-SLO-001` document body.

Runbooks are required for failed batches, stale sources, conflict spikes, review backlog, outbox lag, API degradation, database saturation, key-vault outage, migration failure, false merge, cross-tenant incident, raw identifier finding, backup failure, and regional outage. Operators must not correct identity by direct database mutation. Controlling authority: `BLUTO-OPS-RUNBOOK-001` document body; `BLUTO-SEC-IR-001` sections 1 through 3.

Backups must be encrypted and restore-tested quarterly, proving RPO/RTO, temporal history, tenant isolation, outbox consistency, reviewer evidence, and application compatibility. Controlling authority: `BLUTO-OPS-DR-001` document body; `BLUTO-ARCH-DATA-001` section 9.

## Prohibited Implementation Choices

Do not implement golden-record, master-customer, source-attribute survivorship, or "best value" behavior. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 1 and 10; `BLUTO-NONGOALS-001` document body; `BLUTO-ARCH-REF-001` section 10.

Do not persist or log readable strong identifiers; do not expose HMAC tokens externally; do not keep raw source exports in Bluto. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 5 and 10; `BLUTO-SEC-ARCH-001` section 4; `BLUTO-ARCH-PHYSICAL-001` section 7.

Do not implement fuzzy, probabilistic, machine-learned, or sub-threshold auto-linking in v1. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 4, 10, and 12; `BLUTO-NONGOALS-001` document body.

Do not infer identity at query time in hometown or any consumer; do not let hometown write Bluto state; do not grant direct hometown database access to mutable internal tables. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 2 and 10; `BLUTO-CONTRACT-CONSUMER-001` sections 2 and 5; `BLUTO-ARCH-PHYSICAL-001` section 7.

Do not treat bounded contexts as mandatory microservices, use one database per bounded context in v1 without approved justification, or use direct cross-context repository/table writes. Controlling authority: `BLUTO-ARCH-REF-001` sections 2, 7, and 10; `BLUTO-ARCH-PHYSICAL-001` section 7; `BLUTO-DOM-BC-001` document body.

Do not select a language/runtime, web framework, ORM, migration tool, broker, CI platform, cloud provider, or observability backend without an approved downstream decision. Controlling authority: `BLUTO-ARCH-TECH-001` section 3; `repository/releases/audits/enterprise-repository-audit.json` informational findings.

Do not bypass authorization, tenant isolation, deterministic matching, audit, or data minimization through feature flags or AI-generated shortcuts. Controlling authority: `BLUTO-ENG-CONFIG-001` document body; `BLUTO-AI-CODE-001` document body; `BLUTO-AI-GUARD-001` document body.

## Unresolved Decisions Requiring ADR Or Approved Decision

Initial implementation is blocked from application code until an approved work package defines file scope, tests-first expectations, and selected implementation technologies. Controlling authority: `BLUTO-AI-PROMPT-001` document body; `BLUTO-AI-CONTEXT-001` section "Governing rules"; `BLUTO-ARCH-TECH-001` section 3.

Party Link aggregate boundary remains open in the domain overview and must be reconciled with the later Party Aggregate catalogue before implementation details rely on a transactional model. Controlling authority: `BLUTO-DOM-OVERVIEW-001` sections 7.2 and 19; `BLUTO-DOM-AGGREGATE-001` document body; `BLUTO-ARCH-RUNTIME-001` section 6.

Cross-tenant authorization model requires an approved entity, lifecycle, approver, evidence model, effective interval, and revocation semantics before cross-tenant resolution is implemented. Controlling authority: `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-SEC-TENANT-001` section 3.

Review stewardship ownership remains a business decision before the v1.1 review workflow is operationally complete. Controlling authority: `BLUTO-DOM-OVERVIEW-001` sections 6.5 and 19; `BLUTO_IDENTITY_SPINE_v0.1` section 13.

Ledger and CRM referential-key availability must be inspected before v1 coverage and source adapter work are committed. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` section 13; `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-ARCH-INTEGRATION-001` section 3.

Merge authority and approval threshold, split planning semantics, retirement semantics, and ruleset activation semantics require approved decisions before lifecycle and rule execution implementation. Controlling authority: `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-DOM-STATE-001` document body; `BLUTO-SEC-IAM-001` section 4.

Evidence retention minimum/maximum and privacy deletion/tombstone behavior require approved legal/records/security decisions before storing review/candidate evidence beyond the documented minimum. Controlling authority: `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-SEC-DATA-001` sections 3 and 4; `BLUTO-ARCH-DATA-001` section 8.

Answer Trace Envelope v0.2 fields for `party_id` and ruleset version require a two-team contract change with hometown and Bluto before landing in either project. Controlling authority: `BLUTO_IDENTITY_SPINE_v0.1` sections 7 and 13; `BLUTO-DOM-OVERVIEW-001` section 19.

Governance Evidence Model and persistence boundary remain unresolved and must be confirmed or replaced before `GovernanceRepository` becomes a contract. Controlling authority: `BLUTO-ARCH-REF-001` sections 4.6 and 11.

Cloud provider, language/runtime, web framework, ORM, migration tool, broker, CI platform, and observability backend are explicitly deferred. Controlling authority: `BLUTO-ARCH-TECH-001` section 3.

Digital signing requires enterprise PKI integration and continuous certification automation belongs to Baseline C. Controlling authority: `repository/releases/audits/enterprise-repository-audit.json` informational findings.
