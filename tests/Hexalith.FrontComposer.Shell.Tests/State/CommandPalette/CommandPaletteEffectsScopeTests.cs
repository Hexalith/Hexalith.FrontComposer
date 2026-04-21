// Story 3-4 Task 10.5 (D10 — fail-closed scope guard).
#pragma warning disable CA2007
#pragma warning disable xUnit1051
using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Shortcuts;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CommandPalette;

public class CommandPaletteEffectsScopeTests {
    [Theory]
    [InlineData(null, "user")]
    [InlineData("", "user")]
    [InlineData("   ", "user")]
    [InlineData("tenant", null)]
    [InlineData("tenant", "")]
    [InlineData("tenant", "   ")]
    public async Task HandleRecentRouteVisited_FailClosed_OnMissingScope(string? tenant, string? user) {
        IStorageService storage = Substitute.For<IStorageService>();
        CommandPaletteEffects sut = Build(tenant, user, storage, out IDispatcher dispatcher);

        await sut.HandleRecentRouteVisited(new RecentRouteVisitedAction("/x"), dispatcher);

        await storage.DidNotReceiveWithAnyArgs().SetAsync<string[]>(default!, default!);
    }

    [Fact]
    public async Task HandleRecentRouteVisited_PersistsWhenScopeIsValid() {
        IStorageService storage = Substitute.For<IStorageService>();
        CommandPaletteEffects sut = Build("tenant-a", "user-1", storage, out IDispatcher dispatcher);

        await sut.HandleRecentRouteVisited(new RecentRouteVisitedAction("/x"), dispatcher);

        await storage.Received().SetAsync(
            Arg.Is<string>(k => k.StartsWith("tenant-a:user-1:palette-recent", StringComparison.Ordinal)),
            Arg.Any<string[]>());
    }

    private static CommandPaletteEffects Build(string? tenant, string? user, IStorageService storage, out IDispatcher dispatcher) {
        dispatcher = Substitute.For<IDispatcher>();
        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        FakeTimeProvider time = new();
        services.AddSingleton<TimeProvider>(time);

        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenant);
        accessor.UserId.Returns(user);
        services.AddSingleton(accessor);
        services.AddSingleton(storage);

        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([]);
        services.AddSingleton(registry);

        services.AddSingleton<IShortcutService>(_ => new ShortcutService(time, Substitute.For<ILogger<ShortcutService>>()));
        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(_ => Guid.NewGuid().ToString("N"));
        services.AddSingleton(ulids);

        IServiceProvider sp = services.BuildServiceProvider();

        IState<FrontComposerCommandPaletteState> paletteState = Substitute.For<IState<FrontComposerCommandPaletteState>>();
        paletteState.Value.Returns(new FrontComposerCommandPaletteState(
            IsOpen: true,
            Query: string.Empty,
            Results: ImmutableArray<PaletteResult>.Empty,
            RecentRouteUrls: ["/x"],
            SelectedIndex: 0,
            LoadState: PaletteLoadState.Idle));

        IState<FrontComposerNavigationState> navState = Substitute.For<IState<FrontComposerNavigationState>>();
        navState.Value.Returns(new FrontComposerNavigationState(
            SidebarCollapsed: false,
            CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
            CurrentViewport: ViewportTier.Desktop,
            CurrentBoundedContext: null));

        return new CommandPaletteEffects(navState, paletteState, Substitute.For<ILogger<CommandPaletteEffects>>(), sp);
    }
}
