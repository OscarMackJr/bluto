# Bluto: Enterprise Identity Spine

Version: 0.1.1
Status: Chartered in response to architecture evaluation gap C1. The v0.1.1 changes are **ratified 2026-10-21**, together with IR-1 (record at the end).
Audience: Bluto team (new), hometown team, data stewards, compliance, CTO
Primary post-read action: approve the resolution model and the merge/split semantics, then authorize the v1 deterministic slice.
Program: Fiskroad. Repository: `bluto` (fifth member project).

**Changes in 0.1.1 (2026-10-21, proposed):** corrects two statements the certified baseline got wrong, before the Stream 10 work package is written from them.
1. **bluto does not own a review queue.** Identity-review cases go to olive's shared queue (program charter §6.3, olive REQ-QUE-04), under item contract IR-1 (`olive/contracts/queue/identity-review/QUEUE_ITEM_CONTRACT_identity-review_v0.1.md`). bluto decides what is queued, holds the evidence, and applies decisions. §2, §8, §9, §11, §12 and §13 change.
2. **bluto does not define an ATE version.** hometown owns the Answer Trace Envelope. The identity fields bluto needs are **ATE v0.3**, already specified in `HOMETOWN_CORRECTIONS_ATE_v0.3.md` §3 (`identity.partyIds`, `identity.rulesetVersion`, per-evidence `partyId`), with bluto's review obligations in `BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` §1. v0.1's "ATE v0.2" was stale: v0.2 shipped the retrieval half and omitted the identity half, which v0.3 carries. The fields still need **explicit joint hometown–bluto approval** (§7, §13).

Also: `tenant_id` becomes a vocabulary UUID (§3), and links record the review item that produced them (`review_item_id`).

**Revision 2026-10-21 (after owner review):**
- **§7 and §13** cite ATE v0.3 (`HOMETOWN_CORRECTIONS_ATE_v0.3.md` §3) as the identity-field definition. The joint approval stays open.
- **The new requirements are numbered REQ-ID-12..17**, because `BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` already uses REQ-ID-08..11.
- **§9.1** formally supersedes the review-queue APIs in bluto's baselined contracts. Only a read-only evidence endpoint remains as an inbound surface.
- **§3.3** adds `review_candidate`, bluto's local record of what its rules couldn't settle. Its `accepted`/`rejected` statuses are bluto's projection of having applied an olive decision, not a duplicate decision record.
- **§4 and §12** state that v1 records candidates without submitting them; submission to olive starts in v1.1.
- **§9** requires the vendored, hash-pinned copy of IR-1.
- **Cross-contract review (2026-10-21):** a new-ruleset candidate is submitted normally; earlier decisions on the pair appear only as references in the evidence (§3.3, §9.1). Three implementation invariants are also made explicit:
  - deterministic candidate identity with a canonical pair order (§3.3);
  - the candidate's tenant bound to the popeye token's tenant on every olive call (§9.2);
  - one review-derived link per `review_item_id` (§3.2, §9).

  IR-1 is unchanged.

## 1. Why Bluto Exists

hometown provides governed semantic models per source system. It does not resolve identity *across* them. Today a question that spans Nexus CRM, the ledger, and Intrepid has no defined join: there is no answer to "is this CRM contact the same person as this borrower?" that is auditable, reproducible, or stable over time.

Bluto is the missing spine. It maintains the mapping between source-system keys and stable enterprise entity identifiers, produced by explicit versioned rules, with full lineage for every assertion.

**What Bluto is not, and must never become:** a golden-record master data store. Bluto holds *identity mapping*, not entity attributes. Names, addresses, balances, and statuses stay in the source systems, consistent with hometown's virtualization-first principle. The moment Bluto starts storing a "best" customer name, it has become the warehouse the program exists to avoid. This is the discipline that keeps Bluto small.

## 2. Position In The Ecosystem

- **Bluto owns**: enterprise party identifiers, source-key links, match rules, confidence, **what goes to review and how review decisions are applied**, merge and split lineage, and the evidence a reviewer sees.
- **Bluto does not own**: semantic models or Cube (hometown); the Answer Trace Envelope (hometown); **the review queue, its UI or its decision records (olive)**; business attributes (source systems); gateway, budgets or service credentials (popeye); any interpretation of what an entity means in a business process.
- **olive hosts bluto's review cases** in the program's single shared queue (§9, contract IR-1). bluto creates items and reads decisions; olive is the system of record for the review itself.
- **hometown consumes** Bluto's mapping as a governed source, exactly as it consumes divisional systems: read-only, contract-validated, modelled in Cube. Cross-domain joins in hometown resolve through `party_id`, never through ad-hoc key matching in a Cube model.
- **Reliability coupling**: if Bluto is unavailable, cross-domain retrieval degrades to single-domain. This is a named case in hometown's semantic-path degradation policy.

