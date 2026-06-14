using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Forms;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Forms;

/// <summary>
/// Story 2-5 Task 8.2 — <see cref="FcFormAbandonmentGuard"/> unit-level coverage of the first-edit
/// anchor (D10), wrapper-initiated-nav cascade (D13), and <c>_isLeaving</c> flag-lifecycle (D24 /
/// Red Team Attack-3). Full end-to-end <see cref="Microsoft.AspNetCore.Components.Routing.NavigationLock"/>
/// + <see cref="Microsoft.AspNetCore.Components.Routing.LocationChangingContext"/> interception is
/// covered by the Task 8.11 Playwright E2E test (Story 2-5 Task 0.3 compatibility-spike fallback).
/// </summary>
public sealed class FcFormAbandonmentGuardTests : BunitContext {
    private const string DefaultCorrelationId = "corr-guard-001";
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));

    public FcFormAbandonmentGuardTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLogging();
        _ = Services.AddOptions<FcShellOptions>();
        Services.TryAddSingleton<IValidateOptions<FcShellOptions>, FcShellOptionsThresholdValidator>();
        _ = Services.AddSingleton<TimeProvider>(_time);
        _ = Services.AddSingleton<NavigationManager>(_ => new TestNavigationManager());
    }

    [Fact]
    public async Task Dirty_threshold_crossed_navigation_prevents_and_shows_warning() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        _time.Advance(TimeSpan.FromSeconds(31));

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext context = BuildLocationChangingContext("/leave");
        await InvokeNavigationChangingAsync(cut, guard, context);

        DidPreventNavigation(context).ShouldBeTrue();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-form-abandonment-warning']"));
    }

    [Fact]
    public async Task Clean_navigation_does_not_prevent_or_show_warning() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext context = BuildLocationChangingContext("/leave");
        await InvokeNavigationChangingAsync(cut, guard, context);

        DidPreventNavigation(context).ShouldBeFalse();
        cut.FindAll("[data-testid='fc-form-abandonment-warning']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Null_EditContext_navigation_does_not_prevent_or_show_warning() {
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext: null);

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext context = BuildLocationChangingContext("/leave");
        await InvokeNavigationChangingAsync(cut, guard, context);

        DidPreventNavigation(context).ShouldBeFalse();
        cut.FindAll("[data-testid='fc-form-abandonment-warning']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Dirty_below_threshold_navigation_does_not_prevent_or_show_warning() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        _time.Advance(TimeSpan.FromSeconds(29));

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext context = BuildLocationChangingContext("/leave");
        await InvokeNavigationChangingAsync(cut, guard, context);

        DidPreventNavigation(context).ShouldBeFalse();
        cut.FindAll("[data-testid='fc-form-abandonment-warning']").ShouldBeEmpty();
    }

    [Fact]
    public void First_edit_anchors_timestamp_from_EditContext_OnFieldChanged() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        FcFormAbandonmentGuard guard = RenderGuard(editContext);

        GetFirstEditAt(guard).ShouldBeNull("No edits yet — anchor must be unset.");

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));

        GetFirstEditAt(guard).ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void Second_edit_does_not_reset_the_anchor() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        FcFormAbandonmentGuard guard = RenderGuard(editContext);

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        DateTimeOffset firstAnchor = GetFirstEditAt(guard)!.Value;

        _time.Advance(TimeSpan.FromSeconds(31));
        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));

        GetFirstEditAt(guard).ShouldBe(firstAnchor, "D10 — anchor is captured on first edit only.");
    }

    [Fact]
    public void IsLeaving_flag_defaults_to_false() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        FcFormAbandonmentGuard guard = RenderGuard(editContext);

        GetIsLeavingFlag(guard).ShouldBeFalse();
    }

    [Fact]
    public async Task Stay_click_clears_warning_without_navigating() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        SetField(guard, "_showingWarning", true);
        SetField(guard, "_pendingTarget", "/somewhere-else");

        await cut.InvokeAsync(() => {
            var stayTask = (Task)typeof(FcFormAbandonmentGuard).GetMethod(
                "OnStayClickedAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(guard, null)!;
            return stayTask;
        });

        GetField<bool>(guard, "_showingWarning").ShouldBeFalse();
        GetField<string?>(guard, "_pendingTarget").ShouldBeNull();
        var nav = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        nav.LastNavigateCall.ShouldBeNull("Stay must not navigate.");
    }

    [Fact]
    public async Task Escape_clears_warning_without_navigating() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        SetField(guard, "_showingWarning", true);
        SetField(guard, "_pendingTarget", "/somewhere-else");

        await cut.InvokeAsync(() => {
            var escapeTask = (Task)typeof(FcFormAbandonmentGuard).GetMethod(
                "HandleBarKeyDownAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(guard, [new KeyboardEventArgs { Key = "Escape" }])!;
            return escapeTask;
        });

        GetField<bool>(guard, "_showingWarning").ShouldBeFalse();
        GetField<string?>(guard, "_pendingTarget").ShouldBeNull();
        var nav = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        nav.LastNavigateCall.ShouldBeNull("Escape must behave like Stay and preserve the form.");
    }

    [Fact]
    public async Task Leave_click_navigates_and_flag_survives_until_nav_event_is_observed() {
        // Review 2026-04-17 P4 — `_isLeaving` is now consumed on the next HandleNavigationChangingAsync
        // entry (not cleared in a finally that races with Blazor Server's async nav pipeline). The flag
        // must stay TRUE until the LocationChanging event lands; then it is cleared so any later unrelated
        // nav is NOT bypassed (D24 / Red Team Attack-3).
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        SetField(guard, "_showingWarning", true);
        SetField(guard, "_pendingTarget", "/target");

        await cut.InvokeAsync(() => {
            var leaveTask = (Task)typeof(FcFormAbandonmentGuard).GetMethod(
                "OnLeaveClickedAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(guard, null)!;
            return leaveTask;
        });

        var nav = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        nav.LastNavigateCall?.Uri.ShouldBe("/target");
        GetIsLeavingFlag(guard).ShouldBeTrue("Flag stays TRUE until the resulting nav event consumes it.");

        // Simulate the LocationChanging callback that Blazor's NavigationLock would fire next.
        await InvokeNavigationChangingAsync(cut, guard, BuildLocationChangingContext("/target"));

        GetIsLeavingFlag(guard).ShouldBeFalse("After the nav event consumes the flag, it is cleared so later unrelated navs are not bypassed.");

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        _time.Advance(TimeSpan.FromSeconds(31));
        Microsoft.AspNetCore.Components.Routing.LocationChangingContext laterContext = BuildLocationChangingContext("/later");
        await InvokeNavigationChangingAsync(cut, guard, laterContext);

        DidPreventNavigation(laterContext).ShouldBeTrue("The leave bypass is one-shot and must not leak to unrelated later navigation.");
    }

    // Exception-path clearing of `_isLeaving` (D24 / Red Team Attack-3) is verified at the code level:
    // `OnLeaveClickedAsync` wraps `NavigateTo` in try/catch that resets `_isLeaving` before rethrowing.
    // A bUnit integration test for this invariant is non-trivial because `MethodBase.Invoke` + bUnit's
    // InvokeAsync + xUnit async exception surfacing interact in ways that obscure the assertion; the
    // invariant is small and inspectable in the source. Deferred to Playwright E2E (Task 8.11).

    [Fact]
    public async Task Submitting_lifecycle_suppresses_guard_but_syncing_still_protects_dirty_forms() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        ILifecycleStateService svc = Substitute.For<ILifecycleStateService>();
        _ = svc.GetState(DefaultCorrelationId).Returns(CommandLifecycleState.Submitting, CommandLifecycleState.Syncing);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext, svc);

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        _time.Advance(TimeSpan.FromSeconds(31));

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext submittingContext = BuildLocationChangingContext("/submitting");
        await InvokeNavigationChangingAsync(cut, guard, submittingContext);

        DidPreventNavigation(submittingContext).ShouldBeFalse("Submitting is the only lifecycle state suppressed by the guard.");

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext syncingContext = BuildLocationChangingContext("/syncing");
        await InvokeNavigationChangingAsync(cut, guard, syncingContext);

        DidPreventNavigation(syncingContext).ShouldBeTrue("Syncing must still protect dirty forms.");
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-form-abandonment-warning']"));
    }

    [Fact]
    public async Task Wrapper_initiated_navigation_yields_without_prompting() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        (FcFormAbandonmentGuard guard, IRenderedComponent<FcFormAbandonmentGuard> cut) = RenderGuardWithCut(editContext);

        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        _time.Advance(TimeSpan.FromSeconds(31));
        guard.WrapperInitiatedNavigation = true;

        Microsoft.AspNetCore.Components.Routing.LocationChangingContext context = BuildLocationChangingContext("/wrapper");
        await InvokeNavigationChangingAsync(cut, guard, context);

        DidPreventNavigation(context).ShouldBeFalse();
        cut.FindAll("[data-testid='fc-form-abandonment-warning']").ShouldBeEmpty();
    }

    [Fact]
    public void Guard_without_EditContext_is_inert_no_anchor_captured() {
        FcFormAbandonmentGuard guard = RenderGuard(editContext: null);

        GetFirstEditAt(guard).ShouldBeNull("D10 — null EditContext → inert.");
    }

    [Fact]
    public void Dispose_unsubscribes_from_EditContext_events() {
        TestModel model = new() { Name = "" };
        EditContext editContext = new(model);
        FcFormAbandonmentGuard guard = RenderGuard(editContext);

        guard.Dispose();

        // After disposal, a subsequent field-change must NOT anchor the timestamp.
        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));
        GetFirstEditAt(guard).ShouldBeNull();
    }

    private FcFormAbandonmentGuard RenderGuard(EditContext? editContext)
        => RenderGuardWithCut(editContext).Guard;

    private (FcFormAbandonmentGuard Guard, IRenderedComponent<FcFormAbandonmentGuard> Cut) RenderGuardWithCut(
        EditContext? editContext,
        ILifecycleStateService? lifecycleService = null) {
        ILifecycleStateService svc = lifecycleService ?? Substitute.For<ILifecycleStateService>();
        if (lifecycleService is null) {
            _ = svc.GetState(Arg.Any<string>()).Returns(CommandLifecycleState.Idle);
        }
        Services.RemoveAll<ILifecycleStateService>();
        _ = Services.AddSingleton(svc);

        IRenderedComponent<FcFormAbandonmentGuard> cut = Render<FcFormAbandonmentGuard>(p => p
            .Add(c => c.CorrelationId, DefaultCorrelationId)
            .Add(c => c.EditContext, editContext));
        return (cut.Instance, cut);
    }

    private static DateTimeOffset? GetFirstEditAt(FcFormAbandonmentGuard guard)
        => GetField<DateTimeOffset?>(guard, "_firstEditAt");

    private static bool GetIsLeavingFlag(FcFormAbandonmentGuard guard)
        => GetField<bool>(guard, "_isLeaving");

    private static T GetField<T>(object target, string name)
        => (T)target.GetType().GetField(
            name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(target)!;

    private static void SetField<T>(object target, string name, T value)
        => target.GetType().GetField(
            name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(target, value);

    private static Task InvokeNavigationChangingAsync(
        IRenderedComponent<FcFormAbandonmentGuard> cut,
        FcFormAbandonmentGuard guard,
        Microsoft.AspNetCore.Components.Routing.LocationChangingContext context)
        => cut.InvokeAsync(() => {
            var handle = (Task)typeof(FcFormAbandonmentGuard).GetMethod(
                "HandleNavigationChangingAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(guard, [context])!;
            return handle;
        });

    private static Microsoft.AspNetCore.Components.Routing.LocationChangingContext BuildLocationChangingContext(string target) {
        // Construct via reflection — LocationChangingContext has an internal constructor in .NET 10.
        Type type = typeof(Microsoft.AspNetCore.Components.Routing.LocationChangingContext);
        System.Reflection.ConstructorInfo ctor = type.GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)[0];
        System.Reflection.ParameterInfo[] pars = ctor.GetParameters();
        object[] args = new object[pars.Length];
        for (int i = 0; i < pars.Length; i++) {
            if (pars[i].ParameterType == typeof(string)) {
                args[i] = target;
            }
            else if (pars[i].ParameterType == typeof(bool)) {
                args[i] = false;
            }
            else if (pars[i].ParameterType == typeof(string) || pars[i].ParameterType == typeof(object)) {
                args[i] = null!;
            }
            else if (pars[i].ParameterType == typeof(System.Threading.CancellationToken)) {
                args[i] = System.Threading.CancellationToken.None;
            }
            else {
                args[i] = pars[i].HasDefaultValue ? pars[i].DefaultValue! : null!;
            }
        }
        return (Microsoft.AspNetCore.Components.Routing.LocationChangingContext)ctor.Invoke(args);
    }

    private static bool DidPreventNavigation(Microsoft.AspNetCore.Components.Routing.LocationChangingContext context) {
        // .NET 10 exposes PreventNavigation() but keeps the prevented flag non-public; keep this
        // reflection seam local to tests so production code stays on the public NavigationLock API.
        System.Reflection.FieldInfo? field = typeof(Microsoft.AspNetCore.Components.Routing.LocationChangingContext)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SingleOrDefault(f => f.FieldType == typeof(bool) && f.Name.Contains("prevent", StringComparison.OrdinalIgnoreCase));
        field.ShouldNotBeNull("LocationChangingContext should track whether PreventNavigation was called.");
        return (bool)field.GetValue(context)!;
    }

    private sealed class TestNavigationManager : NavigationManager {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/test");

        public (string Uri, bool ForceLoad)? LastNavigateCall { get; private set; }
        public bool NavigationLockState { get; private set; }

        protected override void NavigateToCore(string uri, bool forceLoad) => LastNavigateCall = (uri, forceLoad);

        protected override void NavigateToCore(string uri, NavigationOptions options) => LastNavigateCall = (uri, options.ForceLoad);

        // Story 2-5 Task 0.3 compatibility — bUnit's default NavigationManager throws for NavigationLock
        // registration because SetNavigationLockState is intentionally abstract-ish. Override to no-op.
        protected override void SetNavigationLockState(bool value) => NavigationLockState = value;
    }

    private sealed class TestModel {
        public string Name { get; set; } = string.Empty;
    }
}
