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
public abstract class LayoutComponentTestBase : BunitContext
{
    protected LayoutComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddHexalithFrontComposerQuickstart();
        Services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ =>
        {
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

        InitializeStoreAsync().GetAwaiter().GetResult();
    }

    protected IThemeService ThemeService { get; }

    protected BunitJSModuleInterop BeforeUnloadModule { get; }

    protected BunitJSModuleInterop BeforeUnloadSubscription { get; }

    protected BunitJSModuleInterop PrefersColorSchemeModule { get; }

    protected BunitJSModuleInterop PrefersColorSchemeSubscription { get; }

    protected void DispatchTheme(ThemeValue theme)
        => Services.GetRequiredService<IDispatcher>().Dispatch(new ThemeChangedAction("test-correlation", theme));

    private async Task InitializeStoreAsync()
    {
        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
    }
}
