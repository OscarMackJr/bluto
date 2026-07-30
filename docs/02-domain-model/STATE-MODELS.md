# Bluto State Models

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-STATE-001 |
| Artifact ID | ART-BLUTO-DOM-STATE-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-AGGREGATE-001, BLUTO-DOM-EVENT-001, BLUTO-DOM-REPOSITORY-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

Party lifecycle is Created → Active → Merged or Retired; Split is an explicit operation producing new Parties and revised links while preserving the original historical identity. Rule Version lifecycle is Draft → Validated → Published → Active → Retired; published content is immutable. Review Case lifecycle is Opened → Accepted, Rejected, or Deferred; terminal decisions are immutable. Merge lifecycle is Prepared → Validated → Executed → Recorded.

Every transition has command, authorization, preconditions, atomic postconditions, events, audit evidence, and prohibited transitions. Failed transitions leave authoritative state unchanged.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
