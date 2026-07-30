# Bluto: Enterprise Identity Spine

Version: 0.1
Status: Draft — chartered in response to architecture evaluation gap C1
Audience: Bluto team (new), hometown team, data stewards, compliance, CTO
Primary post-read action: approve the resolution model and the merge/split semantics, then authorize the v1 deterministic slice.
Program: Fiskroad. Repository: `bluto` (fifth member project).

## 1. Why Bluto Exists

hometown provides governed semantic models per source system. It does not resolve identity *across* them. Today a question that spans Nexus CRM, the ledger, and Intrepid has no defined join: there is no answer to "is this CRM contact the same person as this borrower?" that is auditable, reproducible, or stable over time.

Bluto is the missing spine. It maintains the mapping between source-system keys and stable enterprise entity identifiers, produced by explicit versioned rules, with full lineage for every assertion.

**What Bluto is not, and must never become:** a golden-record master data store. Bluto holds *identity mapping*, not entity attributes. Names, addresses, balances, and statuses stay in the source systems, consistent with hometown's virtualization-first principle. The moment Bluto starts storing a "best" customer name, it has become the warehouse the program exists to avoid. This is the discipline that keeps Bluto small.

## 2. Position In The Ecosystem

- **Bluto owns**: enterprise party identifiers, source-key links, match rules, confidence and review workflow, merge and split lineage.
- **Bluto does not own**: semantic models or Cube (hometown), business attributes (source systems), gateway or budgets (popeye), any interpretation of what an entity means in a business process.
- **hometown consumes** Bluto's mapping as a governed source, exactly as it consumes divisional systems: read-only, contract-validated, modelled in Cube. Cross-domain joins in hometown resolve through `party_id`, never through ad-hoc key matching in a Cube model.
- **Reliability coupling**: if Bluto is unavailable, cross-domain retrieval degrades to single-domain. This is a named case in hometown's semantic-path degradation policy.

Boundary rule, stated as the others are: **identity is resolved ahead of time and recorded, never inferred at query time.** A join that happens because a Cube model matched on normalized name is a bug, not a shortcut.

## 3. Data Model

Two tables carry the model. Both are governed data with full history.

### 3.1 `party` — the resolved entity

| Column | Type | Notes |
|---|---|---|
| `party_id` | uuid, PK | Stable and **never reused**, including after merge or retirement |
| `party_type` | enum | `person` \| `organization` |
| `tenant_id` | text, nullable | Resolution scope — see section 6 |
| `status` | enum | `active` \| `merged` \| `retired` |
| `merged_into_party_id` | uuid, nullable | Set when `status = merged`; points at the surviving party |
| `created_at`, `updated_at` | timestamptz | |

### 3.2 `party_source_link` — the mapping

| Column | Type | Notes |
|---|---|---|
| `link_id` | uuid, PK | |
| `party_id` | uuid FK | |
| `source_system` | text | `nexus_crm` \| `intrepid` \| `ledger` |
| `source_key` | text | The natural key in that system |
| `tenant_id` | text, nullable | Carried from the source record |
| `match_rule_id` | text | Versioned rule that produced this link — e.g. `det.tax_id.v1` |
| `match_method` | enum | `deterministic` \| `review` \| `manual` |
| `confidence` | numeric | 1.0 for deterministic; rules define otherwise |
| `effective_from` | timestamptz | |
| `effective_to` | timestamptz, nullable | Null means currently in force |
| `status` | enum | `active` \| `superseded` \| `rejected` |
| `asserted_by` | text | System rule id, or the reviewer identity for manual links |
| `asserted_at` | timestamptz | |

Constraint: at most one `active` link per (`source_system`, `source_key`) at any instant. A source record belongs to exactly one party at a time.

## 4. Match Rules

Match rules are versioned, source-controlled artifacts. Every link records the rule id that produced it, so any assertion can be reproduced and explained years later.

**v1 is deterministic only.** Three rule classes:

1. **Existing referential keys** (`det.fk.*`) — where a source system already carries a foreign key to another (Intrepid borrower to CRM contact, for example). Highest confidence, zero inference; this is the cheapest and largest win and should be exhausted first.
2. **Strong identifiers** (`det.tax_id.v1`, `det.gov_id.v1`) — exact match on a tokenized strong identifier. See section 5 on how these are stored.
3. **Composite exact** (`det.name_dob_addr.v1`) — exact match on a normalized composite. Normalization (casing, punctuation, address standardization) is part of the rule definition and versioned with it.

Anything not matched by a deterministic rule **does not auto-link**. It enters the review queue. For a lender, wrongly merging two customers is a disclosure incident, not a data-quality nit — so v1 accepts lower coverage in exchange for near-zero false merges.

Probabilistic and fuzzy matching are explicitly deferred. They require a different data-protection design (section 5) and a tuned precision/recall decision, and neither is needed to close gap C1.

## 5. Protecting Match Keys

Strong identifiers are exactly the data least appropriate to accumulate in a new store.

**v1 requirement: no raw strong identifiers at rest in Bluto.** Match keys are stored as HMAC-SHA-256 tokens computed with a key held in the platform key vault and never present in Bluto's database. Deterministic matching is exact-match on the token, which works identically on hashes. Bluto therefore holds an identity mapping and no readable personal identifiers.

Consequences to record honestly:

- The HMAC key is a Tier-0 secret. Rotating it requires recomputing every token; treat rotation as a planned migration, not an operational routine.
- This design forecloses fuzzy matching on those fields. If probabilistic matching is later required, it needs its own data-protection design and compliance review — which is a feature of this decision, not a limitation of it.
- Normalization happens before hashing, in the resolution job's memory, sourced from the system of record. Bluto persists the token only.

