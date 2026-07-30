# Repository Governance Charter

| Metadata | Value |
|---|---|
| Document ID | BLUTO-BASE-GOV-002 |
| Artifact ID | ART-BLUTO-BASE-GOV-002-v2.0.0 |
| Version | 2.0.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline A — Repository Constitution |
| Stream | Repository-wide |
| Authority Level | RAH-1 |
| Depends On | BLUTO-BASE-GOV-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Purpose and scope

This charter establishes constitutional governance for the repository itself. It governs structure, controlled documents, lifecycle, baselines, quality gates, evidence, exceptions, certification, release, and audit. It does not define business semantics or implementation details.

## Governance roles

The Executive Sponsor authorizes major releases. The Chief Enterprise Architect owns the Constitution and resolves authority conflicts. Stream Architecture Owners own their assigned layer. The Architecture Review Board reviews ADRs, exceptions, and certification. Repository Maintainers operate tooling without acquiring architecture authority.

## Lifecycle

Controlled documents move Draft → Under Review → Approved → Baselined → Superseded → Retired. Baselined content is immutable. Baselines move Planning → Authoring → Review → Freeze → Validation → Certification → Release → Maintenance → Supersession.

## Exceptions

Every exception records rationale, scope, controls affected, approver, expiry, compensating controls, and retirement plan. Practice never creates authority.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
