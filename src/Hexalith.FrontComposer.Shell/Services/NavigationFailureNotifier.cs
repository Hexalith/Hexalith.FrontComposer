using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Publishes one support-safe navigation failure message per failed activation.</summary>
public sealed class NavigationFailureNotifier
{
    private readonly IStringLocalizer<FcShellResources> localizer;
    private readonly IServiceProvider? _services;
    private int _focusOwners;

    /// <summary>Creates a notifier without host route metadata.</summary>
    public NavigationFailureNotifier(IStringLocalizer<FcShellResources> localizer) : this(localizer, null) { }

    /// <summary>Creates a notifier with the host's registered route and localization metadata.</summary>
    public NavigationFailureNotifier(IStringLocalizer<FcShellResources> localizer, IServiceProvider? services)
    {
        this.localizer = localizer;
        _services = services;
    }

    /// <summary>Registers the component which confirms rendered route headings.</summary>
    internal void RegisterFocusOwner() => _focusOwners++;

    /// <summary>Releases a mounted route-focus owner.</summary>
    internal void UnregisterFocusOwner() => _focusOwners = Math.Max(0, _focusOwners - 1);

    /// <summary>The destination retaining a tab-fallback announcement across a page change.</summary>
    internal string? TabFallbackDestination { get; set; }

    /// <summary>The support-safe tab-fallback announcement.</summary>
    internal string? TabFallbackMessage { get; set; }

    private readonly List<(string Uri, TaskCompletionSource<bool> Completion)> _pendingConfirmations = [];
    private readonly HashSet<Task<bool>> _cancelledConfirmations = [];
    private string? _failedReturnUri;
    private string? _activeAttemptUri;

    /// <summary>Identifies a navigation attempt, including retries of the same URI.</summary>
    public long AttemptVersion { get; private set; }

    /// <summary>Raised when the current route navigation status changes.</summary>
    public event Action? Changed;

    /// <summary>The latest support-safe message, if any.</summary>
    public string? Message { get; private set; }

    /// <summary>The safe label of the last rendered route.</summary>
    public string? CurrentPageLabel { get; private set; }

    /// <summary>The safe destination label for a navigation attempt.</summary>
    public string? DestinationLabel { get; private set; }

    /// <summary>The URI most recently confirmed by a rendered route heading.</summary>
    public string? LastConfirmedUri { get; private set; }

    /// <summary>Whether the current shell activation owns the supplied route.</summary>
    public bool OwnsAttempt(string routeUri)
        => _activeAttemptUri is not null && MatchesAttempt(_activeAttemptUri, routeUri);

    /// <summary>Clears route recovery and pending work when the tenant or user changes.</summary>
    public void ResetForScopeChange()
    {
        AttemptVersion++;
        foreach ((string _, TaskCompletionSource<bool> completion) in _pendingConfirmations)
        {
            completion.TrySetResult(false);
        }

        _pendingConfirmations.Clear();
        _cancelledConfirmations.Clear();
        _activeAttemptUri = null;
        _failedReturnUri = null;
        LastConfirmedUri = null;
        TabFallbackDestination = null;
        TabFallbackMessage = null;
        CurrentPageLabel = null;
        DestinationLabel = null;
        Clear();
    }

    /// <summary>Records a rendered route for recovery if a later route has no match.</summary>
    /// <remarks>
    /// The current page label comes from registry-owned navigation metadata, never from a rendered
    /// heading, because route headings can contain customer names or identifiers.
    /// </remarks>
    public void RememberSuccessfulRoute()
    {
        CurrentPageLabel = SafeLabel(LastConfirmedUri, "RouteNavigationCurrentPageFallbackLabel");
        DestinationLabel = null;
        Clear();
    }

    /// <summary>Records a heading that belongs to the rendered destination URI.</summary>
    public void ConfirmRoute(string routeUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeUri);
        LastConfirmedUri = routeUri;
        _activeAttemptUri = null;
        foreach ((string requestedUri, TaskCompletionSource<bool> completion) in _pendingConfirmations.ToArray())
        {
            if (string.Equals(requestedUri, routeUri, StringComparison.OrdinalIgnoreCase)
                || IsCanonicalChildRoute(requestedUri, routeUri))
            {
                completion.TrySetResult(true);
                _pendingConfirmations.Remove((requestedUri, completion));
            }
        }