## 6. Tenancy — The Subtle Risk

Intrepid is multi-tenant. **Resolving identity across tenant boundaries is itself a cross-tenant disclosure**: learning that tenant A's borrower is also tenant B's borrower is information neither tenant is entitled to.

Requirement: resolution is scoped within a tenant boundary by default. Cross-tenant resolution occurs only where an explicit, recorded authorization exists (for example, a genuinely shared enterprise-level system). `tenant_id` is carried on both tables so this is enforceable and auditable, and Bluto's own store is subject to the same defense-in-depth tenant isolation hometown applies (see the tenant isolation specification).

This is the single most important thing to say out loud in the partner review on this topic, because it is the failure mode a Foundry-fluent reviewer will look for and it is not obvious.

## 7. Merge, Split, And Historical Truth

Identity changes over time, and answer provenance must survive those changes. An Answer Trace Envelope written in March pins the evidence it used; if two parties merge in May, that March answer must still be explainable as it was then.

- **Merge**: the absorbed party's `status` becomes `merged` with `merged_into_party_id` set. Its links are `superseded` with `effective_to` stamped, and new links to the surviving party are created with `effective_from` at the merge instant. No identifier is deleted or reused.
- **Split** (a bad match undone): affected links are `rejected` with lineage retained, and new `party_id` values are minted for the separated entities. The original id is not recycled.
- **Point-in-time resolution is a first-class query.** "Which party did this source key belong to on 2026-03-14?" must be answerable from effective dating alone.

**Cross-component contract:** the Answer Trace Envelope should record the `party_id` values used and the identity ruleset version in force at answer time. This is an ATE v0.2 change and therefore a two-team contract change under the Fiskroad invariants — it must be reviewed in both `hometown` and `bluto` before it lands in either.

## 8. Resolution Pipeline

Incremental batch, not a query-time service:

1. Extract candidate keys from each source system (read-only, replica-preferred, subject to the source-load budget hometown defines).
2. Normalize and tokenize in memory.
3. Apply rules in priority order; write links for deterministic matches.
4. Queue non-matches and any conflicting matches for review.
5. Emit metrics: coverage per source, new links, queue depth, conflicts.

Cadence for v1: scheduled, with an on-demand trigger. Streaming resolution is not required and should not be built.

## 9. Review Workflow

A reviewer sees candidate pairs with the evidence that produced them and records accept, reject, or defer. Every decision writes an audited link with `match_method = review` and the reviewer identity in `asserted_by`. v1 needs a queue and an audit trail, not a polished application — the review surface can be minimal, but the audit record cannot.

## 10. Guardrails

- Identity mapping only. No business attributes, no golden records, no "best" values.
- No raw strong identifiers at rest.
- Party identifiers are never reused, including after merge or retirement.
- No auto-linking below the deterministic threshold.
- Resolution is scoped within tenant boundaries unless explicitly authorized.
- Every link carries a versioned rule id and is reproducible from it.
- hometown reads Bluto; hometown never writes it, and never matches identity in a Cube model.

## 11. Requirements And Test Criteria

| REQ | Requirement | TC | Test criteria | Method |
|---|---|---|---|---|
| REQ-ID-01 | Source records for one real entity resolve to one stable party | TC-ID-01 | A seeded customer present in CRM, ledger, and Intrepid resolves to a single `party_id`; a hometown cross-domain query returns one entity, not three | DEMO |
| REQ-ID-02 | Every link is reproducible from its rule | TC-ID-02 | Re-running the named `match_rule_id` against the same inputs reproduces the link exactly | CI |
| REQ-ID-03 | Sub-threshold candidates never auto-link | TC-ID-03 | A deliberately ambiguous pair produces a review-queue entry and zero active links | CI |
| REQ-ID-04 | History survives identity change | TC-ID-04 | After a merge, a point-in-time query for a pre-merge date returns the pre-merge party; a pre-merge ATE trace remains resolvable | CI |
| REQ-ID-05 | No readable strong identifiers at rest | TC-ID-05 | Inspection of the Bluto store finds only HMAC tokens; the HMAC key is absent from the database and its configuration | AUDIT |
| REQ-ID-06 | Resolution respects tenant boundaries | TC-ID-06 | Records from two tenants that share a strong identifier do **not** link absent recorded authorization; the attempt is logged | CI |
| REQ-ID-07 | Identifiers are never reused | TC-ID-07 | Merge and split exercises show no `party_id` reuse and no deletion of prior ids | CI |

## 12. Delivery Slices

- **v1 (before POC):** schema, referential-key rules (`det.fk.*`), tokenized strong-identifier matching, tenant scoping, effective dating, the seeded three-system demo (TC-ID-01), and the tenant-boundary test (TC-ID-06). This is what the partner review needs.
- **v1.1:** composite exact rules, review queue with audit, coverage metrics.
- **v2 (not chartered):** probabilistic matching with its own data-protection and compliance review; organization hierarchy resolution.

## 13. Open Decisions

- Store placement: dedicated Postgres versus a schema in the existing shared instance. Recommendation: dedicated, since Bluto's data classification differs from hometown's.
- Data stewardship ownership — who adjudicates the review queue. This is a business role, not an engineering one, and it needs a name before v1.1.
- Whether ledger and CRM already carry usable referential keys to each other. This determines v1 coverage and should be answered by inspection **this week**, since it may make v1 substantially cheaper than assumed.
- ATE v0.2 fields for `party_id` and ruleset version (two-team contract change).
