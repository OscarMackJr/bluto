# Repository and Architecture Glossary

| Metadata | Value |
|---|---|
| Document ID | BLUTO-GLOSSARY-001 |
| Artifact ID | ART-BLUTO-GLOSSARY-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline 2.0 |
| Stream | 1 — Foundation & Governance |
| Authority Level | RAH-2 |
| Depends On | BLUTO-SCOPE-001, BLUTO-NONGOALS-001, BLUTO-BASE-STD-001, BLUTO-BASE-STD-003 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

**Party:** enterprise identity construct represented by immutable `party_id`. **Source Record:** record owned by an external system. **Party Source Link:** effective-dated assertion connecting one source record to one Party. **Identity Resolution:** deterministic process selecting or creating a Party. **Identity Lineage:** immutable history of links, merges, splits, and retirement. **Strong Identifier Token:** HMAC-SHA-256 exact-match token created with Tier-0 key material. **Tenant:** authorization and resolution boundary. **Hometown:** read-only governed consumer. **Controlled Document, Artifact ID, Release ID, Quality Gate, RVEC, LIR, RGTM:** terms defined by Baseline A.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
