# ADR-0010 - Static Analysis And Formatting

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0010 |
| Artifact ID | ART-BLUTO-ADR-0010-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-ENG-CODE-001, BLUTO-SEC-SDLC-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto needs enforceable code quality, formatting, naming, null-safety, security hygiene, and repeatable CI checks without relying on individual IDE settings.

## Governing Document IDs

`BLUTO-ENG-CODE-001`, `BLUTO-ENG-REVIEW-001`, `BLUTO-SEC-SDLC-001`, `BLUTO-AI-CODE-001`, `BLUTO-AI-GUARD-001`, `BLUTO-TEST-GATE-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| .NET SDK Roslyn analyzers, `.editorconfig`, nullable, warnings-as-errors, and `dotnet format` | First-party, build-integrated, low dependency, consistent with .NET. |
| ReSharper/Rider inspections only | Useful locally but not ideal as the sole CI authority. |
| SonarAnalyzer only | Valuable supplement, but adds external analyzer dependency and policy surface. |
| Manual review only | Insufficient for repeatable engineering gates. |

## Criteria

CI enforcement, IDE neutrality, low dependency burden, security/code-quality coverage, formatting consistency, false-positive manageability, and transparent suppression policy.

## Security And Operational Impact

Build-time analyzers catch unsafe patterns early. Formatting and warnings-as-errors improve review quality and reduce drift in AI-generated or human-written implementation artifacts.

## Compatibility Impact

Static analysis does not change runtime behavior. Suppressions must be justified so no analyzer exception bypasses tenant isolation, raw-identifier controls, authorization, or audit requirements.

## Recommendation

Use .NET SDK Roslyn analyzers, repository `.editorconfig`, nullable reference types, warnings-as-errors for selected rule sets, and `dotnet format --verify-no-changes` in CI. Add specialized analyzers later only by approved package governance.

## Consequences

Initial implementation may require stricter coding discipline. Rule severity must be tuned once code exists, with suppressions reviewed as engineering artifacts.

## Migration Or Replacement Considerations

Additional tools such as SonarAnalyzer may be introduced through dependency review. First-party analyzer and formatting gates should remain unless a successor ADR replaces them with equal CI enforcement.
