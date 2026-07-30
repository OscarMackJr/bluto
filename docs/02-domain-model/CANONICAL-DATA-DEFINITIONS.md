# Bluto Canonical Data Definitions

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-CANONICAL-001 |
| Artifact ID | ART-BLUTO-DOM-CANONICAL-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-UL-001, BLUTO-DOM-ENTITY-001, BLUTO-DOM-VALUEOBJECT-001, BLUTO-DOM-STATE-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

Canonical identifiers are `party_id`, `link_id`, `tenant_id`, `source_system`, `source_key`, `match_rule_id`, `rule_version_id`, `review_case_id`, `merge_id`, `audit_record_id`, `correlation_id`, and `causation_id`. Canonical temporal fields are `effective_from`, `effective_to`, `asserted_at`, and processing/occurrence timestamps. Canonical provenance fields include rule version, method, confidence, asserted_by, source version, and evidence reference.

Party IDs are immutable UUIDs. Source keys are opaque source-owned identifiers. Effective intervals are half-open and non-overlapping for active links in the canonical scope. Enumerations include PartyStatus, LinkStatus, RuleStatus, ReviewOutcome, MatchMethod, and ComplianceStatus. Raw government/tax identifiers are never canonical data.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
