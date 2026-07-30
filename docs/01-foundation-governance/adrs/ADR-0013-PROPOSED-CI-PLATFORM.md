# ADR-0013 - CI Platform

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0013 |
| Artifact ID | ART-BLUTO-ADR-0013-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-OPS-BUILD-001, BLUTO-OPS-RELEASE-001, BLUTO-ENG-SUPPLY-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto needs repeatable validation gates for build, test, contract, schema, migration, static analysis, vulnerability scanning, SBOM, provenance, container publishing, and deployment promotion.

## Governing Document IDs

`BLUTO-OPS-BUILD-001`, `BLUTO-OPS-RELEASE-001`, `BLUTO-TEST-GATE-001`, `BLUTO-ENG-SUPPLY-001`, `BLUTO-SEC-SDLC-001`, `BLUTO-AI-VALIDATE-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| GitHub Actions | Natural fit for repository-hosted workflows, Dependabot, CodeQL, OIDC cloud auth, and PR gates. |
| Azure DevOps Pipelines | Strong Azure integration, but adds a separate platform from repository security features. |
| GitLab CI | Capable, but not aligned with likely GitHub-native scanning and PR workflow. |
| Jenkins | Flexible, but high operational burden. |

## Criteria

Repository integration, PR checks, security scanning, OIDC federation, artifact provenance, runner options, approval gates, auditability, and operational overhead.

## Security And Operational Impact

GitHub Actions supports repository-native security checks and short-lived cloud credentials through OIDC. Production deploys requiring private networking may need approved larger or self-hosted runners.

## Compatibility Impact

CI choice does not alter architecture. It must enforce, not replace, repository quality gates and release evidence.

## Recommendation

Use GitHub Actions as the CI platform. Use GitHub-hosted runners for build/test/scan by default, OIDC for cloud authentication, and controlled larger or self-hosted runners only where private deployment networking requires them.

## Consequences

Workflow definitions become governed delivery artifacts. Action dependencies must be pinned and scanned as part of supply-chain governance.

## Migration Or Replacement Considerations

Azure DevOps or another CI platform may replace GitHub Actions if enterprise governance requires it, provided all gates, evidence, OIDC or equivalent credential controls, and audit trails are preserved.
