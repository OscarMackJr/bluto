# ADR-0007 - JSON Schema Tooling

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0007 |
| Artifact ID | ART-BLUTO-ADR-0007-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-CONTRACT-SCHEMA-001, BLUTO-TEST-CONTRACT-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto uses JSON Schema for command and integration-event schemas. Schemas must enforce lower_snake_case, RFC 3339 timestamps, bounded confidence values, closed enumerations unless declared extensible, and prohibition on raw strong identifiers and external HMAC token exposure.

## Governing Document IDs

`BLUTO-CONTRACT-SCHEMA-001`, `BLUTO-CONTRACT-EVENT-001`, `BLUTO-CONTRACT-CQ-001`, `BLUTO-CONTRACT-COMPAT-001`, `BLUTO-SEC-DATA-001`, `BLUTO-TEST-CONTRACT-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| JSON Schema Draft 2020-12 with JsonSchema.Net in .NET and Ajv in CI | Runtime fit for .NET plus independent CI validation. |
| NJsonSchema | Strong .NET/OpenAPI ecosystem, but generation focus is not the primary need. |
| Ajv only | Excellent validator, but would require Node-based validation inside .NET runtime paths. |
| Manual validation code | High drift risk and weak contract evidence. |

## Criteria

Draft support, runtime validation, CI validation, schema linting, deterministic errors, compatibility gates, contract-test integration, and independence from domain object serialization.

## Security And Operational Impact

Schema validation is a trust-boundary control. It must reject unexpected fields where contracts require closure and must support raw-identifier leakage tests.

## Compatibility Impact

The tooling preserves broker-neutral integration events and HTTP/JSON contracts. It does not authorize generated schemas to redefine controlled contract files.

## Recommendation

Use JSON Schema Draft 2020-12. Use JsonSchema.Net for .NET runtime and test validation, and Ajv in CI as an independent schema validator for committed schemas and examples.

## Consequences

Contract tests must validate both schema syntax and representative examples. Generated schemas, if used, are review inputs only until checked against controlled contract standards.

## Migration Or Replacement Considerations

Validator replacement is acceptable if schemas remain Draft 2020-12-compatible or an approved contract migration defines a new dialect.
