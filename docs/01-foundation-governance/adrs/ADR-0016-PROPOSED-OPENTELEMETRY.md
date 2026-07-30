# ADR-0016 - OpenTelemetry Implementation

| Metadata | Value |
|---|---|
| Document ID | BLUTO-ADR-0016 |
| Artifact ID | ART-BLUTO-ADR-0016-v0.1.0 |
| Version | 0.1.0 |
| Status | Approved |
| Owner | Enterprise Architecture |
| Baseline | Implementation Technology Decision Package |
| Stream | Implementation |
| Authority Level | RAH-12 |
| Depends On | BLUTO-ADR-0001, BLUTO-ARCH-TECH-001, BLUTO-ENG-OBS-001 |
| Supersedes | None |
| Approval Date | Not approved |
| Effective Date | Not effective |
| Review Date | 2026-08-30 |
| Classification | Internal |

## Decision Context

Telemetry must be OpenTelemetry-compatible at the interface level while observability backend remains replaceable. Bluto needs correlated traces, metrics, and structured logs across API, worker, stewardship, database access, outbox, migrations, and scheduled batches.

## Governing Document IDs

`BLUTO-ARCH-TECH-001`, `BLUTO-ARCH-CROSSCUT-001`, `BLUTO-ENG-OBS-001`, `BLUTO-OPS-OBS-001`, `BLUTO-OPS-SLO-001`, `BLUTO-SEC-AUDIT-001`.

## Evaluated Options

| Option | Assessment |
|---|---|
| OpenTelemetry .NET SDK with OTLP export and Azure Monitor/collector bridge | Vendor-neutral instrumentation with Azure-compatible backend path. |
| Azure Monitor SDK only | Strong Azure integration but weaker backend portability. |
| Serilog-only logs and custom metrics | Useful logging stack but insufficient for traces/metrics standardization. |
| Custom telemetry abstraction | Unnecessary and likely inferior to OTel standards. |

## Criteria

Trace/metric/log coverage, context propagation, backend portability, Azure support, low application coupling, data redaction controls, SLO measurement, and operational troubleshooting.

## Security And Operational Impact

Telemetry must never include raw strong identifiers, HMAC tokens, secrets, or unauthorized tenant data. Correlation IDs, tenant-safe identifiers, batch IDs, rule versions, and outbox lag are required operational dimensions.

## Compatibility Impact

OpenTelemetry preserves the baseline decision for OTel-compatible telemetry while selecting a concrete .NET implementation. Backend remains replaceable.

## Recommendation

Use OpenTelemetry .NET SDK for traces, metrics, and logs. Export through OTLP to an OpenTelemetry Collector or Azure Monitor-compatible ingestion path. Treat Azure Monitor as the proposed v1 backend only through the cloud-target ADR.

## Consequences

Instrumentation must be designed into repository, outbox, batch, and API boundaries early. Redaction and attribute allowlists become required review artifacts.

## Migration Or Replacement Considerations

Backend replacement should require configuration and collector changes, not code-level instrumentation rewrites. SDK replacement requires preserving OTel semantic conventions and data controls.
