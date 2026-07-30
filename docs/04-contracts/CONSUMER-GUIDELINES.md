# Bluto Contract Consumer Guidelines

| Metadata | Value |
|---|---|
| Document ID | BLUTO-CONTRACT-CONSUMER-001 |
| Artifact ID | ART-BLUTO-CONTRACT-CONSUMER-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 4 — Contracts |
| Authority Level | RAH-5 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-CONTRACT-COMPAT-001, BLUTO-ARCH-INTEGRATION-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing rules

This document derives from upstream baselines and does not redefine domain ownership, architecture topology, security posture, or historical semantics. Identity is deterministic and batch-resolved for v1; tenant scope is explicit; raw strong identifiers are never persisted; Party identifiers are immutable; hometown is a read-only consumer.

## 1. General obligations

Consumers use published interfaces only, authenticate with approved identities, enforce tenant scope, treat Party identifiers as opaque, retain effective dates, and never reconstruct identity from source attributes.

## 2. hometown obligations

hometown resolves cross-domain joins through `party_id`, validates contract versions, supports merged/retired outcomes, preserves mapping provenance in Answer Trace Envelopes, and degrades to single-domain behavior when mapping is unavailable.

## 3. Event consumers

Consumers deduplicate, tolerate delay, avoid global ordering assumptions, persist their own cursor, and reconcile against the authoritative mapping interface.

## 4. Caching

Caches are bounded by documented freshness, tenant scoped, encrypted, and invalidated from mapping changes. Cached identity may not be used beyond its approved freshness for regulated decisions.

## 5. Prohibitions

No direct database access, HMAC-token use, undocumented scraping, ad hoc name matching, cross-tenant inference, or reliance on internal error text.

## Change control

This document is immutable after release except through the approved ADR and change-control process. Every implementation artifact must cite this document and its upstream architecture references.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
