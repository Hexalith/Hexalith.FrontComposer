using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;
using Hexalith.FrontComposer.Shell.Tests.Generated;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

public sealed class FcFreshRowIndicatorTests : LayoutComponentTestBase
{
    private readonly FakeTimeProvider _time = new();
    private readonly Epic9UserContextAccessor _scope = new();

    public FcFreshRowIndicatorTests()
    {
        CultureInfo.CurrentUICulture = new("en");
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_time));
        Services.Replace(ServiceDescriptor.Singleton<IUserContextAccessor>(_scope));
        EnsureStoreInitialized();
    }

    [Fact]
    public void LegacyStandaloneRequiresOnlyItsOriginalLocalizationService()
    {
        using BunitContext context = new();
        context.Services.AddLocalization();
        var cut = context.Render<FcNewItemIndicator>();
        cut.Find("[role=status][aria-live=polite]").TextContent.Trim()
            .ShouldBe("New item. It may not match current filters yet.");
        cut.Find("[role=status]").GetAttribute("aria-label").ShouldBe("New item added outside current filters");
    }

    [Fact]
    public void AdopterCoordinatorReceivesCanonicalSpeechAndOwnGroupCancellation()
    {
        using BunitContext context = new();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        context.Services.AddLocalization();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<TimeProvider>(_time);
        context.Services.AddSingleton<IUserContextAccessor>(_scope);
        context.Services.AddScoped<FreshRowAnnouncementLedger>();
        ISurfaceAnnouncementCoordinator speech = Substitute.For<ISurfaceAnnouncementCoordinator>();
        context.Services.AddSingleton(speech);
        var cut = context.Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()).Add(c => c.SharedStatusOwner, true));
        cut.WaitForAssertion(() => speech.Received(1).Announce("projection:view-one", Arg.Is<string>(group => group.StartsWith("fresh-row:", StringComparison.Ordinal)),
            "message-one", "Updated row: row-one.", false, true));
        cut.Render();
        speech.Received(1).Announce(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), false, true);
        cut.Instance.Dispose();
        speech.Received(1).Cancel("projection:view-one", Arg.Is<string>(group => group.StartsWith("fresh-row:", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("en", "Updated row: row-one.")]
    [InlineData("fr", "Ligne mise à jour : row-one.")]
    public void EntryUsesCanonicalCopyIconAndOneSharedPoliteNode(string culture, string copy)
    {
        CultureInfo.CurrentUICulture = new(culture);
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()));
        var cue = cut.Find("[data-testid=fc-new-item-indicator]");
        cue.TextContent.Trim().ShouldBe(copy);
        cue.GetAttribute("role").ShouldBeNull();
        cue.GetAttribute("aria-live").ShouldBeNull();
        cue.GetAttribute("aria-label").ShouldBeNull();
        cue.QuerySelector("[aria-hidden=true]").ShouldNotBeNull();
        var status = cut.FindComponent<FcSurfaceStatus>();
        status.WaitForAssertion(() => status.Find("[data-testid=fc-surface-status][aria-live=polite]").TextContent.ShouldBe(copy));
        // bUnit's parent query cache was populated above before the deferred child render.
        // The child's DOM and freshly parsed renderer markup both contain the delivered copy.
        var rendered = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(cut.Markup);
        rendered.QuerySelector("[data-testid=fc-surface-status]")!.TextContent.ShouldBe(copy);
        cut.FindAll("[aria-live=polite]").Count.ShouldBe(1);
        JSInterop.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task SharedOwnerOverrideAndRemountKeepCanonicalSpeechOnceAcrossViews()
    {
        ISurfaceAnnouncementCoordinator speech = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> delivered = [];
        using var one = speech.Subscribe("projection:view-one", delivered.Add);
        using var two = speech.Subscribe("projection:view-two", delivered.Add);
        var status = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:view-one"));
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry())
            .Add(c => c.SharedStatusOwner, true).Add(c => c.Text, "Custom visible copy"));
        cut.Markup.ShouldContain("Custom visible copy");
        cut.FindAll("[aria-live=polite]").ShouldBeEmpty();
        status.WaitForAssertion(() => status.Find("[role=status]").TextContent.ShouldBe("Updated row: row-one."));
        cut.Render();
        await DisposeComponentsAsync();
        delivered.Where(x => x.Length > 0).ShouldBe(["Updated row: row-one."]);
        var remount = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()));
        var otherView = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry() with { ViewKey = "view-two" }));
        remount.Find("[data-testid=fc-new-item-indicator]").TextContent.Trim().ShouldBe("Updated row: row-one.");
        otherView.Find("[data-testid=fc-new-item-indicator]").ShouldNotBeNull();
        delivered.Where(x => x.Length > 0).Count().ShouldBe(1);
    }

    [Fact]
    public async Task CancelRestoresPriorStateSilentlyAndLeavesLaterProjectionSpeechAvailable()
    {
        ISurfaceAnnouncementCoordinator speech = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        speech.Announce("projection:view-one", "limit:prior", "limit", "Prior limit.", immediate: true);
        var status = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:view-one"));
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()).Add(c => c.SharedStatusOwner, true));
        status.WaitForAssertion(() => status.Find("[role=status]").TextContent.ShouldBe("Updated row: row-one."));
        await cut.InvokeAsync(cut.Instance.Dispose);
        status.WaitForAssertion(() => {
            status.Find("[role=status]").TextContent.ShouldBe("Prior limit.");
            status.Find("[role=status]").GetAttribute("aria-live").ShouldBe("off");
        });
        speech.Announce("projection:view-one", "limit:next", "new-limit", "Next limit.", immediate: true);
        status.WaitForAssertion(() => {
            status.Find("[role=status]").TextContent.ShouldBe("Next limit.");
            status.Find("[role=status]").GetAttribute("aria-live").ShouldBe("polite");
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UnresolvedScopeRendersNoCueOrSpeech(bool missingTenant)
    {
        if (missingTenant) _scope.TenantId = null;
        else _scope.UserId = " ";
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()));
        cut.Markup.ShouldBeEmpty();
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:view-one").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DirectEntryCannotMoveIntoAnotherResolvedScopeOrReturnAfterScopeClear(bool tenant)
    {
        NewItemIndicatorEntry entry = Entry();
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry));
        cut.Find("[data-testid=fc-new-item-indicator]").ShouldNotBeNull();
        if (tenant) _scope.TenantId = "other-tenant";
        else _scope.UserId = "other-user";
        cut.Render();
        cut.Markup.ShouldBeEmpty();
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:view-one").ShouldBeEmpty();
        _scope.TenantId = "test-tenant";
        _scope.UserId = "test-user";
        cut.Render();
        cut.Markup.ShouldBeEmpty();
        var remount = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry with { ViewKey = "other-view" }));
        remount.Markup.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("", "row-one", "message-one")]
    [InlineData("view-one", " ", "message-one")]
    [InlineData("view-one", "row-one", "")]
    [InlineData("view-one", "row-one", " ")]
    public void InvalidEntryProvenanceIsSuppressedBeforeRenderingOrClaiming(string view, string entity, string message)
    {
        NewItemIndicatorEntry entry = new(view, entity, message, _time.GetUtcNow());
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry));
        cut.Markup.ShouldBeEmpty();
        Services.GetRequiredService<FreshRowAnnouncementLedger>().TryClaim(entry).ShouldBeFalse();
    }

    [Fact]
    public void DirectEntryExpiresAtTenSecondsAndAnExpiredRemountCannotClaimAgain()
    {
        NewItemIndicatorEntry entry = Entry();
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry));
        _time.Advance(TimeSpan.FromMilliseconds(9999));
        cut.Find("[data-testid=fc-new-item-indicator]").ShouldNotBeNull();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => cut.Markup.ShouldBeEmpty());
        var remount = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry));
        remount.Markup.ShouldBeEmpty();
        Services.GetRequiredService<ISurfaceAnnouncementCoordinator>().Current("projection:view-one").ShouldBeEmpty();
    }

    [Fact]
    public void EquivalentEntryCopiesKeepTheSharedNodeAndOriginalDirectExpiry()
    {
        NewItemIndicatorEntry entry = Entry();
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry));
        cut.WaitForAssertion(() => cut.Find("[data-testid=fc-surface-status]").TextContent.ShouldBe("Updated row: row-one."));
        _time.Advance(TimeSpan.FromSeconds(5));
        cut.Render(p => p.Add(c => c.Entry, entry with { }));
        cut.Find("[data-testid=fc-surface-status]").TextContent.ShouldBe("Updated row: row-one.");
        _time.Advance(TimeSpan.FromMilliseconds(4999));
        cut.Find("[data-testid=fc-new-item-indicator]").ShouldNotBeNull();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => cut.Markup.ShouldBeEmpty());
    }

    [Fact]
    public void StateOwnedEntriesKeepTheirAddAnchoredLifetimeEvenWithEarlierProvenance()
    {
        NewItemIndicatorEntry entry = Entry() with { CreatedAt = _time.GetUtcNow().AddMinutes(-1) };
        Services.GetRequiredService<INewItemIndicatorStateService>().Add(entry);
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, entry));
        _time.Advance(TimeSpan.FromMilliseconds(9999));
        cut.Render();
        cut.Find("[data-testid=fc-new-item-indicator]").ShouldNotBeNull();
        Services.GetRequiredService<INewItemIndicatorStateService>().Snapshot("view-one").ShouldHaveSingleItem();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        Services.GetRequiredService<INewItemIndicatorStateService>().Snapshot("view-one").ShouldBeEmpty();
        cut.WaitForAssertion(() => cut.Markup.ShouldBeEmpty());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HiddenPanelDefersFirstSpeechIncludingNestedInactiveAncestors(bool nested)
    {
        List<string> messages = [];
        var speech = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        using var subscription = speech.Subscribe("projection:view-one", messages.Add);
        NewItemIndicatorEntry entry = Entry();
        RenderFragment cue = builder => {
            builder.OpenComponent<FcNewItemIndicator>(0);
            builder.AddAttribute(1, nameof(FcNewItemIndicator.Entry), entry);
            builder.CloseComponent();
        };
        RenderFragment content = nested ? builder => {
            builder.OpenComponent<FcPageTabs>(0);
            builder.AddAttribute(1, nameof(FcPageTabs.ActiveTabId), "cue");
            builder.AddAttribute(2, nameof(FcPageTabs.ChildContent), Panels(cue));
            builder.CloseComponent();
        } : cue;
        var tabs = Render<FcPageTabs>(p => p.Add(c => c.ActiveTabId, "other").AddChildContent(Panels(content)));
        tabs.FindAll("[data-testid=fc-new-item-indicator]").Count.ShouldBe(1);
        messages.ShouldBeEmpty();
        tabs.Render(p => p.Add(c => c.ActiveTabId, "cue"));
        tabs.WaitForAssertion(() => messages.ShouldBe(["Updated row: row-one."]));
        tabs.Render(p => p.Add(c => c.ActiveTabId, "other"));
        tabs.Render(p => p.Add(c => c.ActiveTabId, "cue"));
        messages.ShouldBe(["Updated row: row-one."]);
    }

    [Fact]
    public void OrdinaryNonexclusiveInitialReplayKeepsCoordinatorSpeechPolicy()
    {
        var speech = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        speech.Announce("ordinary", "load:initial", "ready", "Page ready.", terminal: true);
        var status = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "ordinary"));
        status.Find("[role=status][aria-live=polite]").TextContent.ShouldBe("Page ready.");
    }

    [Fact]
    public void RouteBackedHiddenPanelDefersFirstDeliveryAndRouteReactivationIsSilent()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/fresh/other");
        List<string> messages = [];
        using var subscription = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>()
            .Subscribe("projection:view-one", message => { if (!string.IsNullOrEmpty(message)) messages.Add(message); });
        NewItemIndicatorEntry entry = Entry();
        RenderFragment cue = builder => {
            builder.OpenComponent<FcNewItemIndicator>(0);
            builder.AddAttribute(1, nameof(FcNewItemIndicator.Entry), entry);
            builder.CloseComponent();
        };
        var tabs = Render<FcPageTabs>(p => p.Add(c => c.ModuleRoute, "/fresh").Add(c => c.DefaultTabId, "other")
            .AddChildContent(Panels(cue)));
        messages.ShouldBeEmpty();
        tabs.FindAll("[data-testid=fc-surface-status]").ShouldBeEmpty();
        navigation.NavigateTo("/fresh/cue");
        tabs.WaitForAssertion(() => {
            messages.ShouldBe(["Updated row: row-one."]);
            tabs.Find("[data-testid=fc-surface-status][aria-live=polite]").TextContent.ShouldBe("Updated row: row-one.");
        });
        navigation.NavigateTo("/fresh/other");
        navigation.NavigateTo("/fresh/cue");
        tabs.WaitForAssertion(() => tabs.Find("[data-testid=fc-surface-status]").GetAttribute("aria-live").ShouldBe("off"));
        messages.ShouldBe(["Updated row: row-one."]);
    }

    [Fact]
    public void TwoRetainedSameViewPanelsGiveTheirSoleNodeToTheExposedFirstDelivery()
    {
        NewItemIndicatorEntry entry = Entry();
        RenderFragment Panel(string row, string message) => builder => {
            builder.OpenComponent<FcNewItemIndicator>(0);
            builder.AddAttribute(1, nameof(FcNewItemIndicator.Entry), entry with { EntityKey = row, MessageId = message });
            builder.CloseComponent();
        };
        RenderFragment panels = builder => {
            builder.OpenComponent<FcPageTab>(0);
            builder.AddAttribute(1, nameof(FcPageTab.Id), "hidden");
            builder.AddAttribute(2, nameof(FcPageTab.Header), "Hidden");
            builder.AddAttribute(3, nameof(FcPageTab.ChildContent), Panel("row-hidden", "hidden-message"));
            builder.CloseComponent();
            builder.OpenComponent<FcPageTab>(4);
            builder.AddAttribute(5, nameof(FcPageTab.Id), "visible");
            builder.AddAttribute(6, nameof(FcPageTab.Header), "Visible");
            builder.AddAttribute(7, nameof(FcPageTab.ChildContent), Panel("row-visible", "visible-message"));
            builder.CloseComponent();
        };
        var tabs = Render<FcPageTabs>(p => p.Add(c => c.ActiveTabId, "visible").AddChildContent(panels));
        tabs.WaitForAssertion(() => {
            tabs.FindAll("[data-testid=fc-surface-status]").Count.ShouldBe(1);
            tabs.Find("#visible-panel [data-testid=fc-surface-status][aria-live=polite]").TextContent.ShouldBe("Updated row: row-visible.");
        });
        tabs.Render(p => p.Add(c => c.ActiveTabId, "hidden"));
        tabs.WaitForAssertion(() => tabs.Find("#hidden-panel [data-testid=fc-surface-status][aria-live=polite]")
            .TextContent.ShouldBe("Updated row: row-hidden."));
        tabs.Render(p => p.Add(c => c.ActiveTabId, "visible"));
        tabs.WaitForAssertion(() => {
            tabs.FindAll("[data-testid=fc-surface-status]").Count.ShouldBe(1);
            tabs.Find("#visible-panel [data-testid=fc-surface-status]").GetAttribute("aria-live").ShouldBe("off");
        });
    }

    [Fact]
    public async Task ForwardingDecoratorKeepsPendingTimersAndSilentSharedNodeWithdrawal()
    {
        using BunitContext context = new();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        context.Services.AddLocalization();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<TimeProvider>(_time);
        context.Services.AddSingleton<IUserContextAccessor>(_scope);
        context.Services.AddScoped<FreshRowAnnouncementLedger>();
        using SurfaceAnnouncementCoordinator inner = new(_time);
        RendererThreadAnnouncementCoordinator forwarding = new(inner);
        context.Services.AddSingleton<ISurfaceAnnouncementCoordinator>(forwarding);
        var status = context.Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:view-one"));
        forwarding.Announce("projection:view-one", "stale:pending", "stale", "Pending projection.");
        var cut = context.Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()).Add(c => c.SharedStatusOwner, true));
        status.WaitForAssertion(() => status.Find("[role=status][aria-live=polite]").TextContent.ShouldBe("Updated row: row-one."));
        await cut.InvokeAsync(cut.Instance.Dispose);
        status.WaitForAssertion(() => {
            status.Find("[role=status]").TextContent.ShouldBe("Pending projection.");
            status.Find("[role=status]").GetAttribute("aria-live").ShouldBe("off");
        });
        _time.Advance(TimeSpan.FromMilliseconds(250));
        status.WaitForAssertion(() => status.Find("[role=status][aria-live=polite]").TextContent.ShouldBe("Pending projection."));
        forwarding.AnnounceCount.ShouldBe(2);
        forwarding.CancelCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("stale:pending", "stale")]
    [InlineData("limit:pending", "limit")]
    [InlineData("page:1", "ready")]
    public async Task FreshRemovalIsSilentAndPreservesUnrelatedPendingDelivery(string group, string identity)
    {
        var speech = (SurfaceAnnouncementCoordinator)Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<(string Message, bool Silent)> messages = [];
        using var subscription = speech.SubscribeWithReplay("projection:view-one", (message, revision) =>
            messages.Add((message, speech.IsSilentRevision("projection:view-one", revision))));
        var status = Render<FcSurfaceStatus>(p => p.Add(c => c.Surface, "projection:view-one"));
        speech.Announce("projection:view-one", group, identity, "Pending projection.");
        var cut = Render<FcNewItemIndicator>(p => p.Add(c => c.Entry, Entry()).Add(c => c.SharedStatusOwner, true));
        cut.WaitForAssertion(() => messages.Count(x => !x.Silent && x.Message == "Updated row: row-one.").ShouldBe(1));
        await cut.InvokeAsync(cut.Instance.Dispose);
        messages.Where(x => !x.Silent && x.Message == "Pending projection.").ShouldBeEmpty();
        status.WaitForAssertion(() => status.Find("[role=status]").GetAttribute("aria-live").ShouldBe("off"));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        status.WaitForAssertion(() => status.Find("[role=status]").TextContent.ShouldBe("Pending projection."));
        messages.Count(x => !x.Silent && x.Message == "Pending projection.").ShouldBe(1);
    }

    [Fact]
    public void RepeatedFreshCyclesRetireOnlyTheirOwnGroupsAndHistory()
    {
        var speech = (SurfaceAnnouncementCoordinator)Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        speech.Announce("projection:view-one", "limit:prior", "limit", "Prior limit.", immediate: true);
        for (int index = 0; index < 100; index++)
        {
            string group = "fresh-row:" + index;
            speech.PublishFreshRow("projection:view-one", group, "message", "Updated row: row-one.");
            speech.WithdrawFreshRow("projection:view-one", group);
            speech.Current("projection:view-one").ShouldBe("Prior limit.");
        }
        var surfaces = (System.Collections.IDictionary)typeof(SurfaceAnnouncementCoordinator)
            .GetField("_surfaces", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(speech)!;
        object owner = surfaces["projection:view-one"]!;
        var groups = (System.Collections.IDictionary)owner.GetType().GetProperty("Groups")!.GetValue(owner)!;
        groups.Count.ShouldBe(1);
        ((HashSet<long>)owner.GetType().GetProperty("SilentRevisions")!.GetValue(owner)!).ShouldBeEmpty();
        speech.Announce("projection:view-one", "limit:prior", "new-limit", "Updated limit.", immediate: true);
        speech.Current("projection:view-one").ShouldBe("Updated limit.");
    }

    private static RenderFragment Panels(RenderFragment content) => builder =>
    {
        builder.OpenComponent<FcPageTab>(0);
        builder.AddAttribute(1, nameof(FcPageTab.Id), "other");
        builder.AddAttribute(2, nameof(FcPageTab.Header), "Other");
        builder.AddAttribute(3, nameof(FcPageTab.ChildContent), (RenderFragment)(b => b.AddContent(0, "Other content")));
        builder.CloseComponent();
        builder.OpenComponent<FcPageTab>(4);
        builder.AddAttribute(5, nameof(FcPageTab.Id), "cue");
        builder.AddAttribute(6, nameof(FcPageTab.Header), "Cue");
        builder.AddAttribute(7, nameof(FcPageTab.ChildContent), content);
        builder.CloseComponent();
    };

    private NewItemIndicatorEntry Entry() => new("view-one", "row-one", "message-one", _time.GetUtcNow());
}
