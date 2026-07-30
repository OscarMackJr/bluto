# Authority Index

## Precedence

| Order | Authority | Controlling source and section |
|---|---|---|
| 0 | Architecture Specification / Bluto Identity Spine | `BLUTO_IDENTITY_SPINE_v0.1` sections 1 through 13; `BLUTO-BASE-GOV-001` "Repository Authority Hierarchy"; `BLUTO-GOV-DECISION-001` `DEC-002` |
| 1 | Repository README | `BLUTO-REPO-README-001` "Repository status", "Required reading order", "Machine registry", "Document control" |
| 2 | Repository Baseline A / Constitution | `BLUTO-BASE-GOV-001` "Mission", "Constitutional principles", "Repository Authority Hierarchy", "Reading order" |
| 3 | Stream 1 - Foundation and Governance | `BLUTO-VISION-001` body; `BLUTO-SCOPE-001` body; `BLUTO-NONGOALS-001` body; `BLUTO-ARCH-PRINCIPLES-001` body; `BLUTO-GOV-DECISION-001` decision table |
| 4 | Stream 2 - Domain Model | `BLUTO-DOM-OVERVIEW-001` sections 1 through 22; `BLUTO-DOM-BC-001` body; `BLUTO-DOM-AGGREGATE-001` body; `BLUTO-DOM-ENTITY-001` body; `BLUTO-DOM-VALUEOBJECT-001` body; `BLUTO-DOM-EVENT-001` body; `BLUTO-DOM-REPOSITORY-001` body; `BLUTO-DOM-STATE-001` body |
| 5 | Stream 3 - Architecture | `BLUTO-ARCH-REF-001` sections 1 through 13; `BLUTO-ARCH-LOGICAL-001` sections 1 through 8; `BLUTO-ARCH-RUNTIME-001` sections 1 through 9; `BLUTO-ARCH-DATA-001` sections 1 through 10; `BLUTO-ARCH-INTEGRATION-001` sections 1 through 8; `BLUTO-ARCH-PHYSICAL-001` sections 1 through 7; `BLUTO-ARCH-DEPLOY-001` sections 1 through 9; `BLUTO-ARCH-CROSSCUT-001` sections 1 through 13; `BLUTO-ARCH-TECH-001` sections 1 through 4 |
| 6 | Stream 4 - Contracts | `BLUTO-CONTRACT-ARCH-001` sections 1 through 5; `BLUTO-CONTRACT-API-001` sections 1 through 7; `BLUTO-CONTRACT-CQ-001` sections 1 through 5; `BLUTO-CONTRACT-EVENT-001` sections 1 through 4; `BLUTO-CONTRACT-SCHEMA-001` sections 1 through 6; `BLUTO-CONTRACT-ERROR-001` sections 1 through 4; `BLUTO-CONTRACT-CONSUMER-001` sections 1 through 5; `BLUTO-CONTRACT-COMPAT-001` sections 1 through 4 |
| 7 | Stream 5 - Security | `BLUTO-SEC-ARCH-001` sections 1 through 4; `BLUTO-SEC-IAM-001` sections 1 through 4; `BLUTO-SEC-CRYPTO-001` sections 1 through 4; `BLUTO-SEC-TENANT-001` sections 1 through 4; `BLUTO-SEC-DATA-001` sections 1 through 4; `BLUTO-SEC-THREAT-001` sections 1 through 3; `BLUTO-SEC-AUDIT-001` sections 1 through 4; `BLUTO-SEC-SDLC-001` body; `BLUTO-SEC-IR-001` sections 1 through 3 |
| 8 | Stream 6 - Engineering Standards | `BLUTO-ENG-ARCH-001` sections 1 through 3; `BLUTO-ENG-CODE-001` body; `BLUTO-ENG-DATA-001` body; `BLUTO-ENG-CONFIG-001` body; `BLUTO-ENG-SUPPLY-001` body; `BLUTO-ENG-ERROR-001` body; `BLUTO-ENG-OBS-001` body; `BLUTO-ENG-REVIEW-001` body; `BLUTO-ENG-REPO-001` body |
| 9 | Stream 7 - Testing | `BLUTO-TEST-STRATEGY-001` body; `BLUTO-TEST-GATE-001` body; `BLUTO-TEST-DOMAIN-001` body; `BLUTO-TEST-CONTRACT-001` body; `BLUTO-TEST-SEC-001` body; `BLUTO-TEST-PERF-001` body; `BLUTO-TEST-ACCEPT-001` body; `BLUTO-TEST-INTEGRATION-001` body |
| 10 | Stream 8 - DevOps | `BLUTO-OPS-ARCH-001` sections 1 and 2; `BLUTO-OPS-BUILD-001` body; `BLUTO-OPS-RELEASE-001` body; `BLUTO-OPS-INFRA-001` body; `BLUTO-OPS-OBS-001` body; `BLUTO-OPS-SLO-001` body; `BLUTO-OPS-DR-001` body; `BLUTO-OPS-RUNBOOK-001` body; `BLUTO-OPS-CHANGE-001` body |
| 11 | Stream 9 - AI Factory | `BLUTO-AI-CONTEXT-001` body; `BLUTO-AI-POLICY-001` body; `BLUTO-AI-PROMPT-001` body; `BLUTO-AI-CODE-001` body; `BLUTO-AI-GUARD-001` body; `BLUTO-AI-HUMAN-001` body; `BLUTO-AI-VALIDATE-001` body |
| 12 | Implementation | `BLUTO-BASE-GOV-001` "Repository Authority Hierarchy"; subordinate to all above |

