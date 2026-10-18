---
title: "Test generated components"
description: "Use Hexalith.FrontComposer.Testing to render generated views and customization overrides without a running app host."
genre: how-to
audience: adopter
ownerStory: 10-1-adopter-test-host-and-component-testing-utilities
status: published
reviewed: 2026-06-05
uid: frontcomposer.how-to.test-generated-components
slug: how-to/test-generated-components/
---

# Test generated components

Configure failure paths through `host.CommandService`, request-sensitive query/page results through callback overloads, and policy decisions through `host.AuthorizationEvaluator`. Unknown policies fail closed. `host.FaultRecorder` records evidence only and does not emulate a transport.

For setup-time Fluxor initialization, await `AddFrontComposerTestHostAsync(...)` with `StoreInitializationMode.DuringHostSetup`; synchronous composition intentionally rejects that mode.

Add `Hexalith.FrontComposer.Testing` to an xUnit v3 + bUnit test project. The package configures FrontComposer Shell services, in-memory storage, deterministic user context, fake command/query/page-loader providers, loose JS interop, and assertion helpers.

## Inherit from the test base

```csharp no-compile reason="Generated Counter sample types are produced by the sample domain project, not the standalone docs snippet harness."
using Counter.Domain;
using Bunit;
using Fluxor;
using Hexalith.FrontComposer.Testing;
using Hexalith.FrontComposer.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class CounterViewTests : FrontComposerTestBase
{
    public CounterViewTests()
        : base(options =>
        {
            options.TestTenantId = "sample-tenant";
            options.TestUserId = "sample-user";
            options.DomainAssemblies.Add(typeof(CounterDomain).Assembly);
        })
    {
        Services.AddHexalithDomain<CounterDomain>();
    }

    [Fact]
    public async Task CounterProjectionView_LoadedState_RendersExpectedCells()
    {
        await InitializeStoreAsync();

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [new CounterProjection { Id = "counter-1", Count = 12 }]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        cut.WaitForAssertion(() =>
        {
            GeneratedProjectionAssertions.AssertDataGridEnvelope(cut, "Counter");
            GeneratedProjectionAssertions.AssertCellValues(cut, "counter-1", "12");
        });
    }
}
```

Generated projection reducers accept a direct `LoadedAction` or `LoadFailedAction` only before any load request has been dispatched, as in the example above. **Breaking change:** once a request exists, a mismatched correlation is dropped, including push-style direct `LoadedAction` and `LoadFailedAction` values. After `ScopeChangedAction`, stamp `OriginScopeGeneration` from `state.ScopeGeneration`. Stamp `ScopeTenantId` and `ScopeUserId` from the resolved tenant context (`ProjectionStateScopeOwnership.ResolveCurrentScope`), the same source generated Retry uses. A scope change clears tenant and user on projection state, so those two fields cannot come from that state. Then dispatch a new `LoadRequestedAction` before `LoadedAction` or `LoadFailedAction`. Every result must carry that request's correlation. A result without a matching request is discarded so a late response from the prior tenant cannot refill the view. The adopter owns the request and its result; a scope change does not start a load automatically.

Non-grid Retry, and grid Retry when the only failure is `state.Error`, dispatches `{Type}LoadRequestedAction` with `OperatorInitiated: true`. No framework effect fetches on that action. An adopter whose projection can fail must answer the operator-initiated request: reload, then dispatch `Loaded` or `LoadFailed` with the same correlation, `OriginScopeGeneration` from `state.ScopeGeneration`, and tenant and user from the resolved tenant context.

## Compose the host directly

```csharp compile
using Bunit;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FrontComposer.Snippets;

public sealed class CommandDispatchTests
{
    public static async Task CommandServiceDispatchCapturesRedactedEvidence()
    {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);

        ICommandService commandService = context.Services.GetRequiredService<ICommandService>();
        await commandService.DispatchAsync(
            new ConfigureCounterCommand { Count = 7 },
            CancellationToken.None).ConfigureAwait(false);

        CommandDispatchEvidence evidence = host.CommandService.Evidence.Single();
        CommandEvidenceAssertions.AssertLifecycleContains(evidence, CommandLifecycleState.Confirmed);
        CommandEvidenceAssertions.AssertRedacted(evidence, "test-tenant", "test-user");
    }

    private sealed class ConfigureCounterCommand
    {
        public int Count { get; init; }
    }
}
```

The fake providers keep evidence per test context. They do not open EventStore, SignalR, DAPR, browser storage, or a running app host. Use `TestProjectionPageLoader.SucceedWith(...)` or `TestProjectionPageLoader.NotModified(...)` for server-side virtualization paths, `TestQueryService.SucceedWith<T>(...)` or `TestQueryService.NotModifiedWith<T>(...)` for query seams, and `TestFaultEvidenceRecorder` to record deterministic drop, delay, partial delivery, reorder, and reconnect-nudge evidence. Evidence formatting redacts configured tenant/user values and token, secret, or password keyed values before assertion output.

## Assert failure UX with canonical identities

Use the package clock through the existing options seam. The following example observes a rejection that supersedes a pending accepted message. Observe actual events from the component or coordinator; retain every event so duplicate, stale, and silent-behavior drift remains visible.

