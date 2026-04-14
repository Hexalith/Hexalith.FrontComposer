namespace Hexalith.FrontComposer.Shell.Tests.Generated;

using System.Reflection;

using Bunit;

using Fluxor;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

public abstract class GeneratedComponentTestBase : BunitContext
{
    private bool _storeInitialized;

    protected GeneratedComponentTestBase(params Assembly[] scanAssemblies)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
        Services.AddLogging();
        Services.AddFluxor(o =>
        {
            foreach (Assembly assembly in scanAssemblies)
            {
                o.ScanAssemblies(assembly);
            }
        });
    }

    protected async Task InitializeStoreAsync()
    {
        if (_storeInitialized)
        {
            return;
        }

        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(false);
        _storeInitialized = true;
    }
}
