// Story 3-4 Task 10.1f (D24 — AC1).
#pragma warning disable CA2007
using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Shortcuts;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Shortcuts;

public class FrontComposerShortcutRegistrarTests
{
    [Fact]
    public async Task RegisterShellDefaultsAsync_RegistersFiveShellBindings()
    {
        // P19 (2026-04-21 pass-4): renamed from "RegistersThreeShellBindings" and extended to cover
        // all five shell defaults, including the D25 Mac-parity meta+k / meta+, bindings that the
        // previous test silently skipped.
        IShortcutService shortcuts = Substitute.For<IShortcutService>();
        FrontComposerShortcutRegistrar sut = BuildRegistrar(shortcuts, out _);

        await sut.RegisterShellDefaultsAsync();

        shortcuts.Received(1).Register("ctrl+k", "PaletteShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("meta+k", "PaletteShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("ctrl+,", "SettingsShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("meta+,", "SettingsShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("g h", "HomeShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task RegisterShellDefaultsAsync_IsIdempotent_WithinSameInstance()
    {
        IShortcutService shortcuts = Substitute.For<IShortcutService>();
        FrontComposerShortcutRegistrar sut = BuildRegistrar(shortcuts, out _);

        await sut.RegisterShellDefaultsAsync();
        await sut.RegisterShellDefaultsAsync();
        await sut.RegisterShellDefaultsAsync();

        shortcuts.Received(1).Register("ctrl+k", "PaletteShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("meta+k", "PaletteShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("ctrl+,", "SettingsShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("meta+,", "SettingsShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
        shortcuts.Received(1).Register("g h", "HomeShortcutDescription", Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task RegisterShellDefaultsAsync_RollsBackIdempotencyFlagOnFailure_SoRetryCanSucceed()
    {
        // P2 (2026-04-21 pass-4): if a Register call throws, the idempotency flag must reset so a
        // subsequent first-render pass can retry. Previously _registered stayed at 1 permanently
        // and the shell was left with a partial binding set.
        IShortcutService shortcuts = Substitute.For<IShortcutService>();
        int callCount = 0;
        shortcuts
            .When(static x => x.Register(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<Task>>(), Arg.Any<string>(), Arg.Any<int>()))
            .Do(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("boom");
                }
            });

        FrontComposerShortcutRegistrar sut = BuildRegistrar(shortcuts, out _);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => sut.RegisterShellDefaultsAsync());

        // Second call should not be a no-op — the rollback freed the flag.
        await sut.RegisterShellDefaultsAsync();

        // First attempt made 1 call (threw), second attempt made 6 (5 shell defaults + "/" per
        // Story 4-3 D10) → total 7.
        callCount.ShouldBe(7);
    }

    [Fact]
    public async Task OpenPaletteAsync_WhenDialogOpenFails_DispatchesCloseRollback_AndRethrows()
    {
        IShortcutService shortcuts = Substitute.For<IShortcutService>();
        ThrowingDialogService dialog = new();
        FrontComposerShortcutRegistrar sut = BuildRegistrar(shortcuts, out IDispatcher dispatcher, dialog: dialog);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => sut.OpenPaletteAsync());

        ex.Message.ShouldBe("dialog failed");
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteOpenedAction>());
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
    }

    private static FrontComposerShortcutRegistrar BuildRegistrar(
        IShortcutService shortcuts,
        out IDispatcher dispatcher,
        bool isOpen = false,
        IDialogService? dialog = null)
    {
        dispatcher = Substitute.For<IDispatcher>();
        IState<FrontComposerCommandPaletteState> state = Substitute.For<IState<FrontComposerCommandPaletteState>>();
        state.Value.Returns(new FrontComposerCommandPaletteState(isOpen, string.Empty,
            ImmutableArray<PaletteResult>.Empty,
            ImmutableArray<string>.Empty,
            0, PaletteLoadState.Idle));
        dialog ??= new Hexalith.FrontComposer.Shell.Tests.Components.Layout.RecordingDialogService();
        IStringLocalizer<FcShellResources> loc = new EchoLocalizer();
        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(_ => Guid.NewGuid().ToString("N"));
        NavigationManager nav = new BunitNavigationManager();
        DataGridFocusScope focus = new(Substitute.For<IJSRuntime>());
        return new FrontComposerShortcutRegistrar(shortcuts, dispatcher, state, dialog, nav, loc, ulids, focus);
    }

    private sealed class BunitNavigationManager : NavigationManager
    {
        public BunitNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class ThrowingDialogService : DialogService
    {
        public ThrowingDialogService()
            : base(
                new ServiceCollection()
                    .AddSingleton(Substitute.For<IJSRuntime>())
                    .BuildServiceProvider(),
                Substitute.For<IFluentLocalizer>())
        {
        }

        public override Task<DialogResult> ShowDialogAsync(Type dialogComponent, DialogOptions options)
            => throw new InvalidOperationException("dialog failed");
    }

    private sealed class EchoLocalizer : IStringLocalizer<FcShellResources>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, name);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
