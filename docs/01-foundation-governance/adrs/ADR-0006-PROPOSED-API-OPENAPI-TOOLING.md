# ADR-0006 - API And OpenAPI Tooling

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0006 |
| Artifact ID | ART-BLUTO-ADR-0006-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0002, BLUTO-CONTRACT-API-001, BLUTO-CONTRACT-COMPAT-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Interactive APIs are versioned HTTP/JSON contracts with OpenAPI projections, correlation headers, idempotency keys, cursor pagination, and stable error semantics.

## Governing Document IDs

`BLUTO-ARCH-TECH-001`, `BLUTO-CONTRACT-ARCH-001`, `BLUTO-CONTRACT-API-001`, `BLUTO-CONTRACT-CQ-001`, `BLUTO-CONTRACT-ERROR-001`, `BLUTO-CONTRACT-COMPAT-001`, `BLUTO-TEST-CONTRACT-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| ASP.NET Core OpenAPI generation plus build-time document generation, Spectral lint, and openapi-diff | Native generation with independent lint and compatibility gates. |
| Swashbuckle only | Common, but would rely on one package for generation and UI concerns. |
| NSwag generation and client codegen | Strong codegen, but more tooling surface than needed for v1. |
| Manual OpenAPI only | Precise but risks drift from implementation. |

## Criteria

Spec accuracy, build-time artifact generation, compatibility checks, contract linting, source-control reviewability, HTTP/JSON alignment, and low runtime dependency burden.

## Security And Operational Impact

OpenAPI linting must verify no raw strong identifiers or HMAC tokens appear in external schemas. Generated docs must include required security, correlation, idempotency, pagination, and error-contract metadata.

## Compatibility Impact

This approach preserves existing `bluto-v1.openapi.yaml` as a governing projection and adds implementation-time generation and diff checks without changing approved contract semantics.

## Recommendation

Use ASP.NET Core built-in OpenAPI generation for implemented endpoints, `Microsoft.Extensions.ApiDescription.Server` for build-time documents, Spectral for linting, and openapi-diff or an equivalent compatibility checker for version gates.

## Consequences

Contract generation and committed contract artifacts must be reconciled in CI. Any generated drift requires either implementation correction or an approved contract change.

## Migration Or Replacement Considerations

NSwag or another OpenAPI tool can be introduced for client generation later. Any replacement must preserve lint, diff, and source-controlled contract artifacts.
