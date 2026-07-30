# ADR-0012 - Container Build Approach

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0012 |
| Artifact ID | ART-BLUTO-ADR-0012-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-OPS-BUILD-001, BLUTO-ENG-SUPPLY-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Bluto deployables are separate API, worker, stewardship, migration, and optional outbox-publisher containers. Artifacts must be immutable, reproducible, scanned, and suitable for cloud deployment.

## Governing Document IDs

`BLUTO-ARCH-PHYSICAL-001`, `BLUTO-ARCH-DEPLOY-001`, `BLUTO-OPS-BUILD-001`, `BLUTO-OPS-RELEASE-001`, `BLUTO-ENG-SUPPLY-001`, `BLUTO-SEC-SDLC-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| .NET SDK OCI container publishing with Microsoft .NET chiseled/runtime-deps images | Low Dockerfile drift, native .NET support, small runtime images, easy per-entry-point artifacts. |
| Hand-authored multi-stage Dockerfiles | Flexible, but more maintenance and drift risk for standard .NET apps. |
| Buildpacks | Good abstraction, but less explicit for security review and image composition. |
| VM/package deployment | Does not match immutable container artifact direction. |

## Criteria

Reproducibility, small attack surface, per-entry-point images, SBOM/provenance compatibility, vulnerability scanning, registry publishing, and minimal custom build logic.

## Security And Operational Impact

Chiseled/minimal base images reduce OS package surface. Images must run as non-root where supported, include no secrets, and be scanned before promotion.

## Compatibility Impact

Container publishing preserves separate entry points and does not change modular-monolith code ownership. Migration and worker images remain distinct deployable artifacts.

## Recommendation

Use .NET SDK OCI container publishing for v1 images, targeting Linux x64 and approved .NET 10 runtime images. Introduce hand-authored Dockerfiles only when native dependencies or hardening requirements cannot be expressed through SDK container properties.

## Consequences

Build metadata and image settings must be managed in project files or central build props. Container runtime assumptions must be validated in integration environments.

## Migration Or Replacement Considerations

Dockerfiles, BuildKit, or buildpacks may replace SDK publishing through a successor ADR if they improve reproducibility, hardening, or platform compatibility without increasing release risk.
