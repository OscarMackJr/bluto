# ADR-0017 - Event Transport

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0017 |
| Artifact ID | ART-BLUTO-ADR-0017-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ARCH-TECH-001, BLUTO-CONTRACT-EVENT-001, BLUTO-ADR-0004 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

The baseline requires transactional outbox and broker-neutral integration events. An external broker is optional for v1; deterministic scheduled batch resolution and HTTP/JSON query APIs remain primary.

## Governing Document IDs

`BLUTO_IDENTITY_SPINE_v0.1`, `BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-RUNTIME-001`, `BLUTO-ARCH-INTEGRATION-001`, `BLUTO-CONTRACT-EVENT-001`, `BLUTO-CONTRACT-SCHEMA-001`, `BLUTO-OPS-OBS-001`, `BLUTO-TEST-INTEGRATION-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| PostgreSQL transactional outbox with worker publisher; no external broker required for v1 | Lowest complexity, atomic with state changes, aligns with broker-neutral baseline. |
| Azure Service Bus topics for external publication | Good Azure-managed fan-out and at-least-once delivery when external consumers require pushed events. |
| Kafka/Event Hubs | Strong streaming platform, but conflicts with v1 non-streaming resolution needs and adds operational complexity. |
| Synchronous HTTP callbacks | Simple but brittle and poor for durable event delivery. |

## Criteria

Atomicity with domain state, broker neutrality, at-least-once semantics, idempotency, ordering by aggregate where supported, operational visibility, low v1 complexity, and external consumer readiness.

## Security And Operational Impact

Outbox records must exclude raw strong identifiers and external HMAC tokens. Publisher identities must be scoped, events must be schema validated, and consumers must be idempotent. Outbox lag is an SLO/alert dimension.

## Compatibility Impact

This decision preserves transactional outbox and broker-neutral events. It explicitly avoids making an external broker mandatory for v1.

## Recommendation

Use PostgreSQL transactional outbox as the required event transport boundary for v1. Publish from the worker or separately executable publisher. If v1 external consumers require push delivery, use Azure Service Bus topics as an optional adapter fed from the outbox.

## Consequences

Initial v1 can operate without a broker while still recording publishable facts. If Service Bus is enabled, delivery remains at least once and consumers must handle duplicates.

## Migration Or Replacement Considerations

Kafka, Event Hubs, or another broker can replace Azure Service Bus only behind the outbox publisher adapter and without changing event schemas or domain transaction semantics.
