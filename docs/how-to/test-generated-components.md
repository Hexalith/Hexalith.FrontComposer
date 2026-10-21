---
title: "Test generated components"
description: "Use Hexalith.FrontComposer.Testing to render generated views and customization overrides without a running app host."
genre: how-to
audience: adopter
ownerStory: 13-7-i-ux-f-provide-reusable-ux-assertions-for-adopters
status: published
reviewed: 2026-10-20
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
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
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
        var scope = ProjectionStateScopeOwnership.ResolveCurrentScope(
            Services.GetRequiredService<IFrontComposerTenantContextAccessor>(), null, "test")
            ?? throw new InvalidOperationException("Synthetic test scope is unavailable.");
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "counter-initial-read-001",
            [new CounterProjection { Id = "counter-1", Count = 12 }])
        { ScopeTenantId = scope.TenantId, ScopeUserId = scope.UserId });

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

The fake providers keep evidence per test context. They do not open EventStore, SignalR, DAPR, browser storage, or a running app host. Use `TestProjectionPageLoader.SucceedWith(...)` or `TestProjectionPageLoader.NotModified(...)` for server-side virtualization paths, `TestQueryService.SucceedWith<T>(...)` or `TestQueryService.NotModifiedWith<T>(...)` for query seams, and `TestFaultEvidenceRecorder` to record deterministic drop, delay, partial delivery, reorder, and reconnect-nudge evidence. Evidence formatting redacts configured tenant/user values and token, secret, password, authorization, API-key, cookie, private-key, connection-string, JWT, tenant-payload, stack-trace, backend-metadata, email, phone, address, person, or full-name keyed values before truncation and assertion output. Redaction matches key names, not arbitrary values; keep personal data out of neutrally named test payload fields.

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

For client or mapped rejection summaries, pass a measured `UxValidationSummarySnapshot` containing the summary description and error count independently of its accessible label. Supply all observed `UxFieldSnapshot` values, invalid-only summary-link targets, complete ordered `UxFieldDeclaration` / `UxFieldGroupDeclaration` metadata, and `UxFieldIdentity` values captured on the prior render. The helper compares measured group membership, labels, name and description with those declarations; it also rejects regenerated control IDs and changed error IDs when an error exists in both observations. A valid field has an empty `ErrorId`, so first-error insertion and removal after correction remain valid. Extra accessible-name context such as `Name (required)` is valid when it retains the visible `Name` label. This label/name comparison ignores case and collapses whitespace (for example `First\nName` matches `first name`); declared visible-label identity stays exact. Compare useful input with `AssertInputPreserved`. `UxStateAssertions.AssertState` requires row-specific caller-measured `UxSurfaceFacts` and covers SS-01..49; its canonical `State` names correspond to the matrix cases (for example `Rejected`, `AuthorizationDenied`, `Offline`, `FilterNoMatches`, `DegradedExhausted`). `AssertBlockedSubmit` additionally verifies AM-20, no dispatch/queue, and retained attempted-submit focus. Ordinary silent states SS-31/33/36/39/40/43/44/45/46/47, including the SS-46/47 restored-focus rows, accept explicit `aria-live="off"` for roles other than `alert`/`alertdialog`, including overriding implicit status/log speech where the row permits that role; focus-only summaries/headings SS-05/06/28/37/38 still require absent live/atomic attributes. SS-19/21 require measured `FailureVisible = false`; SS-25/36/37/38 require no dispatch, queue or lifecycle entry (`null` or `Idle`) for the attempted command.

`UxAnnouncementAssertions.AssertChannel` takes measured role/name/focus/live attributes. A polite status may have an empty accessible name; assert its canonical message content separately with `AssertSequence`. For AM-28 also supply the number of speech owners and any inherited result-count speech; one atomic polite owner and zero inherited speakers are required. `role="status"` supplies implicit polite/atomic semantics unless explicit attributes override them. Focused summaries and headings must omit both live and atomic attributes. Measure the computed accessibility tree of the focusable control: Fluent UI v5 host ARIA attributes do not reach the shadow input, so reading the host in bUnit is not browser accessibility evidence. Resolve accessible roles and names before using optional `data-testid` locators.

