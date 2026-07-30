# ADR-0003 - Dependency Injection Approach

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0003 |
| Artifact ID | ART-BLUTO-ADR-0003-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-ADR-0002, BLUTO-ENG-ARCH-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto needs dependency composition across API, worker, stewardship, repositories, clock/identifier abstractions, HMAC/key-vault services, outbox publishing, and test doubles while keeping module boundaries explicit.

## Governing Document IDs

`BLUTO-ARCH-REF-001`, `BLUTO-ARCH-CROSSCUT-001`, `BLUTO-ENG-ARCH-001`, `BLUTO-ENG-CODE-001`, `BLUTO-TEST-DOMAIN-001`, `BLUTO-SEC-CRYPTO-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| Microsoft.Extensions.DependencyInjection | Built into .NET host, simple, adequate for constructor injection, scopes, options, hosted services, and tests. |
| Autofac | More features, but adds container complexity without known need. |
| Lamar | Fast and capable, but less standard in current .NET hosting documentation. |
| Manual composition only | Very explicit, but becomes repetitive across three entry points and hosted services. |

## Criteria

Framework compatibility, explicit construction, low magic, testability, scoped lifetimes for database units of work, module registration clarity, and avoidance of service locator patterns.

## Security And Operational Impact

Built-in DI reduces dependency surface. Scoped services can ensure tenant context, authorization context, database connection/transaction, and audit context are consistently composed per request or unit of work.

## Compatibility Impact

This approach supports separate API, worker, and stewardship entry points without changing deployment topology. It preserves tenant isolation by requiring explicit tenant and authorization context services at boundaries.

## Recommendation

Use Microsoft.Extensions.DependencyInjection with constructor injection, module-level registration extension methods, validated options, and explicit scoped lifetimes. Do not introduce a third-party container for v1.

## Consequences

The codebase must avoid hidden service resolution outside composition roots. More advanced container features such as decorators or keyed services require deliberate local patterns or a successor ADR.

## Migration Or Replacement Considerations

A third-party container may be introduced later only if concrete complexity emerges, such as cross-cutting decorators that cannot be expressed cleanly with built-in DI.
