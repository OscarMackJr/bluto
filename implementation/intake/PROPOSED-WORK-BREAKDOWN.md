# Proposed Work Breakdown

This breakdown is intake-only. It proposes sequencing for future approved work packages and does not authorize code, infrastructure, migrations, or executable contract generation.

## Sequencing Principles

Work should proceed documentation-first, architecture-first, contracts-first, tests-first, and code-last. Controlling authority: `BLUTO-BASE-GOV-001` section "Constitutional principles"; `BLUTO-GOV-DECISION-001` `DEC-001`; `BLUTO-ARCH-PRINCIPLES-001` principles 10 through 12.

Work packages must cite stable Document IDs and exact sections, define file scope, list prohibited files, preserve invariants, include tests to add first, define gates, and require evidence. Controlling authority: `BLUTO-AI-PROMPT-001` body; `BLUTO-AI-CODE-001` body; `BLUTO-AI-VALIDATE-001` body.

Large work should be decomposed by vertical behavior and aggregate boundary, not arbitrary file count. Controlling authority: `BLUTO-AI-PROMPT-001` body; `BLUTO-DOM-AGGREGATE-001` body; `BLUTO-ARCH-REF-001` section 6.

## Phase 0 - Decision And Work-Package Readiness

| Work item | Outcome | Required decisions/evidence | Controlling source and section |
|---|---|---|---|
| WP-00.01 | Approve implementation work-package template for Bluto | Work package contains objective, non-goals, references, file scope, invariants, contracts, tests, gates, evidence, and completion definition | `BLUTO-AI-PROMPT-001` body |
| WP-00.02 | Resolve deferred implementation technology selections | Approved decisions for language/runtime, framework, ORM/data access, migration tool, cloud provider, broker if needed, CI platform, and observability backend | `BLUTO-ARCH-TECH-001` section 3; `BLUTO-AI-HUMAN-001` body |
| WP-00.03 | Reconcile Party Link aggregate boundary | ADR or approved decision aligning `BLUTO-DOM-OVERVIEW-001` open aggregate question with later Party Aggregate ownership | `BLUTO-DOM-OVERVIEW-001` sections 7.2 and 19; `BLUTO-DOM-AGGREGATE-001` body |
| WP-00.04 | Define cross-tenant authorization model | Approved entity, lifecycle, owner, approver, evidence, effective interval, and revocation procedure | `BLUTO-SEC-TENANT-001` section 3; `BLUTO-DOM-OVERVIEW-001` section 19 |
| WP-00.05 | Confirm source referential-key availability | Inspection result for ledger, CRM, and Intrepid deterministic keys and v1 coverage impact | `BLUTO_IDENTITY_SPINE_v0.1` section 13; `BLUTO-ARCH-INTEGRATION-001` section 3 |
| WP-00.06 | Complete missing contract schemas | Published schemas/examples for all implemented commands, queries, events, errors, and operational surfaces | `BLUTO-CONTRACT-ARCH-001` section 5; `BLUTO-CONTRACT-CQ-001` sections 2 and 4; `BLUTO-CONTRACT-EVENT-001` section 2 |

## Phase 1 - Contract And Test Design Before Code

| Work item | Outcome | Scope guard | Controlling source and section |
|---|---|---|---|
| WP-01.01 | Requirement/invariant test matrix | Tests mapped to `REQ-ID-01` through `REQ-ID-07`, domain invariants, transitions, threats, and acceptance scenarios | `BLUTO_IDENTITY_SPINE_v0.1` section 11; `BLUTO-TEST-GATE-001` body |
| WP-01.02 | Domain test plan | Positive, negative, boundary, and property-based tests for Party, links, rules, reviews, lifecycle, and value objects | `BLUTO-TEST-DOMAIN-001` body; `BLUTO-DOM-STATE-001` body |
| WP-01.03 | Contract test plan | Provider, consumer, compatibility, event envelope, error, duplicate, and out-of-order tests | `BLUTO-TEST-CONTRACT-001` body; `BLUTO-CONTRACT-COMPAT-001` section 4 |
| WP-01.04 | Security test plan | Authentication, authorization, cross-tenant negatives, concealment, reviewer separation, secret/raw-data scans, and abuse-rate tests | `BLUTO-TEST-SEC-001` body; `BLUTO-SEC-THREAT-001` sections 2 and 3 |
| WP-01.05 | Acceptance demo design | Seeded CRM/ledger/Intrepid demo, ambiguous review, merge history, split correction, no-readable-identifier audit, cross-tenant collision, and ID non-reuse evidence design | `BLUTO-TEST-ACCEPT-001` body |

## Phase 2 - Domain/Core Vertical Slices After Blockers Clear

