# Bluto Architecture Views

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ARCH-VIEWS-001 |
| Artifact ID | ART-BLUTO-ARCH-VIEWS-001-v1.1.0 |
| Version | 1.1.0 |
| Status | Baselined |
| Owner | Enterprise Architecture |
| Baseline | Repository Baseline B |
| Stream | 3 — Architecture |
| Authority Level | RAH-4 |
| Depends On | BLUTO-BASE-GOV-001, BLUTO-BASE-GOV-002, BLUTO-BASE-GOV-004, BLUTO-BASE-STD-001, BLUTO-BASE-STD-002, BLUTO-BASE-STD-003, BLUTO-BASE-STD-004, BLUTO-ARCH-REF-001, BLUTO-ARCH-LOGICAL-001, BLUTO-ARCH-PHYSICAL-001, BLUTO-ARCH-DEPLOY-001, BLUTO-DOM-REVIEW-001 |
| Supersedes | None |
| Approval Date | 2026-07-30 |
| Effective Date | 2026-07-30 |
| Review Date | 2027-07-30 |
| Classification | Internal |

## Governing references

This document is subordinate to the authoritative Bluto Enterprise Identity Spine v0.1 and the immutable Stream 1 and Stream 2 baselines. It preserves the following upstream invariants:

- Bluto owns identity mapping, stable Party identifiers, link lineage, rules, confidence, review decisions, and merge/split history.
- Bluto does not own source-system business attributes and shall not become a golden-record or master-data store.
- v1 resolution is deterministic, incremental batch, and tenant-scoped by default.
- raw strong identifiers are never persisted in Bluto; matching uses HMAC-SHA-256 tokens created with a Tier-0 key held outside the database.
- Party identifiers are never reused, and historical resolution is effective-dated and reproducible.
- hometown consumes Bluto mappings read-only and never performs ad hoc identity matching.


## 1. Purpose

This document provides controlled viewpoints for stakeholders. The Repository Reference Architecture remains the canonical orientation view.

## 2. System context view

```mermaid
flowchart LR
  CRM[Nexus CRM] -->|read-only candidates| BLUTO[Bluto Identity Spine]
  INT[Intrepid] -->|tenant-scoped candidates| BLUTO
  LED[Ledger] -->|read-only candidates| BLUTO
  STEW[Data Stewards] -->|review decisions| BLUTO
  VAULT[Platform Key Vault] -->|HMAC capability| BLUTO
  BLUTO -->|governed mappings| HOME[hometown]
  BLUTO -->|sanitized telemetry| OBS[Observability Platform]
```

## 3. Container view

```mermaid
flowchart TB
  API[Bluto API]
  WORK[Resolution Worker]
  UI[Stewardship Web/API]
  DB[(Dedicated PostgreSQL)]
  OUT[Outbox Publisher]
  KV[Key Vault]
  SRC[Source Adapters]
  HOME[hometown Adapter]
  OBS[Telemetry]

  SRC --> WORK
  WORK --> KV
  WORK --> DB
  API --> DB
  UI --> API
  DB --> OUT
  OUT --> HOME
  API --> HOME
  API --> OBS
  WORK --> OBS
  UI --> OBS
```

## 4. Component view

```mermaid
flowchart LR
  RM[Rule Management]
  IR[Identity Resolution]
  IL[Identity Lifecycle]
  ST[Stewardship]
  IG[Identity Governance]
  PORTS[Repository and Platform Ports]

  RM --> IR
  IR --> IL
  IR --> ST
  ST --> IR
  IR --> IG
  IL --> IG
  ST --> IG
  RM --> PORTS
  IR --> PORTS
  IL --> PORTS
  ST --> PORTS
  IG --> PORTS
```

## 5. Deployment view

```mermaid
flowchart TB
  subgraph PROD[Production private environment]
    LB[Private ingress]
    API1[API instance A]
    API2[API instance B]
    W1[Worker partition A]
    W2[Worker partition B]
    UI[Stewardship process]
    PG[(Managed PostgreSQL HA)]
    LB --> API1
    LB --> API2
    API1 --> PG
    API2 --> PG
    W1 --> PG
    W2 --> PG
    UI --> PG
  end
  KV[Managed Key Vault] --> W1
  KV --> W2
  SOURCES[Source private endpoints] --> W1
  SOURCES --> W2
  PROD --> OBS[Central telemetry]
```

## 6. Information-flow view

```mermaid
sequenceDiagram
  participant S as Source
  participant W as Worker
  participant K as Key Vault
  participant D as Domain/Persistence
  participant O as Outbox
  participant H as hometown

  W->>S: Read bounded candidate page
  S-->>W: Source keys and match inputs
  W->>K: Request HMAC operation/capability
  K-->>W: Tokenization result
  W->>D: Execute deterministic resolution
  D-->>W: Committed Party/link or review case
  D->>O: Commit integration fact
  O-->>H: Publish governed mapping change
```

## 7. View consistency rules

All diagrams are explanatory projections of the same architecture. A diagram may omit detail but may not change ownership, dependency direction, trust boundary, or v1 operating model.

## Document control

This controlled document is immutable in this release. Changes require the approved ADR and change-control process. Cross-references use Constitutional Document IDs; filenames are non-authoritative.
