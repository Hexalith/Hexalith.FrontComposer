using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests;

/// <summary>
/// Base test class that pre-configures Fluxor, storage, a stub <see cref="IUserContextAccessor"/>,
/// and the override registry for bUnit tests. The Fluxor store is initialized during construction,
/// and <see cref="InitializeStoreAsync"/> remains safe to call when a test wants explicit sequencing.
/// </summary>
public abstract class FrontComposerTestBase : BunitContext {
    /// <summary>Tenant segment used by the test-scoped <see cref="IUserContextAccessor"/>.</summary>
    public const string TestTenantId = "test-tenant";

    /// <summary>User segment used by the test-scoped <see cref="IUserContextAccessor"/>.</summary>
    public const string TestUserId = "test-user";

    private bool _storeInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontComposerTestBase"/> class.
    /// Registers all services needed for Shell component testing and initializes the Fluxor store.
    /// </summary>
    protected FrontComposerTestBase() {
        _ = Services.AddFluxor(o => o.ScanAssemblies(typeof(FrontComposerThemeState).Assembly));
        _ = Services.AddScoped<IStorageService, InMemoryStorageService>();
        _ = Services.AddScoped<IUserContextAccessor>(_ => {
            IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
            accessor.TenantId.Returns(TestTenantId);
            accessor.UserId.Returns(TestUserId);
            return accessor;
        });
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