| Work item | Outcome | Scope guard | Controlling source and section |
|---|---|---|---|
| WP-02.01 | Shared Kernel values | Party, Tenant, Source, Rule, Effective Date Range, Correlation, Causation, and common domain error concepts only | `BLUTO-ARCH-LOGICAL-001` section 8; `BLUTO-DOM-VALUEOBJECT-001` body |
| WP-02.02 | Party and Source Link behavior | Immutable Party ID, lifecycle transitions, one active link per source identity/scope, effective dating, provenance, and no business attributes | `BLUTO-DOM-AGGREGATE-001` body; `BLUTO-DOM-OVERVIEW-001` section 8 |
| WP-02.03 | Rule Management behavior | Rule definition/version lifecycle, immutable published versions, activation/retirement, and reproducibility | `BLUTO-DOM-STATE-001` body; `BLUTO-ARCH-REF-001` section 4.2 |
| WP-02.04 | Resolution behavior | Deterministic ordered rule evaluation, idempotent link assertion, deferral of ambiguous/conflicting candidates, no streaming/query-time resolution | `BLUTO_IDENTITY_SPINE_v0.1` sections 4 and 8; `BLUTO-ARCH-RUNTIME-001` section 2 |
| WP-02.05 | Stewardship behavior | Review case lifecycle, immutable decisions, minimized evidence, and separate accepted-decision resolution command | `BLUTO_IDENTITY_SPINE_v0.1` section 9; `BLUTO-ARCH-RUNTIME-001` section 4 |
| WP-02.06 | Lifecycle behavior | Merge, split, retirement, lineage, point-in-time reconstruction, and no ID deletion/reuse | `BLUTO_IDENTITY_SPINE_v0.1` section 7; `BLUTO-ARCH-RUNTIME-001` section 5 |

## Phase 3 - Persistence, Integration, And Runtime After Technology Decisions

| Work item | Outcome | Scope guard | Controlling source and section |
|---|---|---|---|
| WP-03.01 | PostgreSQL persistence design | Context-owned schemas, constraints, temporal model, outbox tables, roles, and migration sequencing | `BLUTO-ARCH-PHYSICAL-001` section 3; `BLUTO-ARCH-DATA-001` sections 3 through 10; `BLUTO-ENG-DATA-001` body |
| WP-03.02 | Application-service transactions | Atomic boundaries for Party creation/link/outbox, link supersession, review decision, merge/split, rule publication, and batch page commits | `BLUTO-ARCH-RUNTIME-001` section 6 |
| WP-03.03 | Source adapters | Read-only incremental adapters for Nexus CRM, Intrepid, and ledger with watermarks, load budgets, retries, classification, and reconciliation evidence | `BLUTO-ARCH-INTEGRATION-001` sections 2, 3, and 6 |
| WP-03.04 | Hometown consumption | Current mapping, point-in-time mapping, link changes, merge-chain resolution, version/provenance metadata, and degradation semantics | `BLUTO-ARCH-INTEGRATION-001` sections 4 and 7; `BLUTO-CONTRACT-CONSUMER-001` section 2 |
| WP-03.05 | Outbox publication | Broker-neutral integration events, at-least-once delivery, dedupe by event ID, no raw identifiers/tokens/internal tables | `BLUTO-ARCH-INTEGRATION-001` section 5; `BLUTO-CONTRACT-EVENT-001` sections 1 through 3 |

## Phase 4 - Security, Operations, And Release Evidence

| Work item | Outcome | Scope guard | Controlling source and section |
|---|---|---|---|
| WP-04.01 | IAM and authorization implementation | Enterprise SSO, workload identity, role policies, separation of duties, and break-glass audit | `BLUTO-SEC-IAM-001` sections 1 through 4 |
| WP-04.02 | Crypto and secret handling | HMAC-SHA-256 tokenization through managed key vault, planned rotation migration, short-lived/scoped secrets, TLS and encryption-at-rest controls | `BLUTO-SEC-CRYPTO-001` sections 1 through 4 |
| WP-04.03 | Tenant isolation enforcement | Tenant claims, application policy checks, repository keys/constraints, filtered queries, tenant partitioning, redacted telemetry, and negative tests | `BLUTO-SEC-TENANT-001` section 2 |
| WP-04.04 | Build and release pipeline | Formatting, dependency-boundary checks, compile/type checks, tests, schema validation, scans, SAST, SBOM, provenance, signing, immutable registry, deployment gates | `BLUTO-OPS-BUILD-001` body; `BLUTO-OPS-RELEASE-001` body |
| WP-04.05 | Observability and runbooks | Dashboards, alerts, telemetry dimensions, operational runbooks, recovery exercises, RPO/RTO evidence, and no direct database mutation procedures | `BLUTO-OPS-OBS-001` body; `BLUTO-OPS-RUNBOOK-001` body; `BLUTO-OPS-DR-001` body |

## Stop Conditions

Implementation must stop if a work item requires a deferred technology selection, unresolved ADR, missing published contract, or unsupported source field. Controlling authority: `BLUTO-AI-GUARD-001` body; `BLUTO-AI-HUMAN-001` body; `BLUTO-AI-CONTEXT-001` body.

Implementation must stop if a change would introduce golden-record behavior, raw strong identifier persistence, fuzzy/probabilistic auto-linking in v1, query-time identity inference, direct hometown writes, direct cross-context SQL, disabled audit, hard-coded secrets, or unversioned endpoints/events. Controlling authority: `BLUTO-AI-CODE-001` body; `BLUTO-AI-GUARD-001` body; `BLUTO-ARCH-REF-001` section 10.
