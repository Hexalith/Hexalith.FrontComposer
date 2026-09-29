using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Forms;

/// <summary>
/// Story 13.3 VR-05 / FM-09 / AM-20 — the blocked-submit outcome keeps one always-mounted polite
/// status node, announces once per blocked attempt, and never moves focus off the attempted control.
/// </summary>
public sealed class FcCommandBlockedOutcomeTests : BunitContext {
    private const string BlockedMessage = "This command did not run. Another command is already in progress.";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));

    public FcCommandBlockedOutcomeTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        FocusModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js");
        _ = FocusModule.Setup<bool>("focusAttemptedControl", _ => true).SetResult(true);
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        _ = Services.AddLogging();
        _ = Services.AddSingleton<TimeProvider>(_time);
    }

    private BunitJSModuleInterop FocusModule { get; }

    [Fact]
    public void StatusNodeIsMountedEmptyBeforeAnyBlockedAttempt() {
        IRenderedComponent<FcCommandBlockedOutcome> cut = RenderOutcome();

        IElement status = cut.Find("[data-testid='fc-command-blocked-status']");
        status.GetAttribute("role").ShouldBe("status");
        status.GetAttribute("aria-live").ShouldBe("polite");
        status.GetAttribute("aria-atomic").ShouldBe("true");
        status.TextContent.ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-view-active-command']").ShouldBeEmpty();
        cut.Instance.IsBlocked.ShouldBeFalse();
    }

    [Fact]
    public async Task EveryBlockedAttemptClearsThenResetsTheExactMessageAndKeepsAttemptedFocus() {
        _ = FocusModule.Setup<bool>("hasActiveLifecycle", _ => true).SetResult(false);
        IRenderedComponent<FcCommandBlockedOutcome> cut = RenderOutcome();

        Task first = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        cut.WaitForAssertion(() => cut.Instance.IsBlocked.ShouldBeTrue());
        StatusText(cut).ShouldBeEmpty();
        await AdvanceUntilAsync(() => first.IsCompleted);
        cut.WaitForAssertion(() => StatusText(cut).ShouldBe(BlockedMessage));

        // BH2-06 — an identical repeat is cleared and set again, so it is announced again.
        Task second = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        cut.WaitForAssertion(() => StatusText(cut).ShouldBeEmpty());
        await AdvanceUntilAsync(() => second.IsCompleted);
        cut.WaitForAssertion(() => StatusText(cut).ShouldBe(BlockedMessage));

        // FM-09 / BH3-05 — each attempt asks to keep focus on the attempted control without forcing it
        // off a control inside the same form (Enter in a field keeps that field focused).
        JSRuntimeInvocation[] focusCalls = [.. FocusModule.Invocations.Where(invocation => invocation.Identifier == "focusAttemptedControl")];
        focusCalls.Length.ShouldBe(2);
        focusCalls.ShouldAllBe(invocation => Equals(invocation.Arguments[0], "attempted-submit") && Equals(invocation.Arguments[1], false));
        cut.FindAll("[role='status']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='fc-view-active-command']").ShouldBeEmpty();
    }

    [Fact]
    public async Task OverlappingBlockedAttemptsEachGetTheirOwnClearAndSetCycle() {
        _ = FocusModule.Setup<bool>("hasActiveLifecycle", _ => true).SetResult(false);
        IRenderedComponent<FcCommandBlockedOutcome> cut = RenderOutcome();
        List<string> texts = [];
        cut.OnMarkupUpdated += (_, _) => {
            string text = StatusText(cut);
            if (texts.Count == 0 || !string.Equals(texts[^1], text, StringComparison.Ordinal)) {
                texts.Add(text);
            }
        };

        // BH5-16 — the second attempt starts before the first one's re-announce delay has elapsed, so
        // only the serialized presentation keeps two separate clear-then-set cycles.
        Task first = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        Task second = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        first.IsCompleted.ShouldBeFalse();
        second.IsCompleted.ShouldBeFalse();
        await AdvanceUntilAsync(() => first.IsCompleted && second.IsCompleted);
        cut.WaitForAssertion(() => StatusText(cut).ShouldBe(BlockedMessage));

        texts.ShouldBe([string.Empty, BlockedMessage, string.Empty, BlockedMessage]);
        FocusModule.Invocations.Count(invocation => invocation.Identifier == "focusAttemptedControl").ShouldBe(2);
        cut.FindAll("[role='status']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task ViewActiveCommandIsOfferedOnlyWhileTheActiveLifecycleIsMounted() {
        _ = FocusModule.Setup<bool>("hasActiveLifecycle", _ => true).SetResult(true);
        _ = FocusModule.Setup<bool>("focusActiveLifecycle", _ => true).SetResult(false);
        IRenderedComponent<FcCommandBlockedOutcome> cut = RenderOutcome();

        Task presentation = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        await AdvanceUntilAsync(() => presentation.IsCompleted);
        cut.WaitForAssertion(() => _ = cut.Find("[data-testid='fc-view-active-command']"));

        cut.Find("[data-testid='fc-view-active-command']").Click();

        // ECH-06 / BH-11 — an action whose lifecycle is gone is withdrawn and focus returns, forced,
        // to the attempted control instead of staying on the removed button.
        cut.WaitForAssertion(() => {
            cut.FindAll("[data-testid='fc-view-active-command']").ShouldBeEmpty();
            FocusModule.Invocations.Count(invocation => invocation.Identifier == "focusActiveLifecycle").ShouldBe(1);
            FocusModule.Invocations.Count(invocation => invocation.Identifier == "focusAttemptedControl").ShouldBe(2);
            Equals(FocusModule.Invocations.Last(invocation => invocation.Identifier == "focusAttemptedControl").Arguments[1], true)
                .ShouldBeTrue();
        });
        StatusText(cut).ShouldBe(BlockedMessage);
    }

    [Fact]
    public async Task ClearWithdrawsAPendingAnnouncement() {
        _ = FocusModule.Setup<bool>("hasActiveLifecycle", _ => true).SetResult(true);
        IRenderedComponent<FcCommandBlockedOutcome> cut = RenderOutcome();

        Task presentation = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        cut.WaitForAssertion(() => cut.Instance.IsBlocked.ShouldBeTrue());
        await cut.InvokeAsync(() => cut.Instance.ClearAsync());
        await AdvanceUntilAsync(() => presentation.IsCompleted);

        // An admitted later attempt withdraws the outcome before its announcement is set.
        presentation.IsCompleted.ShouldBeTrue();
        cut.Instance.IsBlocked.ShouldBeFalse();
        StatusText(cut).ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-view-active-command']").ShouldBeEmpty();
        FocusModule.Invocations.Count(invocation => invocation.Identifier == "hasActiveLifecycle").ShouldBe(0);
    }

    [Fact]
    public async Task AStalledModuleDisposeDoesNotFaultLaterBlockedAttempts() {
        StalledDisposeJSRuntime runtime = new();
        Services.Replace(ServiceDescriptor.Singleton<IJSRuntime>(runtime));
        IRenderedComponent<FcCommandBlockedOutcome> cut = RenderOutcome();

        Task first = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        await AdvanceUntilAsync(() => first.IsCompleted);
        Task second = cut.InvokeAsync(() => cut.Instance.PresentAsync());
        await AdvanceUntilAsync(() => second.IsCompleted);

        // E7-01 — a module dispose that times out is caught like the stalled call itself, so it never
        // faults the serialized presentation chain that every later attempt awaits.
        first.IsCompletedSuccessfully.ShouldBeTrue();
        second.IsCompletedSuccessfully.ShouldBeTrue();
        cut.WaitForAssertion(() => StatusText(cut).ShouldBe(BlockedMessage));
        runtime.Module.DisposeAttempts.ShouldBe(4);
    }

    private async Task AdvanceUntilAsync(Func<bool> condition) {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline) {
            _time.Advance(TimeSpan.FromMilliseconds(50));
            await Task.Delay(10, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

    private IRenderedComponent<FcCommandBlockedOutcome> RenderOutcome()
        => Render<FcCommandBlockedOutcome>(parameters => parameters
            .Add(component => component.AttemptedControlId, "attempted-submit"));

    private static string StatusText(IRenderedComponent<FcCommandBlockedOutcome> cut)
        => cut.Find("[data-testid='fc-command-blocked-status']").TextContent;

    private sealed class StalledDisposeJSRuntime : IJSRuntime {
        public StalledDisposeModule Module { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => identifier == "import" && Module is TValue module
                ? ValueTask.FromResult(module)
                : ValueTask.FromResult(default(TValue)!);
    }

    private sealed class StalledDisposeModule : IJSObjectReference {
        public int DisposeAttempts { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);

        public ValueTask DisposeAsync() {
            DisposeAttempts++;
            return ValueTask.FromException(new TaskCanceledException("The JS interop dispose call timed out."));
        }
    }
}
