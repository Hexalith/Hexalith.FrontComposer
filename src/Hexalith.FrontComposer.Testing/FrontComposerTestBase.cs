using Bunit;

using Fluxor;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Optional inheritance-based bUnit base class for generated FrontComposer component tests.
/// </summary>
public abstract class FrontComposerTestBase : BunitContext {
    private bool _storeInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontComposerTestBase"/> class.
    /// </summary>
    protected FrontComposerTestBase()
        : this(_ => { }) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontComposerTestBase"/> class.
    /// </summary>
    /// <param name="configure">Optional host configuration callback.</param>
    protected FrontComposerTestBase(Action<FrontComposerTestOptions> configure) {
        ArgumentNullException.ThrowIfNull(configure);
        Host = Services.AddFrontComposerTestHost(this, options => {
            configure(options);
            if (options.StoreInitialization == StoreInitializationMode.DuringHostSetup) {
                throw new InvalidOperationException(
                    "FrontComposerTestBase constructors cannot initialize the store asynchronously. Use StoreInitializationMode.OnDemand and await InitializeStoreAsync() from test initialization.");
            }
        });
        _storeInitialized = Host.StoreInitialized;
    }

    /// <summary>Gets the composable host builder used by this test instance.</summary>
    protected FrontComposerTestHostBuilder Host { get; }

    /// <summary>Gets the mutable fake user context registered for this test instance.</summary>
    protected FrontComposerTestUserContextAccessor UserContext => Host.UserContext;

    /// <summary>Gets the fake command service registered for this test instance.</summary>
    protected TestCommandService CommandService => Host.CommandService;

    /// <summary>Gets the fake query service registered for this test instance.</summary>
    protected TestQueryService QueryService => Host.QueryService;

    /// <summary>Gets the fake projection page loader registered for this test instance.</summary>
    protected TestProjectionPageLoader PageLoader => Host.PageLoader;

    /// <summary>
    /// Initializes the Fluxor store once. Repeated calls are safe and preserve existing state.
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

    /// <inheritdoc />
    protected override void Dispose(bool disposing) {
        if (disposing) {
            Host.Dispose();
        }

        base.Dispose(disposing);
    }
}