        if (!string.Equals(_failedReturnUri, routeUri, StringComparison.OrdinalIgnoreCase))
        {
            _failedReturnUri = null;
            RememberSuccessfulRoute();
        }
    }

    /// <summary>Waits for the requested route to render a heading or report failure.</summary>
    public Task<bool> PrepareRouteConfirmation(string routeUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeUri);
        // Multiple activations of the same result own separate completions. A later
        // activation must not turn the first one into a false navigation failure.
        foreach ((string requestedUri, TaskCompletionSource<bool> completion) in _pendingConfirmations.ToArray())
        {
            if (!string.Equals(requestedUri, routeUri, StringComparison.OrdinalIgnoreCase))
            {
                completion.TrySetResult(false);
                _pendingConfirmations.Remove((requestedUri, completion));
            }
        }

        TaskCompletionSource<bool> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingConfirmations.Add((routeUri, pending));
        return pending.Task;
    }

    /// <summary>Starts a shell-owned navigation attempt for the supplied absolute route URI.</summary>
    /// <remarks>
    /// The destination label comes from registry-owned navigation metadata for the route; the URI
    /// itself never appears in a message.
    /// </remarks>
    public void BeginAttempt(string? routeUri)
    {
        AttemptVersion++;
        _activeAttemptUri = routeUri;
        _failedReturnUri = null;
        DestinationLabel = SafeLabel(routeUri, "RouteNavigationUnknownDestinationLabel");
        // A repeated failure must insert fresh live-region text for this attempt.
        Clear();
    }

    /// <summary>Settles a shell-owned attempt whose navigation another handler prevented.</summary>
    /// <remarks>
    /// A navigation lock such as the form-abandonment guard cancels the location change, so no
    /// route renders. The attempt ends without a failure message; the caller that prepared the
    /// confirmation can tell the cancellation apart through <see cref="ConsumeCancellation"/>.
    /// </remarks>
    internal void CancelAttempt(string routeUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeUri);
        if (_activeAttemptUri is not null && MatchesAttempt(_activeAttemptUri, routeUri))
        {
            _activeAttemptUri = null;
            DestinationLabel = null;
        }

        foreach ((string requestedUri, TaskCompletionSource<bool> completion) in _pendingConfirmations.ToArray())
        {
            if (!MatchesAttempt(requestedUri, routeUri))
            {
                continue;
            }

            _cancelledConfirmations.Add(completion.Task);
            completion.TrySetResult(false);
            _pendingConfirmations.Remove((requestedUri, completion));
        }
    }

    /// <summary>Whether the confirmation settled because its navigation was prevented; consumes that record.</summary>
    internal bool ConsumeCancellation(Task<bool> confirmation) => _cancelledConfirmations.Remove(confirmation);

    /// <summary>Retires confirmations when another browser or app navigation supersedes them.</summary>
    public void ObserveLocation(string routeUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeUri);
        if (TabFallbackDestination is not null && Uri.TryCreate(routeUri, UriKind.Absolute, out Uri? location)
            && !string.Equals(location.AbsolutePath.TrimEnd('/'), TabFallbackDestination.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            TabFallbackDestination = null;
            TabFallbackMessage = null;
        }
        if (_activeAttemptUri is not null
            && !MatchesAttempt(_activeAttemptUri, routeUri))
        {
            _activeAttemptUri = null;
        }

        foreach ((string requestedUri, TaskCompletionSource<bool> completion) in _pendingConfirmations.ToArray())
        {
            if (MatchesAttempt(requestedUri, routeUri))
            {
                continue;
            }

            completion.TrySetResult(false);
            _pendingConfirmations.Remove((requestedUri, completion));
        }

        if (_focusOwners == 0 && OwnsAttempt(routeUri))
        {
            ConfirmRoute(routeUri);
        }
    }

    /// <summary>Returns the prior confirmed page only for this shell-owned unmatched activation.</summary>
    public string? ReturnUriForActivatedUnmatched(string routeUri)
        => OwnsAttempt(routeUri)
            ? LastConfirmedUri
            : null;

    /// <summary>Reports an unmatched activation while retaining its message on the recovered route.</summary>
    public void ReportFailureAndPreserveReturn(string returnUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(returnUri);
        ReportFailure();
        _failedReturnUri = returnUri;
    }

    /// <summary>Whether route focus should leave an open palette in place after failure recovery.</summary>
    public bool IsFailedReturn(string routeUri)
        => string.Equals(_failedReturnUri, routeUri, StringComparison.OrdinalIgnoreCase);

    /// <summary>Stops retaining a route when its recovery navigation cannot start.</summary>
    public void CancelFailedReturn() => _failedReturnUri = null;

    /// <summary>Reports a failed route activation without exposing its URL or exception.</summary>
    public void ReportFailure()
    {
        _activeAttemptUri = null;
        _failedReturnUri = null;
        string destination = DestinationLabel ?? localizer["RouteNavigationUnknownDestinationLabel"].Value;
        string? currentUri = _services?.GetService<NavigationManager>()?.Uri;
        bool remainsOnPage = LastConfirmedUri is not null && string.Equals(currentUri, LastConfirmedUri, StringComparison.OrdinalIgnoreCase);
        Message = remainsOnPage
            ? localizer["RouteNavigationFailedNamedText", destination, CurrentPageLabel ?? localizer["RouteNavigationCurrentPageFallbackLabel"].Value].Value
            : localizer["RouteNavigationFailedText", destination].Value;
        DestinationLabel = null;
        foreach ((string requestedUri, TaskCompletionSource<bool> completion) in _pendingConfirmations)
        {
            completion.TrySetResult(false);
        }
        _pendingConfirmations.Clear();
        Changed?.Invoke();
    }

    /// <summary>Whether this URI declares a canonical child route which must finish rendering.</summary>
    internal bool IsCanonicalAlias(string routeUri) => CanonicalPath(routeUri) is not null;

    private string? CanonicalPath(string requestedUri)
    {
        if (!Uri.TryCreate(requestedUri, UriKind.Absolute, out Uri? requested))
        {
            return null;
        }
        return (_services?.GetService<IFrontComposerRegistry>()?.GetManifests() ?? [])
            .SelectMany(manifest => manifest.CanonicalRouteAliases)
            // The alias matching the most query selectors is the most specific one.
            .OrderByDescending(pair => SelectorCount(pair.Key))
            .ThenByDescending(pair => pair.Key.Length)
            .Where(pair => {
                string[] alias = pair.Key.Split('?', 2);
                return string.Equals(alias[0].TrimEnd('/'), requested.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                    && (alias.Length == 1 || QuerySelectorsMatch(alias[1], requested.Query));
            })
            .Select(pair => pair.Value)
            .FirstOrDefault();
    }

    private static int SelectorCount(string alias)
        => alias.Split('?', 2) is [_, string selectors]
            ? QueryHelpers.ParseQuery(selectors).Sum(selector => selector.Value.Count)
            : 0;

    private static bool QuerySelectorsMatch(string selectors, string requestedQuery)
    {
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(requestedQuery);
        return QueryHelpers.ParseQuery(selectors).All(selector =>
            query.TryGetValue(selector.Key, out Microsoft.Extensions.Primitives.StringValues values)
            && selector.Value.All(value => values.Contains(value, StringComparer.OrdinalIgnoreCase)));
    }

    private bool IsCanonicalChildRoute(string requestedUri, string renderedUri)
        => Uri.TryCreate(requestedUri, UriKind.Absolute, out Uri? requested)
            && Uri.TryCreate(renderedUri, UriKind.Absolute, out Uri? rendered)
            && string.Equals(requested.Scheme, rendered.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(requested.Authority, rendered.Authority, StringComparison.OrdinalIgnoreCase)
            && CanonicalPath(requestedUri) is { } canonical
            && string.Equals(rendered.AbsolutePath, canonical, StringComparison.OrdinalIgnoreCase);

    private bool MatchesAttempt(string requestedUri, string routeUri)
        => string.Equals(requestedUri, routeUri, StringComparison.OrdinalIgnoreCase)
            || IsCanonicalChildRoute(requestedUri, routeUri);

    private string SafeLabel(string? routeUri, string fallback)
    {
        IFrontComposerRegistry? registry = _services?.GetService<IFrontComposerRegistry>();
        IStringLocalizerFactory? factory = _services?.GetService<IStringLocalizerFactory>();
        if (registry is not null && Uri.TryCreate(routeUri, UriKind.Absolute, out Uri? route))
        {
            string path = route.AbsolutePath.TrimEnd('/');
            FrontComposerNavEntry? entry = registry.GetNavEntries().FirstOrDefault(candidate =>
                string.Equals(candidate.Href?.Split('?', '#')[0].TrimEnd('/'), path, StringComparison.OrdinalIgnoreCase));
            if (entry is not null)
            {
                return factory is not null && entry.Resource is not null && entry.TitleKey is not null
                    ? factory.Create(entry.Resource)[entry.TitleKey].Value : entry.Title;
            }
            DomainManifest? manifest = registry.GetManifests().FirstOrDefault(candidate =>
                path.Equals("/" + candidate.BoundedContext, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/" + candidate.BoundedContext + "/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/commands/" + candidate.BoundedContext + "/", StringComparison.OrdinalIgnoreCase));
            if (manifest is not null)
            {
                return factory is not null && manifest.Resource is not null && manifest.NameKey is not null
                    ? factory.Create(manifest.Resource)[manifest.NameKey].Value : manifest.Name;
            }
        }
        return localizer[fallback].Value;
    }

    /// <summary>Clears the message after successful navigation.</summary>
    public void Clear()
    {
        if (Message is null)
        {
            return;
        }

        Message = null;
        Changed?.Invoke();
    }

}
