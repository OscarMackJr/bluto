# Bluto Entity Catalogue

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-ENTITY-001 |
| Artifact ID | ART-BLUTO-DOM-ENTITY-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-UL-001, BLUTO-DOM-BC-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

**Party** (Identity Resolution): immutable enterprise identity keyed by `party_id`. **Party Source Link**: effective-dated identity assertion for source system/key and tenant scope. **Match Execution**: attributable rule-evaluation record. **Rule Definition/Rule Version** (Rule Management): governed rule identity and immutable published version. **Review Case/Review Decision** (Stewardship): ambiguous/conflicting candidate and immutable human outcome. **Merge Record/Split Record/Identity Version** (Lifecycle): append-only lineage. **Audit Record/Policy Exception** (Governance): evidence and controlled deviation.

Entities have persistent identity; value objects do not. Each entity has exactly one owning context and may be accessed outside it only through published contracts.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