Boundary rule, stated as the others are: **identity is resolved ahead of time and recorded, never inferred at query time.** A join that happens because a Cube model matched on normalized name is a bug, not a shortcut.

## 3. Data Model

Two tables carry the identity model, and a third records what the rules could not settle. All three are governed data with full history.

### 3.1 `party` — the resolved entity

| Column | Type | Notes |
|---|---|---|
| `party_id` | uuid, PK | Stable and **never reused**, including after merge or retirement |
| `party_type` | enum | `person` \| `organization` |
| `tenant_id` | uuid, nullable | Resolution scope (section 6). A vocabulary tenant UUID, never a display label: a string tenant joins to nothing. Null only for untenanted sources (§13) |
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
| `tenant_id` | uuid, nullable | Carried from the source record; same rule as `party.tenant_id` |
| `match_rule_id` | text | Versioned rule that produced this link — e.g. `det.tax_id.v1` |
| `match_method` | enum | `deterministic` \| `review` \| `manual` |
| `confidence` | numeric | 1.0 for deterministic; rules define otherwise |
| `effective_from` | timestamptz | |
| `effective_to` | timestamptz, nullable | Null means currently in force |
| `status` | enum | `active` \| `superseded` \| `rejected` |
| `asserted_by` | text | System rule id, or the reviewer identity for manual links |
| `asserted_at` | timestamptz | |
| `review_item_id` | uuid, nullable | For `match_method = review`: the olive queue item whose decision produced this link (IR-1 §3). Joins the link to olive's audit record. **Unique where not null** (below) |

Constraint: at most one `active` link per (`source_system`, `source_key`) at any instant. A source record belongs to exactly one party at a time.

Constraint: **at most one link per `review_item_id`**, enforced by a unique index on `review_item_id` where it is not null, across all statuses. Only the link written *directly* by applying a review decision carries `review_item_id`. Links created later by a merge or split carry `null` and keep their lineage through `effective_from`/`effective_to` and `status`. The original review link keeps its `review_item_id` when superseded, so the index never sees two rows for one decision.

### 3.3 `review_candidate`: what the rules could not settle

| Column | Type | Notes |
|---|---|---|
| `candidate_id` | uuid, PK | **Deterministic**: a UUIDv5 over the RFC 8785 canonical form of the identity tuple below, under a fixed bluto namespace. Becomes IR-1's `producer_ref` in v1.1 |
| `tenant_id` | uuid | One vocabulary tenant; cross-tenant candidates are not created (§6) |
| `left_source_system`, `left_source_key` | text | First record of the pair **in canonical order** (below) |
| `right_source_system`, `right_source_key` | text | Second record of the pair in canonical order |
| `reason` | enum | `no_deterministic_match` \| `conflicting_matches` |
| `rules_considered` | text[] | Rule ids evaluated, with the versioned ruleset |
| `ruleset_version` | text | Git SHA of the ruleset in force |
| `status` | enum | `recorded` (v1) \| `submitted` \| `withdrawn` \| `accepted` \| `rejected` \| `stale` |
| `review_item_id` | uuid, nullable | olive's `item_id` once submitted (v1.1). **Unique where not null**: one candidate per olive item |
| `created_at`, `updated_at` | timestamptz | |

**Candidate identity.** A candidate is identified by the tuple `(tenant_id, left_source_system, left_source_key, right_source_system, right_source_key, reason, ruleset_version)`:
- **Canonical pair order.** The two `(source_system, source_key)` records are ordered by comparing `source_system`, then `source_key`, as UTF-8 byte strings (binary collation, no case folding). The smaller is `left`. A pair found in either direction therefore produces the same row.
- **Uniqueness.** A unique constraint on the tuple. Because `candidate_id` is derived from it, a re-run, a reversed pair or a rebuilt database produces the same `candidate_id`, and so the same IR-1 `producer_ref`. olive's idempotency then guarantees one item.
- **A new ruleset means a new candidate.** A changed `ruleset_version` gives a different tuple, so a different `candidate_id`. The old candidate's undecided submission is withdrawn with reason `ruleset_retired` (IR-1 §4). If the new ruleset settles the pair deterministically, no new candidate is created and the old one is withdrawn with `superseded_by_rule`.
- **A new-ruleset candidate is submitted normally, even if the pair was rejected before.** Earlier decisions don't carry forward or suppress anything, because there is no path for a steward to reopen a suppressed pair (IR-1's reserved `link_challenge` covers existing links only). Instead, the steward sees the history: the evidence endpoint (§9.1) returns the `review_item_id`s of earlier decided candidates for the same tenant, canonical pair and reason, so the steward can look them up in olive. These are references only, never decisions copied into bluto.

