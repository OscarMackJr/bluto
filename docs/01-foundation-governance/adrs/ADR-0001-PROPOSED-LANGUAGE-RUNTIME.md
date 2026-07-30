# ADR-0001 - Primary Programming Language And Runtime

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0001 |
| Artifact ID | ART-BLUTO-ADR-0001-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0000, BLUTO-ARCH-TECH-001, BLUTO-ENG-CODE-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto needs a primary implementation language for a modular monolith with separate API, worker, and stewardship entry points. The language must support deterministic domain logic, PostgreSQL access, OpenTelemetry, container delivery, strong typing, long-term support, and security tooling.

## Governing Document IDs

`BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-LOGICAL-001`, `BLUTO-ARCH-PHYSICAL-001`, `BLUTO-SEC-ARCH-001`, `BLUTO-ENG-ARCH-001`, `BLUTO-ENG-CODE-001`, `BLUTO-ENG-SUPPLY-001`, `BLUTO-TEST-STRATEGY-001`, `BLUTO-OPS-BUILD-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| C# on .NET 10 LTS | Strong type system, mature web/worker/runtime host, current LTS support, good PostgreSQL and telemetry ecosystem. |
| Java 21 LTS | Strong ecosystem and support, but heavier runtime and framework surface for this small modular-monolith-first implementation. |
| TypeScript on Node.js LTS | Strong API tooling, but weaker fit for transaction-heavy domain modeling and compile-time invariants. |
| Go 1.x | Operationally simple, but weaker DI, schema, and rich domain modeling conventions for this repository's DDD-style architecture. |

## Criteria

Long-term vendor/community support, type safety, maintainability, domain modeling clarity, security tooling, first-class OpenTelemetry, PostgreSQL support, API/worker parity, container readiness, hiring availability, and low operational complexity.

## Security And Operational Impact

.NET 10 LTS supports monthly servicing, nullable reference types, analyzers, package vulnerability audit, managed identity SDKs, and mature OpenTelemetry instrumentation. Operational impact is favorable because API, worker, stewardship, migration helpers, and test projects can share one runtime and build toolchain.

## Compatibility Impact

This decision preserves modular-monolith-first, separate entry points, HTTP/JSON APIs, managed PostgreSQL, transactional outbox, deterministic scheduled resolution, tenant isolation, Key Vault-backed HMAC-SHA-256, no raw strong identifiers at rest, and no probabilistic matching in v1.

## Recommendation

Use C# with .NET 10 LTS as the primary programming language and supported runtime. Target `net10.0`; require current supported .NET 10 SDK/runtime patches in CI and deployed containers.

## Consequences

The implementation uses one strongly typed runtime for API, worker, stewardship, tests, and shared domain modules. The team accepts the .NET ecosystem for package governance and must keep SDK/runtime patches current to remain supported.

## Migration Or Replacement Considerations

Replacement requires a successor ADR proving equal or better support for all upstream invariants, API compatibility, data compatibility, observability, supply-chain controls, and migration of domain/application modules without semantic changes.
