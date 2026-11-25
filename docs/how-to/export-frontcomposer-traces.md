---
title: "Export FrontComposer traces"
description: "Subscribe to FrontComposer activities in an ASP.NET Core host and route them to an OpenTelemetry collector."
genre: how-to
audience: adopter
ownerStory: DW-960
status: published
reviewed: 2026-11-25
uid: frontcomposer.how-to.export-traces
slug: how-to/export-frontcomposer-traces
---

# Export FrontComposer traces

FrontComposer publishes activities through the `Hexalith.FrontComposer` source.
The ASP.NET Core host chooses the collector, exporter, and sampling policy.

1. Reference `OpenTelemetry.Extensions.Hosting` and
   `OpenTelemetry.Exporter.OpenTelemetryProtocol` from the host project.
2. Subscribe to the FrontComposer activity source before calling
   `builder.Build()`. If the host already configures tracing, add the source
   to that configuration and reuse its exporter.

```csharp no-compile reason="Requires the host-selected OpenTelemetry hosting and OTLP exporter packages."
using Hexalith.FrontComposer.Contracts.Telemetry;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(FrontComposerActivitySource.Name)
        .AddOtlpExporter());
```

3. Set `OTEL_EXPORTER_OTLP_ENDPOINT` in the host environment to the collector
   endpoint. For a local collector with gRPC enabled, use
   `http://localhost:4317` and set `OTEL_EXPORTER_OTLP_PROTOCOL` to `grpc`.
   [OpenTelemetry .NET exporter configuration](https://opentelemetry.io/docs/languages/dotnet/exporters/)
4. Run the host and exercise its EventStore-backed command and query paths.
   Look for `frontcomposer.command.dispatch` and `frontcomposer.query.execute`
   in the collector or the connected trace viewer. The Counter sample can
   also run with in-memory services. Use an EventStore-backed host to see
   these two activities.

Message and correlation fields emitted by FrontComposer tracing are sanitized
and bounded at 128 characters. A trailing tilde (`~`) marks a truncated value.