**`status` is bluto's application state, not a second decision record.** `accepted` and `rejected` mean bluto has *applied* an olive decision: it wrote the link, or recorded that no link is to be made. They are a projection that lets bluto skip candidates already handled. olive's item, with its reviewer, rationale, time and history, remains the only decision record. When the two seem to disagree, olive's record is authoritative for what was decided and bluto's links are authoritative for identity. `review_item_id` joins them.

**This is not a queue.** It has no assignment, no claim, no reviewer, no decision entry and no UI. It records *why* bluto didn't link a pair. It is the evidence behind IR-1's `payload_ref`, and the place bluto notes what happened to its submission. Like the other tables, it holds keys and rule ids only: no business attributes and no strong identifiers, raw or tokenised.

## 4. Match Rules

Match rules are versioned, source-controlled artifacts. Every link records the rule id that produced it, so any assertion can be reproduced and explained years later.

**v1 is deterministic only.** Three rule classes:

1. **Existing referential keys** (`det.fk.*`) — where a source system already carries a foreign key to another (Intrepid borrower to CRM contact, for example). Highest confidence, zero inference; this is the cheapest and largest win and should be exhausted first.
2. **Strong identifiers** (`det.tax_id.v1`, `det.gov_id.v1`) — exact match on a tokenized strong identifier. See section 5 on how these are stored.
3. **Composite exact** (`det.name_dob_addr.v1`) — exact match on a normalized composite. Normalization (casing, punctuation, address standardization) is part of the rule definition and versioned with it.

Anything not matched by a deterministic rule **does not auto-link**. It is recorded as a `review_candidate` (§3.3). **In v1 it stays there**: recorded, counted in the metrics, not submitted. From v1.1 it is submitted to olive's shared queue for review (§9). For a lender, wrongly merging two customers is a disclosure incident, not a data-quality nit — so v1 accepts lower coverage in exchange for near-zero false merges.

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

**Cross-component contract.** For answers to stay explainable across merges and splits, the Answer Trace Envelope must record:
- the `party_id` values the answer's evidence was resolved through; and
- the identity ruleset version in force at answer time.

**hometown owns the ATE.** The field names, their placement in the envelope and the ATE version number are defined once, in hometown's ATE specification, and nowhere else. This section states bluto's requirements; it does not define fields. It is a two-team contract change under the Fiskroad invariants: reviewed in both `hometown` and `bluto` before it lands in either, and treated as a **shared** decision, not a bluto open item.

**Where the fields are defined: ATE v0.3.** `HOMETOWN_CORRECTIONS_ATE_v0.3.md` §3 adds an optional `identity` object (`partyIds`: every party identity used to resolve or join evidence; `rulesetVersion`: the bluto ruleset version in force, exactly as bluto emits it in `party-created.v1`) and an optional per-evidence `partyId`. bluto's side of the review is `BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` §1: `rulesetVersion` matches the event contract with no translation; point-in-time semantics hold across a later merge; consumers read bluto's API, never its tables (REQ-ID-08, REQ-ID-09 there).

v0.1 of this spec called this "ATE v0.2". That was stale: v0.2 shipped the retrieval and degradation half and omitted the identity half, which v0.3 exists to carry. There is one change and one version, so there is nothing left to reconcile on paper.

**What remains is the approval itself**, an explicit joint hometown–bluto decision that has not yet been made:
- it is the "joint ATE v0.3 review" in the v0.3 correction set's sequencing (step 3): one session, both teams;
- the same session names the **owner of the HMAC tokenization key** (`BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` §2, REQ-ID-10);
- it is **hometown v0.4's prerequisite** ("ATE v0.3 identity fields merged before v0.4 work begins") and POC-2 entry criterion 1, so it sits on both teams' critical path;
- it does **not** block IR-1, the queue contract.

