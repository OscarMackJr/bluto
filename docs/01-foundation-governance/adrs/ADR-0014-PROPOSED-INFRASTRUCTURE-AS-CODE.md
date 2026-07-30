# ADR-0014 - Infrastructure-As-Code Tool

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0014 |
| Artifact ID | ART-BLUTO-ADR-0014-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ARCH-TECH-001, BLUTO-OPS-INFRA-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Infrastructure must be declared as code with immutable artifacts, reproducible environments, isolated production resources, private endpoints where available, workload identities, database roles, Key Vault permissions, and auditable changes.

## Governing Document IDs

`BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-DEPLOY-001`, `BLUTO-OPS-INFRA-001`, `BLUTO-OPS-CHANGE-001`, `BLUTO-SEC-IAM-001`, `BLUTO-SEC-CRYPTO-001`, `BLUTO-OPS-DR-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| Terraform with AzureRM and AzAPI providers | Cloud-portable workflow, strong Azure support, mature plan/apply model, policy integration. |
| Bicep | Excellent Azure-native choice, but less portable if cloud target changes. |
| Pulumi | Strong programming-language model, but adds runtime and imperative-code review complexity. |
| Manual portal/CLI | Not acceptable for reproducible environments. |

## Criteria

Declarative review, plan evidence, state management, Azure resource coverage, policy-as-code integration, drift detection, environment promotion, and team familiarity.

## Security And Operational Impact

Terraform plans provide auditable change evidence. Remote state must be encrypted, access-controlled, locked, and isolated by environment. Secrets must remain in Key Vault, not state outputs.

## Compatibility Impact

Terraform can declare Azure Container Apps, PostgreSQL Flexible Server, Key Vault, identities, networking, monitoring, and registries without changing application semantics.

## Recommendation

Use Terraform as the IaC tool, with AzureRM for stable resources and AzAPI only where AzureRM lacks required coverage. Store remote state in an approved encrypted backend with environment isolation.

## Consequences

Infrastructure modules require versioning and review discipline. Provider upgrades become governed dependency changes.

## Migration Or Replacement Considerations

Bicep may replace Terraform if Azure-only governance mandates it. Migration requires state import/export planning, drift reconciliation, and preservation of environment isolation.
