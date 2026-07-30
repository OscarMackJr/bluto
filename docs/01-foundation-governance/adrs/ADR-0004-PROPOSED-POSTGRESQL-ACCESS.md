# ADR-0004 - PostgreSQL Access Strategy

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0004 |
| Artifact ID | ART-BLUTO-ADR-0004-v0.1.0 |
| Version | 0.1.0 |
| Status |  |
| Owner | Enterprise Architecture |
| Baseline | Implementation ApprovedTechnology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-ARCH-TECH-001, BLUTO-ARCH-DATA-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto's authoritative store is managed PostgreSQL with context-owned schemas, effective dating, lineage, tenant isolation, explicit transactional boundaries, and transactional outbox writes.

## Governing Document IDs

`BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-DATA-001`, `BLUTO-ARCH-RUNTIME-001`, `BLUTO-DOM-REPOSITORY-001`, `BLUTO-SEC-TENANT-001`, `BLUTO-SEC-DATA-001`, `BLUTO-TEST-INTEGRATION-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| Npgsql plus Dapper and explicit SQL repositories | Direct PostgreSQL behavior, clear transaction control, low abstraction leakage, easy query-plan review. |
| EF Core | Productive, but ORM change tracking may obscure aggregate transactions and generated SQL for temporal and cross-schema rules. |
| Npgsql raw ADO.NET only | Maximum control but excessive boilerplate. |
| Stored procedures first | Strong database encapsulation, but risks moving domain logic into PostgreSQL and complicating tests. |

## Criteria

Explicit SQL, reliable transactions, performance transparency, tenant predicates, effective-dated query clarity, outbox atomicity, testability, low magic, and managed PostgreSQL compatibility.

## Security And Operational Impact

Explicit SQL makes tenant filters, row ownership, schema boundaries, lock behavior, and query plans reviewable. Parameterized Npgsql/Dapper access reduces injection risk when used consistently.

## Compatibility Impact

The strategy preserves managed PostgreSQL as authority, context-owned schemas, no raw strong identifiers at rest, transactional outbox, deterministic batch resolution, and no direct cross-context persistence writes.

## Recommendation

Use Npgsql `DataSource`/connections with Dapper for mapping result sets, implemented behind repository ports. Use explicit SQL and explicit transactions for aggregate changes and outbox writes. Do not use EF Core as the v1 domain persistence ORM.

## Consequences

Developers must write and review SQL deliberately. Repository tests and query-plan checks become more important because the ORM will not hide database details.

## Migration Or Replacement Considerations

EF Core or another data mapper may be introduced for narrow read projections through a successor ADR. Domain writes must not migrate until aggregate, outbox, and tenant-isolation behavior are proven equivalent.