## 8. Resolution Pipeline

Incremental batch, not a query-time service. From v1.1, each run first reads the review decisions made since the last run, tenant by tenant, and applies them (§9; IR-1 §6). Then:

1. Extract candidate keys from each source system (read-only, replica-preferred, subject to the source-load budget hometown defines).
2. Normalize and tokenize in memory.
3. Apply rules in priority order; write links for deterministic matches.
4. Record non-matches and conflicting matches as `review_candidate` rows (§3.3). **From v1.1**, submit new candidates to olive's shared queue as `identity_review` items (IR-1 §4), idempotently, so a re-run never duplicates an item, and withdraw items the run has made obsolete.
5. Emit metrics: coverage per source, new links, queue depth, conflicts.

Cadence for v1: scheduled, with an on-demand trigger. Streaming resolution is not required and should not be built.

## 9. Review Workflow

**The queue is olive's.** Identity-review cases go to the program's single shared review queue in olive (program charter §6.3; olive REQ-QUE-04). bluto builds no queue, no review UI and no decision store. The item, decisions and their transport are defined by **contract IR-1**, which olive owns and both teams agree. bluto implements the **vendored, hash-pinned copy** (`bluto/contracts/vendored/olive/`, IR-1 §10); olive's copy is authoritative. This section only says how bluto uses it. **v1.1 and later**: v1 records candidates and submits none (§4, §12).

- **What gets queued.** Candidates no deterministic rule settled, and conflicting matches (§4, §8). The deterministic threshold is the gate: only what the rules couldn't settle reaches a person.
- **What the reviewer sees.** The queue item carries references only. The reviewer's client resolves `payload_ref` through bluto's evidence endpoint, which returns the source-key pairs and the rule evidence: no business attributes, no strong identifiers, raw or tokenised (§5, §10). Attributes come from the source systems through hometown, under the reviewer's own entitlements.
- **Decisions.** `accept`, `reject` or `defer`, with a required rationale, by a member of `role:identity_steward` (IR-1 §3).
- **Applying decisions.** An `accept` writes an `active` link with `match_method = review`, `asserted_by` = the reviewer identity olive recorded, and `review_item_id` = the olive item. Application is **one transaction**: insert the link, relying on the unique `review_item_id` index (§3.2), and update the candidate's status. If the link already exists, the insert does nothing, the existing link is returned as the result, and the candidate's status is left as it is. A retry, a re-run or two concurrent runs therefore produce exactly one link. A `reject` is recorded on the candidate in the same way, keyed by its unique `review_item_id`. A decision on a candidate that has since been superseded is recorded but not applied. Identity truth is bluto's effective-dated links, not the queue.
- **Audit.** olive holds the review record: claim, decision, reviewer, time, rationale. bluto holds the link and its lineage. `review_item_id` joins the two.
- **Splits.** A steward-initiated challenge to an existing link (`link_challenge`) is reserved in IR-1 and waits on the merge/split approval (§13).

### 9.1 Superseded baseline surfaces

bluto's certified baseline defines review-queue commands and endpoints that this revision removes. They are **superseded, not retained**, and are deleted from the baseline in the same contract-change PR that adopts this revision:

| Baseline surface | Where | Disposition |
|---|---|---|
| `ListReviewQueue` | `docs/04-contracts/COMMAND-QUERY-CONTRACTS.md` | **Removed.** Listing is olive's (`GET /v1/queue/items`) |
| `GetReviewCase` | same | **Removed.** Replaced by the read-only evidence query below |
| `RecordReviewDecision` | same | **Removed.** Decisions are recorded in olive; bluto applies them internally (§9) |
| `POST /v1/review-cases/{review_case_id}/decisions` | `docs/04-contracts/schemas/bluto-v1.openapi.yaml` | **Removed.** No inbound decision API in bluto. Decisions reach bluto only by its own pull from olive (IR-1 §6) |
| Any review-case list or read path in the same OpenAPI document | same | **Removed**, except the evidence endpoint below |

**The only review-related surfaces bluto keeps (v1.1):**
- **`GET /v1/review-evidence/{candidate_id}`.** Read-only. Called by a reviewer's client with the reviewer's own credential, resolving IR-1's `payload_ref`. Returns the two source-key pairs, `reason`, `rules_considered` and `ruleset_version` from `review_candidate`, plus `prior_review_item_ids`: the olive `item_id`s of earlier decided candidates for the same tenant, canonical pair and reason, oldest first (empty if none). Nothing else. Tenant-scoped like every bluto read.
- **The decision-application step** inside the batch pipeline (§8). It is internal: an outbound pull from olive, not an API anyone calls.

