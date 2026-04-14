using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests;

/// <summary>
/// Base test class that pre-configures Fluxor, storage, and override registry for bUnit tests.
/// The Fluxor store is initialized during construction, and <see cref="InitializeStoreAsync"/>
/// remains safe to call when a test wants explicit sequencing.
/// </summary>
public abstract class FrontComposerTestBase : BunitContext {
    private bool _storeInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontComposerTestBase"/> class.
    /// Registers all services needed for Shell component testing and initializes the Fluxor store.
    /// </summary>
    protected FrontComposerTestBase() {
        _ = Services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly));
        _ = Services.AddSingleton<IStorageService, InMemoryStorageService>();
        _ = Services.AddSingleton(Substitute.For<IOverrideRegistry>());
        _ = Services.AddLogging();
        InitializeStoreAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Initializes the Fluxor store if it has not already been initialized.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task InitializeStoreAsync() {
        if (_storeInitialized) {
            return;
        }

        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
        _storeInitialized = true;
    }
}
