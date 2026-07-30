# ADR-0002 - Application Framework

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0002 |
| Artifact ID | ART-BLUTO-ADR-0002-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-ARCH-TECH-001, BLUTO-CONTRACT-API-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto requires HTTP/JSON interactive APIs, independently executable worker and stewardship entry points, health/operational endpoints, authentication/authorization integration, and deterministic scheduled batch orchestration.

## Governing Document IDs

`BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-LOGICAL-001`, `BLUTO-ARCH-RUNTIME-001`, `BLUTO-ARCH-PHYSICAL-001`, `BLUTO-CONTRACT-API-001`, `BLUTO-SEC-IAM-001`, `BLUTO-OPS-OBS-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| ASP.NET Core 10 Minimal APIs plus Generic Host | Native fit for .NET, API and worker parity, built-in OpenAPI generation, health checks, auth, DI, logging, and hosted services. |
| ASP.NET Core MVC controllers | Mature but more ceremony than required for focused HTTP/JSON endpoints. |
| FastEndpoints | Productive but adds a third-party framework dependency before native ASP.NET Core limits are known. |
| gRPC-first framework | Does not match the upstream HTTP/JSON interactive API decision. |

## Criteria

Compatibility with versioned HTTP/JSON, explicit endpoint grouping, auth middleware, OpenAPI support, worker-host symmetry, testability, low dependency count, operational endpoints, and long-term support.

## Security And Operational Impact

ASP.NET Core provides established authentication, authorization, middleware, request limits, structured logging, health checks, and OpenTelemetry integration points. Minimal APIs keep endpoint behavior explicit and reduce framework layering.

## Compatibility Impact

The framework preserves HTTP/JSON interactive contracts and does not imply microservices. Worker and stewardship entry points use the same Generic Host conventions without changing bounded-context ownership.

## Recommendation

Use ASP.NET Core 10 Minimal APIs for interactive APIs and stewardship HTTP surfaces, and .NET Generic Host worker services for batch, outbox, and scheduled jobs.

## Consequences

Endpoint definitions must remain thin and delegate to application services. Module boundaries must be enforced through project structure and internal contracts, not by the web framework alone.

## Migration Or Replacement Considerations

MVC controllers or another .NET web framework can replace Minimal APIs through a successor ADR if endpoint complexity or governance evidence justifies it while preserving OpenAPI and HTTP behavior.