The baseline documents are bluto's to change; this spec states what they must say. Until the PR lands, the conflicting baseline text is **not** to be implemented.


### 9.2 Tenant binding on every olive call

olive takes a queue item's tenant from the popeye token, not from bluto's body (IR-1 §2, §4). A token for the wrong tenant would put a valid candidate into the wrong tenant's queue. bluto therefore checks, **before every olive call**, that the token's `tenant_id` equals the tenant of the data involved:
- **Submit and withdraw:** `candidate.tenant_id == token.tenant_id`. On mismatch: no olive call, nothing changes, a `review.tenant_mismatch` event is logged, and the run alarms.
- **Poll:** bluto requests a token per tenant and polls with it. Every item returned must have `tenant_id` equal to the token's, and its `producer_ref` must name a candidate in that same tenant. A returned item that fails either check is **not applied**; it is logged as `review.tenant_mismatch` and the run alarms. This guards against a fault on olive's side as well as bluto's.
- **Apply:** a decision is applied only to a candidate whose `tenant_id` equals the polled item's.

The token's tenant is read from the token itself (popeye introspection, or the token's verified claims), never from configuration that could drift from it.

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
| REQ-ID-03 | Sub-threshold candidates never auto-link | TC-ID-03 | A deliberately ambiguous pair produces one `review_candidate` and zero active links. In v1.1 builds it also produces one `identity_review` item on olive's queue (a stub in CI) | CI |
| REQ-ID-04 | History survives identity change | TC-ID-04 | After a merge, a point-in-time query for a pre-merge date returns the pre-merge party; a pre-merge ATE trace remains resolvable | CI |
| REQ-ID-05 | No readable strong identifiers at rest | TC-ID-05 | Inspection of the Bluto store finds only HMAC tokens; the HMAC key is absent from the database and its configuration | AUDIT |
| REQ-ID-06 | Resolution respects tenant boundaries | TC-ID-06 | Records from two tenants that share a strong identifier do **not** link absent recorded authorization; the attempt is logged | CI |
| REQ-ID-07 | Identifiers are never reused | TC-ID-07 | Merge and split exercises show no `party_id` reuse and no deletion of prior ids | CI |
| REQ-ID-12 | bluto keeps no review queue of its own; review cases go to olive under IR-1 | TC-ID-12 | Code, schema and contract audit: no queue, claim or decision tables; no review UI; none of the §9.1 removed surfaces in the command-query contract or the OpenAPI document; the evidence endpoint is the only review-related inbound route. From v1.1, every submitted candidate maps to one olive `identity_review` item | AUDIT |
| REQ-ID-13 | Review items carry references only | TC-ID-13 | Items bluto creates contain no record values (with olive TC-QUE-01 and IR-1 TC-IRQ-01); the evidence endpoint returns no attributes or strong identifiers | CI |
| REQ-ID-14 | Review decisions are applied idempotently and only when still current | TC-ID-14 | IR-1 TC-IRQ-07 and TC-IRQ-08 against an olive queue stub | CI |
| REQ-ID-15 | `tenant_id` is a vocabulary UUID | TC-ID-15 | A display-label tenant is rejected on write; every non-null `tenant_id` is one of the vendored vocabulary's UUIDs | CI |
| REQ-ID-16 | v1 records unresolved candidates and submits none | TC-ID-16 | A v1 build with an ambiguous pair: one `review_candidate` row with `status = recorded`; zero calls to olive (no client configured); zero active links | CI |
| REQ-ID-17 | bluto implements the hash-pinned vendored copy of IR-1 | TC-ID-17 | CI fails if `contracts/vendored/olive/` differs from the pinned SHA-256 in `VENDORED.md` | CI |
| REQ-ID-18 | A candidate is unique per tenant, canonical pair, reason and ruleset version; its id is deterministic | TC-ID-18 | The same pair found as (A,B) and as (B,A) in one run and across two runs gives one row and one `candidate_id`; a rebuilt database gives the same `candidate_id`; a new `ruleset_version` gives a new candidate and withdraws the old undecided submission (`ruleset_retired`); a pair rejected under the old ruleset is submitted again under the new one, and the evidence endpoint lists the earlier `review_item_id` | CI |
| REQ-ID-19 | bluto never calls olive with a token whose tenant differs from the data's | TC-ID-19 | A tenant-A candidate with a tenant-B token: zero olive calls, `review.tenant_mismatch` logged, alarm. A polled item whose tenant or candidate tenant differs from the token's: not applied, logged, alarm | CI |
| REQ-ID-20 | One review-derived link per `review_item_id` | TC-ID-20 | Apply the same accepted decision twice in sequence, and twice concurrently: exactly one link; the second application returns the first link; candidate status unchanged by the repeat. A merge after a review link: the new links have `review_item_id = null`; the unique index holds | CI |

