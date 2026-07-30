# ADR-0009 - Property-Based Testing Framework

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0009 |
| Artifact ID | ART-BLUTO-ADR-0009-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0008, BLUTO-TEST-DOMAIN-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto's identity invariants involve temporal intervals, merge/split lineage, tenant scopes, deterministic matching eligibility, idempotency, and non-reuse rules that benefit from generated input exploration.

## Governing Document IDs

`BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-DOM-OVERVIEW-001`, `BLUTO-DOM-STATE-001`, `BLUTO-TEST-DOMAIN-001`, `BLUTO-TEST-ACCEPT-001`, `BLUTO-SEC-TENANT-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| FsCheck integrated with xUnit | Mature .NET property-testing library with shrinking and C#/F# support. |
| Hedgehog | Strong property-testing model, but less common in C# enterprise test suites. |
| Hypothesis via Python side tests | Powerful, but adds another runtime and cross-language model translation. |
| Example-only tests | Insufficient for temporal and invariant-heavy behavior. |

## Criteria

Shrinking quality, xUnit integration, custom generators, deterministic seeds, CI repeatability, C# usability, and suitability for domain invariant testing.

## Security And Operational Impact

Generated cases can exercise cross-tenant collision, ambiguous candidate, interval boundary, replay, duplicate command, and no-raw-identifier scenarios beyond hand-picked examples.

## Compatibility Impact

Property-based tests are additive and do not alter contracts or architecture. They strengthen evidence for no probabilistic matching in v1 by testing deterministic rule behavior.

## Recommendation

Use FsCheck for property-based testing, integrated with xUnit test projects. Require explicit seeds in failure output and checked-in generators for domain value objects.

## Consequences

The team must maintain generators carefully so invalid generated data is intentional and meaningful. Property tests should complement, not replace, example-based UAT and contract tests.

## Migration Or Replacement Considerations

Replacement requires preserving generated coverage for identity, temporal, tenant, idempotency, and lineage invariants with reproducible failing cases.
