# Repository Release Standard

| Metadata | Value |
|---|---|
| Document ID | BLUTO-BASE-REL-001 |
| Artifact ID | ART-BLUTO-BASE-REL-001-v2.0.0 |
| Version | 2.0.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline A — Repository Constitution |
| Stream | Repository-wide |
| Authority Level | RAH-1 |
| Depends On | BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-BASE-QG-001, BLUTO-BASE-VAL-001, BLUTO-BASE-VAL-002, BLUTO-BASE-VAL-003, BLUTO-BASE-VAL-004, BLUTO-BASE-VAL-005, BLUTO-BASE-VAL-006 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

A release is an immutable, certified collection of Artifact IDs identified by a Release ID. It includes controlled documents, generated registries, JSON Schemas, RVEC evidence, reviews, certification, audit report, release notes, a self-describing manifest, and checksums. Publication is reproducible from governed sources. Existing releases are never overwritten; corrected releases receive new versions.

GATE-07 validates composition, manifest closure, checksums, source-to-artifact provenance, dependency presence, and reproducible packaging. The release manifest includes a canonical self-entry using a content digest computed over the manifest with that digest field blank, avoiding circular hashing ambiguity.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
