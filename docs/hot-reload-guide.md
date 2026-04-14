# Hot Reload Guide

> **Version scope:** Validated against **.NET 10 SDK**. Verify on **.NET 11+** — the `.g.cs` hot reload limitation described below may be resolved in a future Roslyn/Blazor release.

This guide documents the developer inner-loop behavior for FrontComposer-generated code: what kinds of domain-model changes are picked up by `dotnet watch`, which changes require a full restart, and the measurement template a human uses to record end-to-end latency (Story 1.8 AC1).

## 1. Fundamental Limitation: `.g.cs` and Blazor Hot Reload

Blazor hot reload does **not** pick up changes to source-generator output (`.g.cs` files). This is a fundamental .NET / Roslyn limitation, **not** a FrontComposer bug.

The developer workflow is therefore a fast **incremental rebuild** cycle (not "true" hot reload):

1. Developer modifies a domain type (e.g., adds a property to `CounterProjection`).
2. `dotnet watch` detects the `.cs` file change and triggers an incremental rebuild.
3. The FrontComposer source generator runs (Parse → Transform → Emit).
4. New `.g.cs` files are written to `obj/`.
5. `dotnet watch` detects the rebuilt assembly and restarts / reloads the Blazor Server application.
6. The browser reflects the change after a short reconnect.

The < 2 s target (NFR10) covers this **full** cycle: file save → incremental rebuild → browser update.

### Fluxor State Is NOT Preserved

`dotnet watch` restarts the Blazor Server process, which tears down the Blazor DI container and with it the Fluxor store. After every rebuild the counter state resets to zero and any in-memory state is lost. This is expected behavior; document it to adopters so they don't treat it as a regression.

## 2. Change-Type Support Matrix

| # | Change Type                                                       | Hot Reload?                        | Notes                                                                                                                                                                                    |
|---|-------------------------------------------------------------------|------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Add/remove property on `[Projection]` type                        | Yes (via `dotnet watch` rebuild)   | Incremental generator re-runs. Parse stage target < 500 ms (NFR8).                                                                                                                       |
| 2 | Change `[Display(Name = ...)]` attribute                          | Yes (via `dotnet watch` rebuild)   | Label resolution chain updates on next rebuild.                                                                                                                                          |
| 3 | Change `[BoundedContext]` attribute argument (single declaration) | Yes (via `dotnet watch` rebuild)   | Nav grouping / bounded-context label refreshes after rebuild.                                                                                                                            |
| 4 | Change `[BoundedContext]` on a **separate partial declaration**   | **Unverified — speculative, needs investigation** | If `[Projection]` and `[BoundedContext]` live on different partial declarations of the same type, the change may not retrigger re-generation. Needs investigation (see `deferred-work.md`). |
| 5 | Change property type (e.g., `int` → `long`)                       | **No — requires full restart**     | Type change breaks Fluxor state deserialization; analyzer HFC1010 will (when implemented) surface this.                                                                                 |
| 6 | Add / remove nullability (`int` → `int?`)                         | **No — requires full restart**     | Nullable wrapper changes field type mapping in generated code.                                                                                                                          |
| 7 | Change generic type parameters                                    | **No — requires full restart**     | Roslyn limitation on generic type hot reload.                                                                                                                                            |
| 8 | Add new `[Projection]` attribute to previously unannotated type   | Yes (via `dotnet watch` rebuild)   | New type enters the `ForAttributeWithMetadataName` pipeline on next run.                                                                                                                |
| 9 | Modify non-generated `.razor` files (hand-written)                | **Yes — true Blazor hot reload**   | No rebuild required for hand-written Razor; only generated `.g.cs` is subject to the limitation in §1.                                                                                   |

### Diagnostic Reservation: HFC1010

HFC1010 is **reserved** (not implemented). It is declared as:

- A **comment** in `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` (no `DiagnosticDescriptor` field).
- A **table row** in `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`.

Its intended purpose is to warn a developer when they make a change type that requires a full restart (e.g., rows 5–7 above). Implementation is deferred because **source generators observe current state, not diffs**. Detecting "what changed from the previous compilation" requires an analyzer that maintains its own state or compares against a baseline snapshot — out of scope for Story 1.8.

## 3. Human Validation — Measurement Template (AC1, Task 1)

This section is a **human-executed** checklist. The dev agent cannot reliably launch `dotnet watch` and drive a browser; the measurements below must be captured by a developer on their workstation.

> If you observe an end-to-end latency above **2 seconds**, add a row to `_bmad-output/implementation-artifacts/deferred-work.md` recording the measured value, environment, and proposed revised threshold.

### 3.1 Environment

Fill in before running the test:

| Field                       | Value                                                       |
|-----------------------------|-------------------------------------------------------------|
| OS (name + version)         | `TODO: human validation`                                    |
| Machine (CPU / RAM)         | `TODO: human validation`                                    |
| .NET SDK (`dotnet --version`) | `TODO: human validation`                                  |
| Branch / commit             | `TODO: human validation`                                    |
| Sample app                  | `samples/Counter/Counter.Web` (Blazor Server, `dotnet watch`) |

### 3.2 Runs

For each run, record the wall-clock time between the moment the file is saved in the editor and the moment the browser shows the updated DataGrid / label.

| Scenario                                                     | File touched                                   | Measured latency | Notes (observations, anomalies) |
|--------------------------------------------------------------|------------------------------------------------|------------------|---------------------------------|
| 1.1 Add a new property to `CounterProjection`                | `samples/Counter/Counter.Domain/CounterProjection.cs` | `TODO: human validation` | DataGrid column should appear. |
| 1.2 Modify a `[Display(Name = "...")]` attribute             | `samples/Counter/Counter.Domain/CounterProjection.cs` | `TODO: human validation` | Column header label should change. |
| 1.3 Repeated measurement of 1.1 (n = 5, record each + median) | Same as 1.1                                    | `TODO: human validation` | Used as the baseline for NFR10. |
| 1.4 Fluxor store re-initialization after rebuild             | Any file under `samples/Counter/Counter.Domain` | `TODO: human validation` | Counter must start at zero; store must be functional. Note that Fluxor state is **not** preserved across `dotnet watch` restarts (expected). |

### 3.3 Deviation Log

If the median latency for scenario 1.3 exceeds 2 s, copy the row below into `_bmad-output/implementation-artifacts/deferred-work.md`:

```
- Story 1.8 / NFR10 deviation
  - measured-median-ms: <value>
  - samples: <count>
  - environment: <OS, CPU/RAM, .NET SDK>
  - proposed-revised-threshold-ms: <value + rationale>
  - branch/commit: <ref>
```

## 4. How the Incremental Generator Is Validated Automatically

Human validation (§3) measures the **full** cycle. Automated validation lives in the test project:

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Performance/ParseStagePerformanceTests.cs` — parse-stage budget (< 500 ms, NFR8) for ≥ 20 projection types.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/IncrementalRebuildBenchmarkTests.cs` — two-pass incremental protocol (seed cache, then add one property and measure the delta rebuild). Asserts the incremental rebuild delta is < 500 ms.

The benchmark runs in CI as an **advisory** job (`continue-on-error: true`) to establish a regression baseline without blocking merges during Epic 1.

## 5. References

- Story spec: `_bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md`
- Fluent UI contingency plan: `docs/fluent-ui-v5-contingency.md`
- PRD: FR70, NFR8, NFR10
- Architecture: ADR-003 (Fluent UI contingency), ADR-004 (IR pipeline)