## 12. Delivery Slices

- **v1 (before POC; no dependency on olive, popeye or nono; hometown only for the live demo):** schema, including `review_candidate` (recorded, **not submitted**: TC-ID-16), referential-key rules (`det.fk.*`), tokenized strong-identifier matching, tenant scoping, effective dating, the seeded three-system demo (TC-ID-01), and the tenant-boundary test (TC-ID-06). This is what the partner review needs.
- **v1.1:** composite exact rules; review through olive's shared queue under IR-1 (creating items, the evidence endpoint, applying decisions); coverage metrics. Needs olive's queue (olive Phase 1), popeye's tenant-scoped tokens with a `bluto` registry entry, and a named `role:identity_steward`; stubs until then.
- **v2 (not chartered):** probabilistic matching with its own data-protection and compliance review; organization hierarchy resolution.

## 13. Open Decisions

- **Merge/split semantics** (§7), and with them the `link_challenge` item (IR-1 §7.1). Approve before v1 is authorised.
- Store placement: dedicated Postgres versus a schema in the existing shared instance. Recommendation: dedicated, since Bluto's data classification differs from hometown's. A third option, used by olive for its POC, is **its own database on the shared POC instance** with forced row-level security, moving to a dedicated server later as a connection-string change (olive decision record §13). It keeps bluto's data out of other projects' grants without a new server for the POC.
- Data stewardship ownership — who adjudicates identity review on olive's queue. A business role, not an engineering one; it becomes the Entra group behind `role:identity_steward` and needs a name before v1.1.
- Whether ledger and CRM already carry usable referential keys to each other. This determines v1 coverage and should be answered by inspection **this week**, since it may make v1 substantially cheaper than assumed.
- **Shared with hometown, not bluto's alone: approve ATE v0.3's identity fields** (`HOMETOWN_CORRECTIONS_ATE_v0.3.md` §3, `BLUTO_ATE_AND_KEY_OWNERSHIP_v0.3.md` §1) in one joint session, and name the HMAC tokenization key owner in the same session (§7). The version question is settled: it is v0.3; v0.1's "v0.2" was stale. The approval is not. It is hometown v0.4's prerequisite and POC-2 entry criterion 1. It does not block IR-1.
- **Untenanted sources.** olive's queue requires a tenant on every item. Decide whether records from untenanted systems (possibly the CRM and the ledger) belong to a designated enterprise tenant UUID, which would be a vocabulary addition, or stay outside review (IR-1 §7.3).
- **Purpose for reviewer evidence reads** through hometown (IR-1 §7.2).
- **popeye registry entry for bluto** (tenant-scoped service tokens, no delegated agent). Raise with popeye alongside olive's acknowledged dependency.

---

## Ratification (v0.1.1)

| Field | Value |
|---|---|
| Decision | **Ratified**: the v0.1.1 corrections as revised 2026-10-21, including the cross-contract invariants (§3.2, §3.3, §9.2) and option 1 for re-submitting under a new ruleset |
| Ratified by | Oscar Mack (architecture and risk decisions) |
| Ratified together with | `QUEUE_ITEM_CONTRACT_identity-review_v0.1.md` (IR-1). olive and bluto owner agreement, 2026-10-21, recorded there |
| Date | 2026-10-21 |
| Not decided here | The §13 open decisions, including merge/split semantics and the shared ATE identity fields |
| Still requires | bluto's contract-change PR removing the superseded queue APIs (§9.1) from `COMMAND-QUERY-CONTRACTS.md` and `bluto-v1.openapi.yaml`; confirmation that REQ-ID-12..20 don't collide with baseline numbering. The §13 open decisions, including the joint ATE v0.3 approval, remain open |
| Effect | The Stream 10 work package is written against this revision, not v0.1. bluto v1 can be built with stand-ins; v1.1's review work is built against IR-1 and an olive queue stub |
