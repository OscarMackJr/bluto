# Bluto Cross-Cutting Architecture Concerns

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-CROSSCUT-001 |
| Artifact ID | ART-BLUTO-ARCH-CROSSCUT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-LOGICAL-001, BLUTO-ARCH-RUNTIME-001, BLUTO-ARCH-INTEGRATION-001, BLUTO-ARCH-DATA-001, BLUTO-DOM-REVIEW-001 |
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

This document defines concerns that apply uniformly across modules and deployables.

## 2. Tenant context

Tenant context is explicit from ingress through persistence and telemetry. It is validated against caller authorization and never inferred from mutable payload data. Background jobs operate on declared tenant partitions.

## 3. Identity and authorization

Human users authenticate through enterprise identity. Workloads use managed identities. Authorization is policy-based and distinguishes mapping readers, rule administrators, reviewers, lifecycle operators, auditors, and platform operators.

## 4. Correlation and causation

Every command, batch, review decision, state transition, event, and external request carries a correlation identifier. Events additionally record causation. These identifiers contain no sensitive data.

## 5. Error semantics

Errors use stable categories:

- invalid request;
- unauthorized or forbidden scope;
- not found;
- conflict/invariant violation;
- review required;
- dependency unavailable;
- rate/load budget exceeded;
- transient infrastructure failure;
- internal failure.

External errors never expose tokens, source payloads, SQL, stack traces, or key metadata.

## 6. Observability

Structured logs describe technical operation without sensitive inputs. Metrics distinguish business outcomes from technical failures. Traces cross application and adapter boundaries. Required dimensions include source, tenant-safe identifier, rule version, batch, operation, and outcome, subject to cardinality controls.

## 7. Audit

Security and domain audit records are immutable, time-synchronized, attributable, and queryable. Audit includes rule changes, review actions, merge/split, authorization failures, cross-tenant attempts, secret access, deployment, and administrative operations.

## 8. Configuration

Configuration is validated at startup, environment-specific, externalized, and non-secret. Secrets are referenced, not embedded. Runtime feature flags may control rollout but may not bypass domain or security invariants.

## 9. Time and identifiers

UTC is authoritative. Clock and identifier generation are ports. UUIDs are never reused. Effective time, assertion time, and processing time remain distinct.

## 10. Resilience

Remote calls use deadlines, bounded retries, and circuit breakers. Internal commands are idempotent. Backpressure protects source systems and PostgreSQL. Poison records are quarantined with evidence.

## 11. Data minimization and redaction

No raw strong identifiers are persisted or logged. Evidence shown to stewards is the minimum necessary. Lower environments use synthetic data. Diagnostic exports are access-controlled, time-limited, and audited.

## 12. Compatibility

Every public contract, event, and persistent migration follows additive-first evolution and explicit versioning. Consumer compatibility is tested before release.

## 13. Feature evolution

Feature flags are temporary rollout controls with owners and expiration. A flag cannot authorize probabilistic matching, cross-tenant resolution, or golden-record behavior.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
