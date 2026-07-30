# ADR-0015 - Cloud And Deployment Target

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0015 |
| Artifact ID | ART-BLUTO-ADR-0015-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ARCH-TECH-001, BLUTO-ARCH-PHYSICAL-001, BLUTO-ARCH-DEPLOY-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

The baseline defers cloud provider but requires managed PostgreSQL, managed key vault, workload identity, private endpoints where available, separate entry points, scheduled jobs, immutable artifacts, and OpenTelemetry-compatible operations.

## Governing Document IDs

`BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-PHYSICAL-001`, `BLUTO-ARCH-DEPLOY-001`, `BLUTO-OPS-INFRA-001`, `BLUTO-OPS-OBS-001`, `BLUTO-SEC-IAM-001`, `BLUTO-SEC-CRYPTO-001`, `BLUTO-OPS-SLO-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| Azure Container Apps, Azure Database for PostgreSQL Flexible Server, Azure Key Vault, Azure Container Registry, Azure Monitor | Strong fit for managed identity, Key Vault-backed HMAC, managed PostgreSQL, containers, jobs, and OTel-compatible operations. |
| Azure Kubernetes Service | More control, but higher operational burden than v1 needs. |
| AWS ECS/Fargate plus RDS/Secrets Manager | Viable, but Key Vault language and likely enterprise alignment favor Azure. |
| Self-managed Kubernetes/VMs | Excessive operational burden and weaker managed-service posture. |

## Criteria

Managed PostgreSQL, managed key vault, workload identity, container hosting, scheduled jobs, private networking, observability, IaC support, operational simplicity, and SLO support.

## Security And Operational Impact

Azure managed identities and Key Vault support static credential avoidance. Container Apps and scheduled jobs can host API, worker, stewardship, migrations, and batch jobs while minimizing platform operations.

## Compatibility Impact

This target preserves all upstream architecture decisions. It selects platform services but does not mandate external broker use for v1.

## Recommendation

Use Azure as the v1 deployment target: Azure Container Apps for API/worker/stewardship and jobs, Azure Database for PostgreSQL Flexible Server for the authoritative store, Azure Key Vault for HMAC key custody and secrets, Azure Container Registry for images, and Azure Monitor/OpenTelemetry-compatible ingestion for operations.

## Consequences

The implementation becomes Azure-first for v1. Cloud portability remains possible at the architecture level but IaC, identity, and operational runbooks will be Azure-specific.

## Migration Or Replacement Considerations

Migration to AKS, another Azure compute target, or another cloud requires successor ADRs for identity, key custody, database, networking, telemetry, and deployment runbooks.
