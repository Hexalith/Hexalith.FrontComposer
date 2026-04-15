using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public abstract class GeneratedComponentTestBase : BunitContext {
    private bool _storeInitialized;

    protected GeneratedComponentTestBase(params Assembly[] scanAssemblies) {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluxor(o => {
            foreach (Assembly assembly in scanAssemblies) {
                _ = o.ScanAssemblies(assembly);
            }
        });

        // Zero-delay stub so bUnit tests stay deterministic under CI load.
        _ = Services.Configure<StubCommandServiceOptions>(o => {
            o.AcknowledgeDelayMs = 0;
            o.SyncingDelayMs = 0;
            o.ConfirmDelayMs = 0;
        });
        _ = Services.AddScoped<ICommandService, StubCommandService>();
    }

    protected async Task InitializeStoreAsync() {
        if (_storeInitialized) {
            return;
        }

        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
        _storeInitialized = true;
    }
}
