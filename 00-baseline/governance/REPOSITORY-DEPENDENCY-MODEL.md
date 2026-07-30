# Repository Dependency Model

| Metadata | Value |
|---|---|
| Document ID | BLUTO-BASE-GOV-004 |
| Artifact ID | ART-BLUTO-BASE-GOV-004-v2.0.0 |
| Version | 2.0.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline A — Repository Constitution |
| Stream | Repository-wide |
| Authority Level | RAH-1 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-003 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Rules

Authority and refinement flow downward. Every controlled document explicitly declares direct dependencies by immutable Document ID. Same-layer dependencies must be acyclic. Implementation may depend on architecture; architecture may not depend on implementation.

A release is certifiable only when all upstream authorities are present and baselined, all references resolve, and the dependency graph is complete and acyclic. AI context assembly follows the same graph and may not infer authority from file order or conversational history.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
