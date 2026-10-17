using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 4-5 T4.7 / AC8 — bUnit coverage for the AC8 breadcrumb banner. Three tests:
/// renders nothing on no-suppression, renders banner copy when IsHiddenByFilter is true,
/// "Clear filter" link dispatches FiltersResetAction.
/// </summary>
public sealed class FcExpandedRowHiddenBannerTests : BunitContext {
    [Fact]
    public async Task ScopeChangeDuringHeldRecoveryImportCancelsPriorFocusAndSpeech()
    {
        DelayedHiddenDetailFocusRuntime runtime = new();
        RecordingHiddenDetailFocusModule module = new();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.ScopeGeneration, 1));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(cut.Instance, runtime);
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "old-scope"));
        runtime.ImportCalls.ShouldBe(1);
        cut.Render(p => p.Add(c => c.ScopeGeneration, 2).Add(c => c.IsHiddenByFilter, false).Add(c => c.ResultIdentity, "replacement"));
        runtime.Import.TrySetResult(module);
        await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        module.FocusCalls.ShouldBe(0);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:orders:Orders").ShouldBeEmpty();
    }

    [Fact]
    public async Task DisposalDuringMissingFocusImportDoesNotRegisterDetailListeners()
    {
        DelayedHiddenDetailFocusRuntime runtime = new();
        RecordingHiddenDetailFocusModule module = new();
        Services.AddSingleton<IJSRuntime>(runtime);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders"));
        runtime.ImportCalls.ShouldBe(1);

        cut.Instance.Dispose();
        runtime.Import.TrySetResult(module);
        await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        module.TrackingCalls.ShouldBe(0);
        module.FocusCalls.ShouldBe(0);
    }

    [Fact]
    public void MissingDetailWithLostFocusUsesOnlyTheNonLiveExplanation()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js");
        module.Setup<bool>("focusReplacementHeading", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsMissing, true).Add(c => c.ResultIdentity, "removed"));
        module.VerifyInvoke("trackDetailFocus", 1).Single().Arguments[1].ShouldBe("orders:Orders");
        cut.Find(".fc-expanded-row-hidden-banner-text").GetAttribute("tabindex").ShouldBe("-1");
        cut.Find("[data-testid='fc-expanded-row-hidden-banner']").GetAttribute("aria-live").ShouldBe("off");
        spoken.Where(static message => message.Length > 0).ShouldBeEmpty();
        cut.Render(p => p.Add(c => c.ResultIdentity, "refresh"));
        module.VerifyInvoke("focusReplacementHeading", 1);
    }

    [Fact]
    public void MissingWithoutFilterEvidenceShowsNeutralReasonAndAnnouncesItOnce()
    {
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsMissing, true)
            .Add(c => c.ResultIdentity, "deleted"));

        cut.Find("[data-testid='fc-expanded-row-hidden-banner']").TextContent
            .ShouldContain("not in the current results");
        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner-clear']").ShouldBeEmpty();
        spoken.Where(static message => message.Length > 0).ShouldBe(["The expanded item is not in the current results."]);
        cut.Render(p => p.Add(c => c.ResultIdentity, "refresh"));
        spoken.Where(static message => message.Length > 0).Count().ShouldBe(1);
    }

    [Fact]
    public void ContinuouslyHiddenAtoBtoAUsesFreshSpeechEpisodeWithoutRefocus()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "A"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);

        cut.Render(p => p.Add(c => c.ResultIdentity, "B"));
        cut.Render(p => p.Add(c => c.ResultIdentity, "A"));
        cut.Render();

        spoken.Where(static message => message.Length > 0).ShouldBe([
            "Expanded item hidden by current filter.", "Expanded item hidden by current filter."]);
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    [Fact]
    public void SettledRepeatValueAfterPendingIntervalSpeaksWithoutRefocus()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "same"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        cut.Render(p => p.Add(c => c.ResultSettled, false));
        cut.Render(p => p.Add(c => c.ResultSettled, true));
        cut.Render();
        spoken.Where(static message => message.Length > 0).ShouldBe(["Expanded item hidden by current filter."]);
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    [Fact]
    public void SharedHiddenFocusReleasesCompletedOldResultsWithoutReopeningCurrentClaim()
    {
        SurfaceHiddenFocusOwnership ownership = SurfaceHiddenFocusOwnership.For(
            Services.GetRequiredService<ISurfaceAnnouncementCoordinator>());
        object first = new();
        object survivor = new();
        object late = new();
        for (int index = 0; index < 500; index++)
        {
            string group = "projection:orders:scope:1:result:" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ownership.Claim(group, first, _ => { }).ShouldBeTrue();
            ownership.TryOwn(group, first).ShouldBeTrue();
            ownership.Complete(group, first);
            ownership.Claim(group, survivor, _ => { }).ShouldBeFalse();
            ownership.Release(group, first);
            ownership.Claim(group, late, _ => { }).ShouldBeFalse();
            ownership.TryOwn(group, late).ShouldBeFalse();
            ownership.Release(group, late);
            ownership.Release(group, survivor);
        }

        var groups = (System.Collections.IDictionary)typeof(SurfaceHiddenFocusOwnership)
            .GetField("_groups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(ownership)!;
        groups.Count.ShouldBe(0);
    }

    [Fact]
    public void CoMountedFirstHiddenResultFocusesOnlyOnce()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        IRenderedComponent<FcExpandedRowHiddenBanner> first = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "same").Add(c => c.SharedStatusOwner, true));
        IRenderedComponent<FcExpandedRowHiddenBanner> second = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "same").Add(c => c.SharedStatusOwner, true));

        first.WaitForAssertion(() => _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1));
        first.Instance.Dispose();
        second.Render();
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CoMountedHeldFocusImportHandsCurrentEpisodeToSurvivingOwner(bool focusSucceeds)
    {
        DelayedHiddenDetailFocusRuntime firstRuntime = new();
        RecordingHiddenDetailFocusModule firstModule = new();
        DelayedHiddenDetailFocusRuntime secondRuntime = new();
        RecordingHiddenDetailFocusModule secondModule = new()
        {
            TimeoutFocusCallsRemaining = focusSucceeds ? 0 : 2,
        };
        secondRuntime.Import.TrySetResult(secondModule);
        IRenderedComponent<FcExpandedRowHiddenBanner> first = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false)
            .Add(c => c.SharedStatusOwner, true));
        IRenderedComponent<FcExpandedRowHiddenBanner> second = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false)
            .Add(c => c.SharedStatusOwner, true));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(first.Instance, firstRuntime);
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(second.Instance, secondRuntime);

        first.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "same"));
        second.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "same"));
        firstRuntime.ImportCalls.ShouldBe(1);
        secondRuntime.ImportCalls.ShouldBe(0);

        first.Instance.Dispose();
        second.WaitForAssertion(() => secondModule.FocusCalls.ShouldBe(focusSucceeds ? 1 : 2));
        secondRuntime.ImportCalls.ShouldBe(focusSucceeds ? 1 : 2);
        firstRuntime.Import.TrySetResult(firstModule);
        await firstModule.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        firstModule.FocusCalls.ShouldBe(0);
        secondModule.FocusCalls.ShouldBe(focusSucceeds ? 1 : 2);
        second.WaitForAssertion(() => Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection:orders:Orders")
            .ShouldBe(focusSucceeds ? string.Empty : "Expanded item hidden by current filter."));
    }

    [Fact]
    public void ScopeSwitchStartsFreshHiddenFocusForReusedResultIdentity()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "1").Add(c => c.ScopeGeneration, 1));
        cut.WaitForAssertion(() => _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1));
        cut.Render(p => p.Add(c => c.ScopeGeneration, 2));
        cut.WaitForAssertion(() => _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 2));
    }

    [Fact]
    public void CoMountedHiddenNoticesRetainSharedSpeechUntilLastOwnerLeaves()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcExpandedRowHiddenBanner> first = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "first").Add(c => c.SharedStatusOwner, true));
        IRenderedComponent<FcExpandedRowHiddenBanner> second = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "first").Add(c => c.SharedStatusOwner, true));
        first.Render(p => p.Add(c => c.ResultIdentity, "second"));
        second.Render(p => p.Add(c => c.ResultIdentity, "second"));
        announcements.Current("projection:orders:Orders").ShouldBe("Expanded item hidden by current filter.");
        first.Instance.Dispose();
        announcements.Current("projection:orders:Orders").ShouldBe("Expanded item hidden by current filter.");
        second.Instance.Dispose();
        announcements.Current("projection:orders:Orders").ShouldBeEmpty();
    }

    [Fact]
    public async Task NewHiddenEpisodeFocusesAfterObsoleteImportFinishes()
    {
        RecordingHiddenDetailFocusModule module = new();
        DelayedHiddenDetailFocusRuntime runtime = new();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "same"));
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, false));
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "same"));
        runtime.ImportCalls.ShouldBe(1);

        runtime.Import.TrySetResult(module);
        await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => module.FocusCalls.ShouldBe(1));
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:orders:Orders").ShouldBeEmpty();
    }

    [Fact]
    public async Task ConcurrentRenderDuringHeldFirstImportStartsOnlyOneFocusAttempt()
    {
        RecordingHiddenDetailFocusModule module = new();
        DelayedHiddenDetailFocusRuntime runtime = new();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "first"));
        cut.Render();
        runtime.ImportCalls.ShouldBe(1);
        runtime.Import.TrySetResult(module);
        await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        module.FocusCalls.ShouldBe(1);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:orders:Orders").ShouldBeEmpty();
    }

    [Fact]
    public async Task LaterSettledHiddenResultWithdrawsPendingFirstFocusAndSpeaksOnlyOnce()
    {
        RecordingHiddenDetailFocusModule module = new();
        DelayedHiddenDetailFocusRuntime runtime = new();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> delivered = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", delivered.Add);
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "first"));
        runtime.ImportCalls.ShouldBe(1);
        cut.Render(p => p.Add(c => c.ResultIdentity, "second"));
        delivered.ShouldBe(["Expanded item hidden by current filter."]);
        runtime.Import.TrySetResult(module);
        await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.Render();
        module.FocusCalls.ShouldBe(0);
        runtime.ImportCalls.ShouldBe(1);
        delivered.ShouldBe(["Expanded item hidden by current filter."]);
    }

    [Theory]
    [InlineData("import")]
    [InlineData("focusFirstButtonWithin")]
    [InlineData("dispose")]
    public void RequiredFocusTimeoutRetriesWithoutAnExternalRender(string timedOutCall)
    {
        RecordingHiddenDetailFocusModule module = new()
        {
            TimeoutFocusCallsRemaining = timedOutCall == "focusFirstButtonWithin" ? 1 : 0,
            TimeoutDisposal = timedOutCall == "dispose",
        };
        DelayedHiddenDetailFocusRuntime runtime = new() { TimeoutImportsRemaining = timedOutCall == "import" ? 1 : 0 };
        runtime.Import.TrySetResult(module);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "first"));
        cut.WaitForAssertion(() => module.FocusCalls.ShouldBe(timedOutCall == "focusFirstButtonWithin" ? 2 : 1));
        cut.Render();
        module.FocusCalls.ShouldBe(timedOutCall == "focusFirstButtonWithin" ? 2 : 1);
        runtime.ImportCalls.ShouldBe(timedOutCall == "dispose" ? 1 : 2);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:orders:Orders").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("import")]
    [InlineData("focusFirstButtonWithin")]
    public void RepeatedFocusTimeoutAnnouncesCurrentReasonOnceWithoutLaterRefocus(string timedOutCall)
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(new SurfaceAnnouncementCoordinator(time));
        RecordingHiddenDetailFocusModule module = new()
        {
            TimeoutFocusCallsRemaining = timedOutCall == "focusFirstButtonWithin" ? 2 : 0,
        };
        DelayedHiddenDetailFocusRuntime runtime = new() { TimeoutImportsRemaining = timedOutCall == "import" ? 2 : 0 };
        runtime.Import.TrySetResult(module);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);

        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "first"));
        cut.WaitForAssertion(() => spoken.Where(message => message.Length > 0)
            .ShouldBe(["Expanded item hidden by current filter."]));
        time.Advance(TimeSpan.FromMinutes(1));
        cut.Render();
        cut.Render();

        runtime.ImportCalls.ShouldBe(2);
        module.FocusCalls.ShouldBe(timedOutCall == "focusFirstButtonWithin" ? 2 : 0);
        spoken.Where(message => message.Length > 0).ShouldBe(["Expanded item hidden by current filter."]);
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBe("Expanded item hidden by current filter.");
    }

    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly IStringLocalizer<FcShellResources> _localizer = Substitute.For<IStringLocalizer<FcShellResources>>();

    public FcExpandedRowHiddenBannerTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _localizer["ExpandedRowHiddenByFilterBanner", Arg.Any<object[]>()]
            .Returns(callInfo => new LocalizedString(
                "ExpandedRowHiddenByFilterBanner",
                "1 expanded item hidden by current filter"));
        _localizer["ExpandedRowHiddenByFilterBannerClearLink"]
            .Returns(new LocalizedString("ExpandedRowHiddenByFilterBannerClearLink", "Clear filter"));
        _localizer["ExpandedRowUnavailableBanner"]
            .Returns(new LocalizedString("ExpandedRowUnavailableBanner", "The expanded item is not in the current results."));
        _localizer["Am22DetailHidden"]
            .Returns(new LocalizedString("Am22DetailHidden", "Expanded item hidden by current filter."));

        Services.AddSingleton(_dispatcher);
        Services.AddSingleton(_localizer);
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(new SurfaceAnnouncementCoordinator(TimeProvider.System));
    }

    [Fact]
    public void RendersNothing_WhenNotHiddenByFilter() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, false));

        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner']").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersBannerCopy_WhenHiddenByFilter() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, true));

        AngleSharp.Dom.IElement banner = cut.Find("[data-testid='fc-expanded-row-hidden-banner']");
        banner.GetAttribute("role").ShouldBe("group");
        banner.GetAttribute("aria-live").ShouldBe("off");
        banner.TextContent.ShouldContain("hidden by current filter");
        string? reasonId = cut.Find("[data-testid='fc-expanded-row-hidden-banner-clear']").GetAttribute("aria-describedby");
        reasonId.ShouldNotBeNullOrWhiteSpace();
        cut.Find("#" + reasonId).TextContent.ShouldContain("hidden by current filter");
    }

    [Fact]
    public void ClearFilterLink_DispatchesFiltersResetAction() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, true));

        cut.Find("[data-testid='fc-expanded-row-hidden-banner-clear']").Click();

        _dispatcher.Received(1).Dispatch(ArgEx.Is<FiltersResetAction>(a => a.ViewKey == "orders:Orders"));
    }

    [Fact]
    public void SecondHiddenResultSpeaksOnce() {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "row-a/filter-1"));

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "row-b/filter-2"));
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "row-b/filter-2"));

        spoken.Where(message => message.Length > 0).ShouldBe(["Expanded item hidden by current filter."]);

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false)
            .Add(c => c.ResultIdentity, "row-b/filter-2"));
        announcements.Current("projection:orders:Orders").ShouldBe(string.Empty);
    }

    [Fact]
    public void DirectHiddenNoticeKeepsOnePoliteNodeAndFirstResultUsesFocusAlone()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "first"));
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty();

        cut.Render(p => p.Add(c => c.ResultIdentity, "second"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-surface-status']")
            .TextContent.ShouldBe("Expanded item hidden by current filter."));
        cut.Render(p => p.Add(c => c.SharedStatusOwner, true));
        cut.FindAll("[data-testid='fc-surface-status']").ShouldBeEmpty();
    }

    [Fact]
    public void FirstHiddenResultFocusesClearFilterButLaterResultsDoNotRefocus() {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));

        announcements.Announce("projection:orders:Orders", "query:failed", "failed", "Data could not be loaded.", terminal: true);

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        cut.WaitForAssertion(() => _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1));
        announcements.Current("projection:orders:Orders").ShouldBe("Data could not be loaded.");

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-2"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    [Fact]
    public void FalseFocusOutcomeSpeaksOnceWithoutRetryingOnLaterRender() {
        var focus = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true);
        _ = focus.SetResult(false);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection:orders:Orders").ShouldContain("hidden");

        _ = focus.SetResult(true);
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }
    [Fact]
    public void UnsettledHiddenDetailDoesNotFocusUntilItsResultSettles()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1").Add(c => c.ResultSettled, false));
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "focusFirstButtonWithin").ShouldBe(0);
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1").Add(c => c.ResultSettled, true));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    [Fact]
    public void ContinuouslyHiddenPendingResultSpeaksOnceAndVisibleTransitionRefocuses()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "A"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        cut.Render(p => p.Add(c => c.ResultIdentity, "B").Add(c => c.ResultSettled, false));
        cut.Render(p => p.Add(c => c.ResultSettled, true));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        spoken.ShouldBe(["Expanded item hidden by current filter."]);
        cut.Render();
        spoken.Count.ShouldBe(1);

        cut.Render(p => p.Add(c => c.IsHiddenByFilter, false));
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "C"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 2);
    }
    [Fact]
    public void EqualInputSurfaceRebindStartsWithFocusOnlyAndWithdrawsOriginal()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p.Add(c => c.ViewKey, "original")
            .Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "first"));
        cut.Render(p => p.Add(c => c.ViewKey, "original").Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "second"));
        announcements.Current("projection:original").ShouldBe("Expanded item hidden by current filter.");
        cut.Render(p => p.Add(c => c.ViewKey, "replacement").Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "second"));
        announcements.Current("projection:original").ShouldBeEmpty();
        announcements.Current("projection:replacement").ShouldBeEmpty();
        cut.Render(p => p.Add(c => c.ViewKey, "replacement").Add(c => c.IsHiddenByFilter, true).Add(c => c.ResultIdentity, "third"));
        announcements.Current("projection:replacement").ShouldBe("Expanded item hidden by current filter.");
        cut.Instance.Dispose();
        cut.Dispose();
        announcements.Current("projection:replacement").ShouldBeEmpty();
    }

    [Fact]
    public async Task SettledToPendingDuringFocusImportKeepsContinuouslyHiddenReplacementSpeechOnly()
    {
        RecordingHiddenDetailFocusModule module = new();
        DelayedHiddenDetailFocusRuntime runtime = new();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));
        typeof(FcExpandedRowHiddenBanner).GetProperty("JS", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);
        cut.Render(p => p.Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "old-result").Add(c => c.ResultSettled, true));
        runtime.ImportCalls.ShouldBe(1);
        cut.Render(p => p.Add(c => c.ResultIdentity, "replacement").Add(c => c.ResultSettled, false));
        runtime.Import.TrySetResult(module);
        await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        module.FocusCalls.ShouldBe(0);

        cut.Render(p => p.Add(c => c.ResultSettled, true));
        module.FocusCalls.ShouldBe(0);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection:orders:Orders").ShouldBe("Expanded item hidden by current filter.");
    }

    [Fact]
    public void LaterSpeechOnlyResultStaysSpeechOnlyWhenAnotherBannerMounts()
    {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        IRenderedComponent<FcExpandedRowHiddenBanner> first = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "result-a"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);

        first.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "result-b"));
        _ = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "result-b"));

        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Current("projection:orders:Orders").ShouldContain("hidden");
    }


}
