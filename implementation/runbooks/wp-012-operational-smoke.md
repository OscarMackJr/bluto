# WP-012 Operational Smoke Runbook

This implementation runbook supports WP-012 nonproduction smoke evidence. It is governed by `BLUTO-ADR-0012`, `BLUTO-ADR-0014`, `BLUTO-ADR-0015`, `BLUTO-ADR-0016`, `BLUTO-OPS-INFRA-001`, `BLUTO-OPS-RELEASE-001`, `BLUTO-OPS-OBS-001`, `BLUTO-OPS-DR-001`, `BLUTO-SEC-IAM-001`, and `BLUTO-SEC-CRYPTO-001`.

## Scope

This runbook covers nonproduction operational smoke only. It does not authorize production rollout, public database endpoints, embedded credentials, portal-only resources, disabled authorization, or disabled tenant isolation.

## Required Evidence

- Terraform format and validation transcript.
- Immutable API and worker image digests.
- SBOM and vulnerability scan artifacts.
- Runtime configuration validation result.
- API health and mapping-read smoke transcript.
- Worker dry-run smoke transcript.
- Telemetry query reference for API latency, batch completion, outbox lag, and errors.
- Rollback exercise transcript.

## Failure Handling

- Migration failure: stop application promotion, keep the previous revision serving traffic, preserve migration logs, and use approved forward-recovery migration rules.
- API degradation: roll back to the previous healthy revision and record the dependency state without exposing tenant data.
- Outbox lag: disable publisher execution if needed, preserve pending facts for replay, and verify no duplicate event publication.
- Key Vault reference outage: fail closed with structured degraded responses; do not fall back to local credential values.

## Rollback Exercise

1. Capture the current API and worker revisions.
2. Roll back to the previous known-good API revision.
3. Disable the worker schedule.
4. Run the API health and mapping-read smoke checks.
5. Record whether rollback is safe or forward recovery is required.
