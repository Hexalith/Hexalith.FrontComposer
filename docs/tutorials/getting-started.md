---
title: "Build your first FrontComposer projection"
description: "Create a small projection and inspect generated FrontComposer output."
genre: tutorial
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-11-20
uid: frontcomposer.tutorials.getting-started
slug: tutorials/getting-started/
---

# Build your first FrontComposer projection

Use the included Counter domain for your first run. It has a working Blazor host and a domain project configured with Contracts, Contracts.UI, Shell, and the SourceTools analyzer.

## Build the sample

Install Git and the .NET 11 SDK selected in `global.json` (11.0.100 with latest-patch roll-forward). From a shell, clone the repository and initialize its direct dependencies at their recorded revisions.

```sh
git clone --no-recurse-submodules https://github.com/Hexalith/Hexalith.FrontComposer.git
cd Hexalith.FrontComposer
git -c submodule.recurse=false submodule update --init
dotnet --version
dotnet build Hexalith.FrontComposer.slnx -c Release
```

## Render a projection

Start the sample from the repository root. Open the local Counter page, enter an amount in **Batch Increment**, and submit the form. The page shows the command acknowledgement and confirmation. Counter uses a simulated service for this feedback. A production domain host registers the EventStore clients through `AddHexalithEventStore`. See the [adopter bootstrap proof](../how-to/adopter-bootstrap-proof.md).

```sh
dotnet run --project samples/Counter/Counter.Web -c Release --no-build --no-launch-profile --environment ASPNETCORE_ENVIRONMENT=Development --urls http://127.0.0.1:5201
```

[Open the local Counter page](http://127.0.0.1:5201/counter)

## Inspect the generated output

In another shell at the repository root, run the CLI against the domain project.

```sh
dotnet run --project src/Hexalith.FrontComposer.Cli -c Release --no-build -- inspect --project samples/Counter/Counter.Domain/Counter.Domain.csproj --configuration Release --framework net11.0 --build --format json
```

The JSON report lists both `CounterProjection` and `BatchIncrementCommand` in the generated file inventory. To try your own projection, add the following type to the Counter.Domain project, then run the same inspection command again. Look for `OrderSummaryProjection` in `generatedFiles`.

```csharp compile
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Acme.Orders;

[Projection]
public sealed partial class OrderSummaryProjection
{
    public string OrderId { get; init; } = "";

    public DateTimeOffset UpdatedAt { get; init; }
}
```

After the first build, keep generated files read-only. Edit the domain type, rebuild, and use the generated-output reference when an IDE or CLI report points to an `obj/.../generated/HexalithFrontComposer` path.
