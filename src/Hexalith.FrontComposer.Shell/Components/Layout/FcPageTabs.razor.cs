using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;

// Component callbacks must resume on Blazor's synchronization context.
#pragma warning disable CA2007

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Renders page-level tabs whose <see cref="FcPageTab"/> children own their associated panel content.
/// Place the component in the page body, after the page header, so Fluent UI can keep every tab and
/// generated tab panel in one semantic contract.
/// </summary>
public sealed partial class FcPageTabs : ComponentBase, IAsyncDisposable
{
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private readonly List<FcPageTab> _tabs = [];
    private IJSObjectReference? _focusModule;
    private string? _announcedFallback;
    private string? _pendingFallback;
    private string? _fallbackDestination;
    private string? _fallbackStatus;
    private bool _disposed;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject] private NavigationFailureNotifier NavigationFailure { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private IReadOnlyList<FcPageTab> Tabs => _tabs;

    /// <summary>Currently active tab id.</summary>
    [Parameter]
    public string? ActiveTabId { get; set; }

    /// <summary>The Module route prefix. Set this to enable route-backed tabs.</summary>
    [Parameter]
    public string? ModuleRoute { get; set; }

    /// <summary>The declared default tab id required for route-backed tabs.</summary>
    [Parameter]
    public string? DefaultTabId { get; set; }

    /// <summary>Raised when Fluent UI changes the active tab id.</summary>
    [Parameter]
    public EventCallback<string?> ActiveTabIdChanged { get; set; }

    /// <summary>Accessible name for the page tab list.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Page sections";

    /// <summary>Stable selector applied to the Fluent tabs root.</summary>
    [Parameter]
    public string TestId { get; set; } = "fc-page-tabs";

    /// <summary>Fluent tab appearance. Defaults to the subtle page-level treatment.</summary>
    [Parameter]
    public TabsAppearance? Appearance { get; set; } = TabsAppearance.Subtle;

    /// <summary>Whether the entire tab set is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Fluent tab orientation. Defaults to horizontal.</summary>
    [Parameter]
    public Orientation? Orientation { get; set; } = Microsoft.FluentUI.AspNetCore.Components.Orientation.Horizontal;

    /// <summary>Width applied to the Fluent tabs root.</summary>
    [Parameter]
    public string? Width { get; set; } = "100%";

    /// <summary>Caller-owned <see cref="FcPageTab"/> children.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string? SelectedTabId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModuleRoute))
            {
                return ActiveTabId;
            }

            if (string.IsNullOrWhiteSpace(DefaultTabId))
            {
                throw new InvalidOperationException($"{nameof(DefaultTabId)} is required when {nameof(ModuleRoute)} is set.");
            }

            FcPageTab? declaredDefault = _tabs.FirstOrDefault(tab => string.Equals(tab.Id, DefaultTabId, StringComparison.OrdinalIgnoreCase) && !tab.Disabled);
            if (declaredDefault is null)
            {
                if (_tabs.Count > 0)
                {
                    throw new InvalidOperationException($"An enabled {nameof(DefaultTabId)} tab must be declared for route-backed tabs.");
                }

                return null;
            }

            string path = Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0].Trim('/');
            string prefix = ModuleRoute.Trim('/');
            if (string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase))
            {
                _pendingFallback = null;
                _announcedFallback = null;
                _fallbackStatus = null;
                _fallbackDestination = null;
                return declaredDefault.Id;
            }

            if (!path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                return declaredDefault.Id;
            }

            string requested = path[(prefix.Length + 1)..];
            FcPageTab? matching = _tabs.FirstOrDefault(tab => string.Equals(tab.Id, requested, StringComparison.OrdinalIgnoreCase) && !tab.Disabled);
            if (matching is not null)
            {
                _pendingFallback = null;
                _announcedFallback = null;
                if (!string.Equals("/" + path, _fallbackDestination, StringComparison.OrdinalIgnoreCase))
                {
                    _fallbackStatus = null;
                    _fallbackDestination = null;
                }
                return matching.Id;
            }

            _pendingFallback = path;
            if (!string.Equals(_announcedFallback, path, StringComparison.Ordinal))
            {
                _fallbackDestination = $"/{prefix}/{declaredDefault.Id}";
                _fallbackStatus = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    Localizer["ModuleTabUnavailableTemplate"].Value, declaredDefault.Header);
            }
            return declaredDefault.Id;
        }
    }

    private string? FallbackMessage => _fallbackStatus;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(ModuleRoute) && string.IsNullOrWhiteSpace(DefaultTabId))
        {
            throw new InvalidOperationException($"{nameof(DefaultTabId)} is required when {nameof(ModuleRoute)} is set.");
        }

        if (!string.IsNullOrWhiteSpace(ModuleRoute))
        {
            ValidateRouteTabId(DefaultTabId);
            foreach (FcPageTab tab in _tabs)
            {
                ValidateRouteTabId(tab.Id);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        string renderUri = Navigation.Uri;
        if (!string.IsNullOrWhiteSpace(ModuleRoute) && !_tabs.Any(tab => string.Equals(tab.Id, DefaultTabId, StringComparison.OrdinalIgnoreCase) && !tab.Disabled))
        {
            throw new InvalidOperationException($"An enabled {nameof(DefaultTabId)} tab must be declared for route-backed tabs.");
        }

        if (_tabs.Count > 0)
        {
            try
            {
                _focusModule ??= await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
                await _focusModule.InvokeVoidAsync("labelTabPanels", TestId);
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException)
            {
                // Fluent tab selection remains usable if panel-label interop is unavailable.
            }
        }

        if (_disposed || !string.Equals(renderUri, Navigation.Uri, StringComparison.Ordinal)
            || _pendingFallback is not { } fallback
            || string.Equals(_announcedFallback, fallback, StringComparison.Ordinal))
        {
            return;
        }

        string currentPath = Navigation.ToBaseRelativePath(renderUri).Split('?', '#')[0].Trim('/');
        if (!string.Equals(currentPath, fallback, StringComparison.Ordinal))
        {
            return;
        }

        _announcedFallback = fallback;
        string destination = $"/{ModuleRoute!.Trim('/')}/{DefaultTabId}";
        try
        {
            NavigationFailure.BeginAttempt(_tabs.FirstOrDefault(t => string.Equals(t.Id, DefaultTabId, StringComparison.Ordinal))?.Header,
                Navigation.ToAbsoluteUri(destination).ToString());
            Navigation.NavigateTo(destination, replace: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            NavigationFailure.ReportFailure();
        }
    }

    /// <summary>Registers one declarative page-tab child in render order.</summary>
    /// <param name="tab">The child descriptor to register.</param>
    internal void Register(FcPageTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (!string.IsNullOrWhiteSpace(ModuleRoute))
        {
            ValidateRouteTabId(tab.Id);
        }

        if (_tabs.Contains(tab))
        {
            return;
        }

        if (_tabs.Any(existing => string.Equals(existing.Id, tab.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A page tab with id '{tab.Id}' is already registered.");
        }

        _tabs.Add(tab);
        _ = InvokeAsync(StateHasChanged);
    }

    private static void ValidateRouteTabId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id is "." or ".."
            || !id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or '~'))
        {
            throw new InvalidOperationException($"Route-backed page tab id '{id}' must be one safe path segment.");
        }
    }

    private async Task OnActiveTabIdChangedAsync(string? activeTabId)
    {
        if (_disposed)
        {
            return;
        }

        // FluentTabs also raises this callback while registering or disposing tabs.
        // A value already selected by the route is not a user tab change.
        if (string.Equals(activeTabId, SelectedTabId, StringComparison.Ordinal))
        {
            return;
        }

        FcPageTab? selected = _tabs.FirstOrDefault(tab => string.Equals(tab.Id, activeTabId, StringComparison.Ordinal) && !tab.Disabled);
        if (selected is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(ModuleRoute))
        {
            string callbackOrigin = Navigation.Uri;
            string desired = $"/{ModuleRoute.Trim('/')}/{selected.Id}";
            string current = "/" + Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0].Trim('/');
            string callbackOriginPath = current;
            string modulePath = "/" + ModuleRoute.Trim('/');
            if (!string.Equals(current, modulePath, StringComparison.OrdinalIgnoreCase)
                && !current.StartsWith(modulePath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.Equals(current, desired, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _focusModule ??= await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
                    await _focusModule.InvokeVoidAsync("prepareTabNavigation", desired, selected.Id);
                }
                catch (Exception ex) when (ex is InvalidOperationException or JSException or JSDisconnectedException or OperationCanceledException)
                {
                    // Route selection still works when optional focus interop is unavailable.
                }
            }

            if (_disposed || !string.Equals(callbackOrigin, Navigation.Uri, StringComparison.Ordinal))
            {
                return;
            }

            await ActiveTabIdChanged.InvokeAsync(selected.Id);
            _fallbackStatus = null;
            _fallbackDestination = null;

            // An adopter may navigate with panel-owned query state. Do not replace that
            // destination with the bare tab URL after its callback completes.
            current = "/" + Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0].Trim('/');
            if (_disposed || (!string.Equals(current, modulePath, StringComparison.OrdinalIgnoreCase)
                && !current.StartsWith(modulePath + "/", StringComparison.OrdinalIgnoreCase))
                || (!string.Equals(current, callbackOriginPath, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(current, desired, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (!string.Equals(current, desired, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    NavigationFailure.BeginAttempt(selected.Header, Navigation.ToAbsoluteUri(desired).ToString());
                    Navigation.NavigateTo(desired);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    NavigationFailure.ReportFailure();
                }
            }
        }
        else
        {
            await ActiveTabIdChanged.InvokeAsync(selected.Id);
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Navigation.LocationChanged -= OnLocationChanged;
        if (_focusModule is not null)
        {
            try
            {
                await _focusModule.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
            {
                // The browser circuit can end before disposal.
            }
        }

        GC.SuppressFinalize(this);
    }
}