## Machine-Readable Projections Inspected

| Artifact | Intake use | Controlling source and section |
|---|---|---|
| `repository/lir/logical-identity-registry.yaml` | Document IDs, artifact IDs, status, paths, hashes, dependencies, release IDs | `BLUTO-REPO-README-001` "Machine registry"; `repository/schemas/metadata.schema.json` |
| `repository/lir/document-index.json` | Machine index projection of controlled documents | `BLUTO-REPO-README-001` "Machine registry"; `repository/schemas/metadata.schema.json` |
| `repository/rgtm/repository-governance-traceability.yaml` | Quality gate inventory, validation mappings, dependency traceability | `BLUTO-REPO-README-001` "Machine registry"; `repository/validation/validation-standards.yaml` |
| `repository/rgtm/dependency-graph.json` | Dependency graph projection | `BLUTO-BASE-GOV-001` "Reading order"; `repository/constitution/repository-control-catalog.yaml` controls `RC-DEP-*` |
| `repository/validation/quality-gates.yaml` | Gate names `GATE-01` through `GATE-10` | `BLUTO-BASE-QG-001`; `repository/rgtm/repository-governance-traceability.yaml` `quality_gates` |
| `repository/validation/execution-order.yaml` | Gate sequence and parallel validation slots | `repository/validation/execution-order.yaml`; `repository/constitution/repository-control-catalog.yaml` |
| `repository/validation/validation-standards.yaml` | Mapping from GATE-01 through GATE-06 to validation standards | `repository/validation/validation-standards.yaml`; `repository/rgtm/repository-governance-traceability.yaml` |
| `repository/evidence/rvec/*.json` | Evidence that stream gates were run and passed | `repository/schemas/rvec.schema.json`; sampled `stream1-gate-01.json` and `stream9-gate-06.json` |
| `repository/constitution/repository-baseline.yaml` | Baseline status, principles, RAH projection | `BLUTO-BASE-GOV-001` "Constitutional principles" and "Repository Authority Hierarchy" |
| `repository/constitution/repository-control-objectives.yaml` | Control objectives RCO-01 through RCO-06 | `repository/constitution/repository-control-objectives.yaml` |
| `repository/constitution/repository-control-catalog.yaml` | Control IDs for metadata, identity, reference, dependency, authority, traceability, release, evidence, certification, audit | `repository/constitution/repository-control-catalog.yaml` |
| `repository/metrics/repository-health.yaml` | Health projection: 195 documents, 0 unresolved references, 0 cycles, 0 duplicate IDs, 100% gate coverage, 9 certified streams | `repository/metrics/repository-health.yaml`; `BLUTO-REPO-README-001` "Machine registry" |
| `repository/releases/audits/enterprise-repository-audit.json` | Repository audit outcome and informational findings | `repository/releases/audits/enterprise-repository-audit.json` |
| `release-manifest.json` | Certified release file list, hashes, authoritative spec digest | `release-manifest.json`; `BLUTO-REPO-README-001` "Machine registry" |
| `repository/schemas/metadata.schema.json` | Metadata record shape and status enum | `repository/schemas/metadata.schema.json` |
| `repository/schemas/rvec.schema.json` | RVEC evidence record shape and result enum | `repository/schemas/rvec.schema.json` |

## Normative Technology Decisions

| Decision | Status | Controlling source and section |
|---|---|---|
| Modular monolith first, with API, worker, and stewardship entry points | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-01`; `BLUTO-ARCH-LOGICAL-001` section 7 |
| Dedicated managed PostgreSQL authoritative store | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-02`; `BLUTO-ARCH-DATA-001` section 3 |
| Transactional outbox | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-03`; `BLUTO-ARCH-RUNTIME-001` section 6 |
| Managed key vault and HMAC-SHA-256 | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-04`; `BLUTO-SEC-CRYPTO-001` section 1 |
| Scheduled worker orchestration, not streaming platform for v1 | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-05`; `BLUTO_IDENTITY_SPINE_v0.1` section 8 |
| Versioned HTTP/JSON APIs for interactive contracts | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-06`; `BLUTO-CONTRACT-API-001` section 2 |
| Broker-neutral integration events | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-07`; `BLUTO-CONTRACT-EVENT-001` sections 1 through 4 |
| OpenTelemetry-compatible telemetry interfaces | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-08`; backend deferred by section 3 |
| Infrastructure as code and immutable artifacts | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-09`; `BLUTO-OPS-INFRA-001` body |
| No distributed cache as authority | Selected | `BLUTO-ARCH-TECH-001` section 2 `TD-10`; `BLUTO-CONTRACT-CONSUMER-001` section 4 |
| Language/runtime, web framework, ORM, migration tool, broker, CI platform, cloud provider, observability backend | Deferred | `BLUTO-ARCH-TECH-001` section 3 |
