# ADR-0005 - Schema Migration Tool

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0005 |
| Artifact ID | ART-BLUTO-ADR-0005-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0004, BLUTO-ARCH-DATA-001, BLUTO-ENG-DATA-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto needs versioned, auditable PostgreSQL schema migrations for context-owned schemas, outbox tables, roles, indexes, and expand-migrate-contract release sequencing.

## Governing Document IDs

`BLUTO-ARCH-DATA-001`, `BLUTO-ARCH-DEPLOY-001`, `BLUTO-ENG-DATA-001`, `BLUTO-OPS-BUILD-001`, `BLUTO-OPS-RELEASE-001`, `BLUTO-TEST-GATE-001`, `BLUTO-SEC-AUDIT-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| Flyway SQL migrations | Mature SQL-first migration history, PostgreSQL support, strong fit for explicit database governance. |
| DbUp | Simple .NET-native migration runner, but less standard for enterprise database governance. |
| Liquibase | Powerful diff/changelog model, but heavier than required for SQL-first controlled migrations. |
| EF Core migrations | Tied to ORM model and not aligned with explicit SQL repository strategy. |

## Criteria

SQL-first reviewability, immutable migration history, CI and deployment support, rollback/forward-recovery planning, PostgreSQL compatibility, least coupling to application code, and audit evidence.

## Security And Operational Impact

SQL-first migrations make privileges, schemas, constraints, tenant guards, and outbox structures reviewable. Migration execution must use a constrained migration identity and must not grant broad runtime privileges.

## Compatibility Impact

Flyway preserves managed PostgreSQL, context-owned schemas, migration gates, and immutable artifact release requirements. It does not alter public API or event contracts by itself.

## Recommendation

Use Flyway with checked-in SQL migration files and a dedicated migration job. Require expand-migrate-contract sequencing, migration dry runs in preproduction, and restore-tested rollback or forward-recovery evidence.

## Consequences

Database changes become explicit artifacts. Developers must coordinate schema, repository SQL, contract compatibility, and operational runbooks in the same release plan.

## Migration Or Replacement Considerations

Replacement with Liquibase, DbUp, or a platform-native migration service requires preserving migration history, checksums, execution audit, and compatible recovery procedures.
