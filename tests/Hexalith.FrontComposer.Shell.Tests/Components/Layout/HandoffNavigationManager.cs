using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>A navigation manager that delivers location notifications on the caller's thread.</summary>
internal sealed class HandoffNavigationManager : NavigationManager {
    /// <summary>Starts at the application root.</summary>
    internal HandoffNavigationManager() => Initialize("http://localhost/", "http://localhost/");

    /// <inheritdoc />
    protected override void NavigateToCore(string uri, NavigationOptions options) {
        Uri = ToAbsoluteUri(uri).AbsoluteUri;
        NotifyLocationChanged(isInterceptedLink: false);
    }
}
