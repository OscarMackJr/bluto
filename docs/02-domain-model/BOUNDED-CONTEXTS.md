# Bluto Bounded Contexts

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-BC-001 |
| Artifact ID | ART-BLUTO-DOM-BC-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-CORE-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

**Rule Management** owns rule definitions, immutable versions, validation, activation, and retirement. **Identity Resolution** owns Party creation, deterministic matching, source links, and deferral. **Identity Lifecycle** owns merge, split, retirement, lineage, and temporal reconstruction. **Stewardship** owns review cases and immutable decisions. **Identity Governance** owns audit evidence, policy findings, compliance projections, and operational metrics.

Contexts are logical semantic boundaries, not mandatory services. They interact through public ports and domain/integration events; direct cross-context persistence writes and cyclic dependencies are prohibited. The shared kernel is limited to Party/Tenant/Source/Rule identifiers, Effective Date Range, and correlation/causation identifiers.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