Capture an overlay/guard invoker with `UxFocusAssertions.CaptureOrigin` before activation, retaining the same direct element object for reference comparison. After close, pass the current origin's connected/enabled/visible status to `AssertRestored`; a removed origin uses `null`. FM-04/05 require a current route `h1` fallback; FM-11 permits a named form heading. `AssertUnobscured` accepts measured CSS rectangles for the full target and painted focus indicator, every active viewport/scrollport, and app-owned sticky/overlay/message paint bounds. Include pseudo-elements, outlines and shadows in the focus indicator, and overflowing descendants in each occluder even when the wrapper has zero size. Zero-size occluders paint nothing and are ignored; negative dimensions fail.

Record AE-01..09 with `new TestAccessibilityEvidenceRecorder(host.Options)`. Pass `AccessibilityEvidenceContext(surfaceNumber, stateNumber, theme)` to each `Record` call; these positive ordinals reference your surface/journey inventory without storing routes or identifiers. `UxTheme.Light` and `UxTheme.Dark` distinguish contrast packets captured at the same instant. `AccessibilityMeasurements` retains row-specific setup plus observed content loss, clipping, overlap, control truncation, forced-color cue preservation, animation suppression, immediate focus, silent expiry, keyboard and axe outcomes, WCAG 2.2 AA findings and measured geometry. `ViewportWidth` records effective CSS layout width after browser zoom: AE-01/02 require a positive width of at most 320 CSS px, and AE-02 also requires at least 400% zoom. A `Passed` AE-06 packet must meet the shared 3:1 contrast floor; the caller must still assess the 4.5:1 requirement for ordinary text. The recorder rejects an incomplete row, a row measured under another row's setup or theme, and a `Passed` packet whose measured outcomes or AE-04/AE-05 geometry failed; it retains failed findings without changing them. Target exceptions use `AccessibilityTargetEvidence` with ordinal target/equivalent references, typed rationale and keyboard keys. `UxAccessibilityAssertions.AssertTargetSize` verifies minimum dimensions or an evidenced exception. For Spacing, `Neighbors = []` records a measured empty inventory; `null` is unmeasured and fails `AssertTargetSize` and Passed AE-04 packets. Spacing is checked per recorded target: a measured neighbor that intersects the target's 24 px spacing circle, or an undersized neighbor whose own 24 px circle intersects it, also fails those checks. The recorder has no free-text or raw AX-snapshot field, uses the shared redactor, and respects record/payload bounds; increase the diagnostic payload limit for a full packet.


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

`AssertChannel`, `AssertLinkedSummary`, `AssertState` and `AssertTargetSize` failures name failed checks without printing measured values. Recorder missing-fact, ordinal, setup and contradicted-outcome failures name required or failed facts; non-finite and context failures describe the failed contract. Sequence diagnostics show the mismatch index, catalog row/channel labels, differing member names and counts, omitting observed copy and opaque identifiers. `AssertUnobscured` includes bounded redacted geometry; other helpers, including the `AssertBlockedSubmit` focus and AM-20 count checks, describe the failed contract.

The .NET helpers assert caller measurements; browser acquisition and required manual assistive checks remain separate. This implementation does not close OI-16, G-4, FLUENT-APP-1, or Product approval.

## Support-safe assertions

`GeneratedProjectionAssertions.AssertCellValues` checks raw DOM values and may print them on failure. Use only deterministic synthetic values in those DOM tests; do not copy their failure output into support evidence. Use the existing redacted command/evidence and measured UX assertions for support packets: they report bounded failed contracts without raw values, application selectors or exception details. Include sensitive sentinels in redaction tests and assert they are absent from formatted failure output. Keep the measured timing, focus and accessibility guidance above; these helpers do not replace browser acquisition.