```csharp compile
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Testing;

namespace FrontComposer.Snippets;

public static class RejectionAnnouncementExample
{
    public static void AssertRejectedOperation()
    {
        TestTimeProvider clock = new();
        using SurfaceAnnouncementCoordinator coordinator = new(clock);
        UxAnnouncement rejected = UxAnnouncementCatalog.Expected("AM-14", "operation", "Rejected");
        List<UxAnnouncement> observed = [];
        using IDisposable subscription = coordinator.Subscribe("command", text =>
            observed.Add(rejected with { Copy = text }));
        coordinator.Announce("command", "lifecycle:operation", "Acknowledged",
            UxAnnouncementCatalog.Expected("AM-11", "operation", "Acknowledged").Copy);
        coordinator.Announce("command", "lifecycle:operation", "Rejected", rejected.Copy, terminal: true);
        UxAnnouncementAssertions.AssertImmediateCancellation(clock, () => observed, rejected);
    }
}
```

Set `options.TimeProvider = clock` when composing `AddFrontComposerTestHost`. Advance fake time on the bUnit dispatcher for UI transitions. Use `UxAnnouncementAssertions.AssertCoalescingBoundary` immediately after the last eligible change to test 249/250 ms; use `UxTimingAssertions.AssertBoundary` from the operation start for 1,999/2,000 ms SlowQuery and 119,999/120,000 ms exhaustion. `UxTiming` supplies the remaining canonical retry, polling, notice and expiry budgets.

For client or mapped rejection summaries, pass a measured `UxValidationSummarySnapshot` containing the summary description and error count independently of its accessible label. Supply all observed `UxFieldSnapshot` values, invalid-only summary-link targets, complete ordered `UxFieldDeclaration` / `UxFieldGroupDeclaration` metadata, and `UxFieldIdentity` values captured on the prior render. The helper compares measured group membership, labels, name and description with those declarations; it also rejects regenerated control/error IDs. Extra accessible-name context such as `Name (required)` is valid when it retains the visible `Name` label. Compare useful input with `AssertInputPreserved`. `UxStateAssertions.AssertState` requires row-specific caller-measured `UxSurfaceFacts` and covers SS-01..49; its canonical `State` names correspond to the matrix cases (for example `Rejected`, `AuthorizationDenied`, `Offline`, `FilterNoMatches`, `DegradedExhausted`). `AssertBlockedSubmit` additionally verifies AM-20, no dispatch/queue, and retained attempted-submit focus.

`UxAnnouncementAssertions.AssertChannel` takes measured role/name/focus/live attributes. A polite status may have an empty accessible name; assert its canonical message content separately with `AssertSequence`. For AM-28 also supply the number of speech owners and any inherited result-count speech; one atomic polite owner and zero inherited speakers are required. Focused summaries and headings must have no live attributes. Resolve accessible roles and names before using optional `data-testid` locators.

Capture an overlay/guard invoker with `UxFocusAssertions.CaptureOrigin` before activation, retaining the same direct element object for reference comparison. After close, pass the current origin's connected/enabled/visible status to `AssertRestored`; a removed origin uses `null`. FM-04/05 require a current route `h1` fallback; FM-11 permits a named form heading. `AssertUnobscured` accepts measured CSS rectangles for the full target and painted focus indicator, every active viewport/scrollport, and app-owned sticky/overlay/message rectangles.

Record AE-01..09 with `new TestAccessibilityEvidenceRecorder(host.Options)`. Pass `AccessibilityEvidenceContext(surfaceNumber, stateNumber, theme)` to each `Record` call; these positive ordinals reference your surface/journey inventory without storing routes or identifiers. `UxTheme.Light` and `UxTheme.Dark` distinguish contrast packets captured at the same instant. `AccessibilityMeasurements` retains row-specific setup plus observed content loss, clipping, overlap, control truncation, forced-color cue preservation, animation suppression, immediate focus, silent expiry, keyboard and axe outcomes, WCAG 2.2 AA findings and measured geometry. The recorder rejects an incomplete row and retains failed findings without changing them. Target exceptions use `AccessibilityTargetEvidence` with ordinal target/equivalent references, typed rationale and keyboard keys. `UxAccessibilityAssertions.AssertTargetSize` verifies minimum dimensions or an evidenced exception. The recorder has no free-text or raw AX-snapshot field, uses the shared redactor, and respects record/payload bounds; increase the diagnostic payload limit for a full packet.


```csharp compile
using Hexalith.FrontComposer.Testing;

namespace FrontComposer.Snippets;

public static class MeasuredValidationExample
{
    public static void AssertSummary(UxValidationSummarySnapshot measuredSummary,
        IReadOnlyList<UxFieldSnapshot> measuredFields, IReadOnlyList<string> measuredLinks,
        IReadOnlyList<UxFieldIdentity> priorRenderTargets)
    {
        UxFieldDeclaration[] declaredFields = [new("Name", "Name", "command")];
        UxFieldGroupDeclaration[] declaredGroups =
            [new("command", "Command fields", "Command fields", "Editable command details", ["Name"])];
        UxValidationAssertions.AssertLinkedSummary(measuredSummary, "Validation failed", measuredFields,
            measuredLinks, declaredFields, declaredGroups, priorRenderTargets);
    }

    public static AccessibilityEvidence RecordContrast(FrontComposerTestOptions options,
        int surfaceNumber, int stateNumber, UxTheme activeTheme, double measuredRatio, bool passed)
        => new TestAccessibilityEvidenceRecorder(options).Record(AccessibilityEvidenceRow.AE06,
            new(surfaceNumber, stateNumber, activeTheme), new() { Passed = passed, ContrastRatio = measuredRatio });
}
```

The .NET helpers assert caller measurements; browser acquisition and required manual assistive checks remain separate. This implementation does not close OI-16, G-4, FLUENT-APP-1, or Product approval.
