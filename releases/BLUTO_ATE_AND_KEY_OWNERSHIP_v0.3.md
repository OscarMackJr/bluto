# Bluto v0.3 — ATE Identity Review And Token Key Ownership

Version: 0.3
Status: Draft — Bluto's half of cross-repo finding X3 and the closure of the B1 residual
Repository: bluto. Audience: Bluto team; hometown team (joint review).
Primary post-read action: complete the two-team ATE v0.3 review (section 1) and record the key owner (section 2). Both are meetings plus paragraphs, not builds.

## 1. ATE v0.3 Joint Review (with hometown)

hometown's `HOMETOWN_CORRECTIONS_ATE_v0.3` proposes the identity additions to the Answer Trace Envelope: optional `identity.partyIds`, `identity.rulesetVersion`, and per-evidence `partyId`. Bluto is the co-owner of that contract change under the standing two-team invariant.

Bluto's review obligations, specifically:

1. **Confirm `rulesetVersion` semantics match the event contract.** The value in a trace must be the same string Bluto emits as `ruleset_version` in `party-created.v1` — same format, same versioning cadence, no translation layer. If the event schema and the trace disagree about what a ruleset version looks like, this review is where that dies.
2. **Confirm point-in-time semantics.** Party IDs in a trace are historical facts; Bluto's effective-dated links plus `status`/`merged_into_party_id` (V002) are what make a March trace resolvable after a May merge. The review asserts this works end to end: TC-PROV-13's seeded demo should include one merge performed *after* the trace is written, with the trace still resolving to the pre-merge view.
3. **Confirm consumers read the API, not the tables.** hometown obtains party IDs through Bluto's resolution surface; nothing in hometown queries `identity_resolution.*` directly. RLS would stop it anyway (V002), but the contract should say it, not merely the database enforce it.

| REQ | Requirement | TC | Test criteria | Method |
|---|---|---|---|---|
| REQ-ID-08 | Trace `rulesetVersion` equals event `ruleset_version` for the same resolution | TC-ID-08 | Contract test comparing a resolution's emitted event and the trace written from it | CI |
| REQ-ID-09 | A pre-merge trace resolves to the pre-merge party view | TC-ID-04b | Seeded demo: write trace, merge parties, re-resolve trace at its timestamp | DEMO + CI |

## 2. Tokenization Key Ownership (B1 residual)

Bluto now enforces `^v1\.[A-Za-z0-9_-]{43}$` on every identity token and stores the token verbatim — correct, and safe precisely because a raw identifier cannot match the pattern. What remains unowned is the other end: **some component computes HMAC-SHA-256 over normalized identifiers with a key, and no repository names that component or that key.**

To record, in this repository's authoritative spec (one section, not a new document):

1. **Owner.** The component that computes tokens. The natural candidates are the source-integration layer that extracts candidates for resolution (hometown's integrations) or a small dedicated tokenization utility invoked by them. Whichever is chosen, it is named, and the token format (`v1.` + base64url(HMAC-SHA-256(key, normalized-identifier)), 43 chars) is specified where the owner lives.
2. **Key custody.** The key is a Tier-0 secret in the platform Key Vault, readable by the owning component's managed identity and by nothing else — explicitly not by Bluto (the existing runtime-config test that forbids `hmac` in Bluto's configuration stays, and now has a stated reason).
3. **Normalization.** The pre-hash normalization rules (case, whitespace, format stripping) are versioned with the token version. A change to normalization is a new token version prefix (`v2.`), because it changes every token — which is also the rotation story:
4. **Rotation.** Key rotation = new version prefix + recompute campaign + dual-accept window in Bluto's pattern (`^v[12]\.` during migration) + retire the old prefix. Planned migration, not routine operation, as the v0.1 spec already stated — now with the mechanics written down.

| REQ | Requirement | TC | Test criteria | Method |
|---|---|---|---|---|
| REQ-ID-10 | The tokenization owner and key custody are named in the spec | TC-ID-10 | Spec review; the owner's repository documents the token computation | AUDIT |
| REQ-ID-11 | Bluto's configuration cannot hold the HMAC key | TC-ID-05b | Existing runtime-config negative test, retained | CI |
