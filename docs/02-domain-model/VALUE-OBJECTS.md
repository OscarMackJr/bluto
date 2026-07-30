# Bluto Value Objects

| Metadata | Value |
|---|---|
| Document ID | BLUTO-DOM-VALUEOBJECT-001 |
| Artifact ID | ART-BLUTO-DOM-VALUEOBJECT-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 2 — Domain Model |
| Authority Level | RAH-3 |
| Depends On | BLUTO-DOM-ENTITY-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

Shared immutable value objects are PartyIdentifier, TenantIdentifier, SourceSystem, SourceKey, MatchRuleIdentifier, RuleVersion, EffectiveDateRange, CorrelationIdentifier, and CausationIdentifier. Context-specific values include MatchConfidence, MatchMethod, ReviewOutcome, DecisionReason, IdentityStatus, and ComplianceStatus.

Value objects compare by value, validate on creation, and are replaced rather than mutated. Effective ranges are half-open `[from,to)`; confidence is bounded decimal; identifiers are opaque and normalized; strong-identifier tokens are restricted exact-match values and never externally exposed.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
