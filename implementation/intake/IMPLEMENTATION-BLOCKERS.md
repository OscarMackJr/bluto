# Implementation Blockers

This file lists unresolved decisions and missing prerequisites found during repository intake. No blocker is resolved here.

## Blocking Decisions

| ID | Blocker | Why it blocks implementation | Controlling source and section |
|---|---|---|---|
| BLK-001 | No approved implementation work package has been provided for code generation | AI Factory requires work packages to define objective, non-goals, references, allowed/prohibited files, invariants, contracts, tests, gates, evidence, and completion definition | `BLUTO-AI-PROMPT-001` body; `BLUTO-AI-CONTEXT-001` body |
| BLK-002 | Programming language/runtime is deferred | No application code can be scaffolded without selecting a runtime, and the baseline explicitly defers it | `BLUTO-ARCH-TECH-001` section 3 |
| BLK-003 | Web framework is deferred | API and stewardship entry points cannot be implemented without an approved framework decision | `BLUTO-ARCH-TECH-001` section 3; `BLUTO-ARCH-DEPLOY-001` section 2 |
| BLK-004 | ORM/data-access approach and migration tool are deferred | PostgreSQL is selected, but implementation of repositories and migrations needs approved tooling | `BLUTO-ARCH-TECH-001` section 3; `BLUTO-ENG-DATA-001` body |
| BLK-005 | Cloud provider is deferred | Managed PostgreSQL, key vault, scheduler, private endpoints, identities, telemetry, and infrastructure implementation need a provider decision | `BLUTO-ARCH-TECH-001` section 3; `BLUTO-OPS-INFRA-001` body |
| BLK-006 | CI platform is deferred | Build, scan, validation, evidence, and deployment gates cannot be implemented as pipeline code without platform selection | `BLUTO-ARCH-TECH-001` section 3; `BLUTO-OPS-BUILD-001` body |
| BLK-007 | Broker/queue transport is deferred | Events are broker-neutral, but external event publication cannot be built until transport requirements are approved | `BLUTO-ARCH-TECH-001` section 3; `BLUTO-CONTRACT-EVENT-001` section 3 |
| BLK-008 | Observability backend is deferred | OpenTelemetry-compatible interfaces are selected, but dashboards, alerting, retention, and backend-specific operations need a backend decision | `BLUTO-ARCH-TECH-001` sections 2 `TD-08` and 3; `BLUTO-OPS-OBS-001` body |
| BLK-009 | Party Link aggregate boundary needs formal reconciliation | The domain overview leaves the Party Link aggregate boundary open, while later documents place Party Source Links inside the Party Aggregate; implementation must not silently choose transactional boundaries | `BLUTO-DOM-OVERVIEW-001` sections 7.2 and 19; `BLUTO-DOM-AGGREGATE-001` body; `BLUTO-ARCH-RUNTIME-001` section 6 |
| BLK-010 | Cross-tenant authorization model is incomplete | Cross-tenant resolution requires an explicit authorization record, lifecycle, approver, evidence, effective interval, and revocation semantics before implementation | `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-SEC-TENANT-001` section 3 |
| BLK-011 | Stewardship business owner is unnamed | Review workflow requires an accountable business role before v1.1 operational completeness | `BLUTO-DOM-OVERVIEW-001` sections 6.5 and 19; `BLUTO_IDENTITY_SPINE_v0.1` section 13 |
| BLK-012 | Ledger and CRM referential-key availability is unknown | v1 coverage and source adapter priority depend on whether source systems already contain usable deterministic referential keys | `BLUTO_IDENTITY_SPINE_v0.1` section 13; `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-ARCH-INTEGRATION-001` section 3 |
| BLK-013 | Merge authority and approval threshold are undefined | Lifecycle operations require elevated authorization and possibly dual control; implementation must not invent role authority | `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-SEC-IAM-001` section 4; `BLUTO-ARCH-RUNTIME-001` section 5 |
| BLK-014 | Split planning semantics are undefined | Split atomicity, new Party creation, unaffected Party reuse, and correction representation must be approved before lifecycle implementation | `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO_IDENTITY_SPINE_v0.1` section 7 |
| BLK-015 | Retirement semantics are undefined | The baseline defines `active -> retired`, but not when retirement is appropriate versus an active Party with no current links | `BLUTO-DOM-OVERVIEW-001` sections 9.1 and 19 |
| BLK-016 | Ruleset activation semantics are undefined | Publication, effective time, rollback, and concurrent-run behavior are open and affect deterministic reproducibility | `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-DOM-STATE-001` body |
| BLK-017 | Evidence retention limits are undefined | Candidate/review evidence storage needs minimum, maximum, legal deletion, restriction, and tombstone policy before durable evidence implementation | `BLUTO-DOM-OVERVIEW-001` section 19; `BLUTO-ARCH-DATA-001` section 8; `BLUTO-SEC-DATA-001` sections 3 and 4 |
| BLK-018 | ATE v0.2 contract change is unresolved | `party_id` and ruleset version in Answer Trace Envelope require hometown and Bluto review before landing | `BLUTO_IDENTITY_SPINE_v0.1` sections 7 and 13; `BLUTO-DOM-OVERVIEW-001` section 19 |
| BLK-019 | Governance Evidence Model boundary is unresolved | `GovernanceRepository` is provisional and must be confirmed or replaced before becoming a contract or implementation boundary | `BLUTO-ARCH-REF-001` sections 4.6 and 11 |
| BLK-020 | Complete contract schema set is not present | The catalogue is broader than the checked-in OpenAPI and two JSON schemas, so undocumented endpoints/events/commands cannot be implemented | `BLUTO-CONTRACT-ARCH-001` sections 4 and 5; `BLUTO-CONTRACT-CQ-001` sections 2 and 4; `BLUTO-CONTRACT-EVENT-001` section 2; checked schemas under `docs/04-contracts/schemas` |
| BLK-021 | Enterprise PKI integration is not complete | Signed artifacts are required by delivery standards, but the audit says digital signing is schema-ready and requires enterprise PKI integration | `BLUTO-OPS-BUILD-001` body; `repository/releases/audits/enterprise-repository-audit.json` informational findings |
| BLK-022 | Continuous certification automation is future baseline work | Repository audit says continuous certification automation belongs to Baseline C, so automated certification cannot be assumed now | `repository/releases/audits/enterprise-repository-audit.json` informational findings |

## Concise Blocker List

- Missing approved implementation work package.
- Deferred language/runtime, framework, ORM/data access, migration tool, cloud provider, broker, CI platform, and observability backend.
- Open Party Link aggregate boundary reconciliation.
- Incomplete cross-tenant authorization model.
- Unnamed stewardship business owner.
- Unknown ledger/CRM referential-key availability.
- Undefined merge authority, split semantics, retirement semantics, and ruleset activation semantics.
- Undefined evidence retention and privacy tombstone policy.
- Unresolved hometown/Bluto ATE v0.2 contract change.
- Provisional Governance Evidence Model boundary.
- Incomplete published schema set for the full contract catalogue.
- Enterprise PKI and continuous certification automation not complete in this baseline.
