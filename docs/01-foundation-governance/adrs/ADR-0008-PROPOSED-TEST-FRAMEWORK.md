# ADR-0008 - Test Framework

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0008 |
| Artifact ID | ART-BLUTO-ADR-0008-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-TEST-STRATEGY-001, BLUTO-TEST-DOMAIN-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto requires domain, application, contract, integration, security, acceptance, performance, and migration tests. Domain tests must use fake clocks, deterministic identifiers, in-memory ports, and precise invariant coverage.

## Governing Document IDs

`BLUTO-TEST-STRATEGY-001`, `BLUTO-TEST-DOMAIN-001`, `BLUTO-TEST-CONTRACT-001`, `BLUTO-TEST-SEC-001`, `BLUTO-TEST-INTEGRATION-001`, `BLUTO-TEST-ACCEPT-001`, `BLUTO-TEST-GATE-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| xUnit.net v3 with Testcontainers for integration tests | Strong .NET fit, parallel execution controls, analyzer ecosystem, and containerized PostgreSQL tests. |
| NUnit | Mature, but less aligned with common .NET SDK templates in this package. |
| MSTest | First-party, but less expressive for domain-heavy test style. |
| Expecto/F# tests | Good for property tests, but introduces another primary test language. |

## Criteria

Developer familiarity, deterministic execution, async support, fixture lifecycle, CI compatibility, property-test integration, PostgreSQL integration testing, and analyzer support.

## Security And Operational Impact

The framework must support negative authorization, cross-tenant, raw-identifier, migration, outbox, and restore compatibility tests. Integration tests must avoid external cloud dependencies by default.

## Compatibility Impact

xUnit does not change runtime architecture. Testcontainers supports managed-PostgreSQL-compatible integration tests without making local containers authoritative.

## Recommendation

Use xUnit.net v3 as the primary test framework. Use Testcontainers for PostgreSQL-backed integration and contract tests where database behavior is material.

## Consequences

Test projects must classify unit, property, contract, integration, security, acceptance, and performance tests clearly so CI can run appropriate gates.

## Migration Or Replacement Considerations

NUnit or MSTest may replace xUnit only with a successor ADR preserving test coverage, fixtures, property-test integration, and CI reporting.
