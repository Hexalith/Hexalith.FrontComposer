using Bunit;
using Bunit.TestDoubles;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Shared bUnit setup for Story 3-1 shell layout component tests.
/// </summary>
public abstract class LayoutComponentTestBase : BunitContext {
    private bool _storeInitialized;

    protected LayoutComponentTestBase() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        MarkupSanitizedOptions.ThrowOnUnsafe = false;
        _ = Services.AddLogging();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddHexalithFrontComposerQuickstart();
        Services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => {
            IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
            accessor.TenantId.Returns("test-tenant");
            accessor.UserId.Returns("test-user");
            return accessor;
        }));

        ThemeService = Substitute.For<IThemeService>();
        Services.Replace(ServiceDescriptor.Scoped<IThemeService>(_ => ThemeService));

        BeforeUnloadModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-beforeunload.js");
        BeforeUnloadSubscription = BeforeUnloadModule.SetupModule("register", _ => true);
        _ = BeforeUnloadModule.SetupVoid("unregister", _ => true).SetVoidResult();

        PrefersColorSchemeModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-prefers-color-scheme.js");
        PrefersColorSchemeSubscription = PrefersColorSchemeModule.SetupModule("subscribe", _ => true);
        _ = PrefersColorSchemeModule.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        KeyboardModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js");
        _ = KeyboardModule.SetupVoid("focusElement", _ => true).SetVoidResult();
        _ = KeyboardModule.SetupVoid("registerShellKeyFilter", _ => true).SetVoidResult();
        _ = KeyboardModule.SetupVoid("registerPaletteKeyFilter", _ => true).SetVoidResult();
        _ = KeyboardModule.Setup<bool>("isEditableElementActive", _ => true).SetResult(false);

        FocusModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js");
        _ = FocusModule.SetupVoid("focusBodyIfNeeded", _ => true).SetVoidResult();

        // Story 3-2 Task 10 — Do NOT initialize the Fluxor store here. bUnit locks the service
        // container on first service resolution, which blocks derived tests from
        // Services.Replace(registry) / Services.Replace(ulidFactory) calls in their own
        // constructors. Store initialization happens on-demand from Render<T>() via EnsureStoreInitialized.
    }

    protected IThemeService ThemeService { get; }

    protected BunitJSModuleInterop BeforeUnloadModule { get; }

    protected BunitJSModuleInterop BeforeUnloadSubscription { get; }

    protected BunitJSModuleInterop PrefersColorSchemeModule { get; }

    protected BunitJSModuleInterop PrefersColorSchemeSubscription { get; }

    protected BunitJSModuleInterop KeyboardModule { get; }

    protected BunitJSModuleInterop FocusModule { get; }

    /// <summary>
    /// Initializes the Fluxor store exactly once per test context. Derived tests that call
    /// <c>Services.Replace(...)</c> in their own constructor MUST invoke this at the end of the
    /// constructor (after all replaces) so the store is wired with the final service graph.
    /// Idempotent; safe to call multiple times.
    /// </summary>
    protected void EnsureStoreInitialized() {
        if (_storeInitialized) {
            return;
        }

        _storeInitialized = true;
        IStore store = Services.GetRequiredService<IStore>();
        store.InitializeAsync().GetAwaiter().GetResult();
    }

    protected void DispatchTheme(ThemeValue theme) {
        EnsureStoreInitialized();
        Services.GetRequiredService<IDispatcher>().Dispatch(new ThemeChangedAction("test-correlation", theme));
    }
}
