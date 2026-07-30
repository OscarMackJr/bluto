# Technology Selection Summary

| Metadata | Value |
|---|---|
| Document ID | BLUTO-IMPL-TECH-SUMMARY-001 |
| Artifact ID | ART-BLUTO-IMPL-TECH-SUMMARY-001-v0.1.0 |
| Version | 0.1.0 |
| Status | Proposed |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-ADR-0002, BLUTO-ADR-0003, BLUTO-ADR-0004, BLUTO-ADR-0005, BLUTO-ADR-0006, BLUTO-ADR-0007, BLUTO-ADR-0008, BLUTO-ADR-0009, BLUTO-ADR-0010, BLUTO-ADR-0011, BLUTO-ADR-0012, BLUTO-ADR-0013, BLUTO-ADR-0014, BLUTO-ADR-0015, BLUTO-ADR-0016, BLUTO-ADR-0017 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Purpose

This package proposes implementation technology decisions only. It does not create production code and does not mark any ADR Approved or Baselined.

## Preserved Upstream Decisions

The proposed selections preserve modular-monolith-first implementation, separate API, worker, and stewardship entry points, managed PostgreSQL, transactional outbox, HTTP/JSON interactive APIs, deterministic scheduled batch resolution, tenant isolation, Key Vault-backed HMAC-SHA-256, no raw strong identifiers at rest, and no probabilistic matching in v1.

## Proposed Selection Matrix

| Area | Proposed selection | ADR |
|---|---|---|
| Primary programming language and supported version | C# on .NET 10 LTS, target `net10.0` | `BLUTO-ADR-0001` |
| Application framework | ASP.NET Core 10 Minimal APIs and .NET Generic Host workers | `BLUTO-ADR-0002` |
| Dependency injection | Microsoft.Extensions.DependencyInjection with constructor injection and validated options | `BLUTO-ADR-0003` |
| PostgreSQL access | Npgsql `DataSource` plus Dapper and explicit SQL repositories | `BLUTO-ADR-0004` |
| Schema migration tool | Flyway SQL migrations executed by a dedicated migration job | `BLUTO-ADR-0005` |
| API and OpenAPI tooling | ASP.NET Core OpenAPI generation, build-time documents, Spectral, and OpenAPI diff checks | `BLUTO-ADR-0006` |
| JSON Schema tooling | JSON Schema Draft 2020-12, JsonSchema.Net runtime/test validation, Ajv CI validation | `BLUTO-ADR-0007` |
| Test framework | xUnit.net v3 with Testcontainers for PostgreSQL integration tests | `BLUTO-ADR-0008` |
| Property-based testing | FsCheck integrated with xUnit | `BLUTO-ADR-0009` |
| Static analysis and formatting | .NET Roslyn analyzers, `.editorconfig`, nullable, warnings-as-errors, `dotnet format` | `BLUTO-ADR-0010` |
| Dependency and vulnerability scanning | NuGetAudit, Dependabot, CodeQL, Trivy, CycloneDX SBOM | `BLUTO-ADR-0011` |
| Container build approach | .NET SDK OCI container publishing with approved .NET runtime images | `BLUTO-ADR-0012` |
| CI platform | GitHub Actions with OIDC and governed runners | `BLUTO-ADR-0013` |
| Infrastructure-as-code tool | Terraform with AzureRM and AzAPI providers | `BLUTO-ADR-0014` |
| Cloud or deployment target | Azure Container Apps, Azure Database for PostgreSQL Flexible Server, Azure Key Vault, Azure Container Registry, Azure Monitor | `BLUTO-ADR-0015` |
| OpenTelemetry implementation | OpenTelemetry .NET SDK with OTLP export to collector or Azure Monitor-compatible ingestion | `BLUTO-ADR-0016` |
| Event transport | PostgreSQL transactional outbox; optional Azure Service Bus topics only when external push delivery is required for v1 | `BLUTO-ADR-0017` |

## Package Consequences

The package is Azure-first and .NET-first while preserving the architecture's broker-neutral, modular-monolith-first constraints. The selections intentionally avoid a full ORM, mandatory external broker, mandatory Kubernetes platform, and production code generation.

## Required Next Governance Steps

Each ADR remains Proposed. Review must confirm owners, evidence expectations, package/version pinning, license posture, cloud account constraints, and approval authority before implementation work packages create production code.
