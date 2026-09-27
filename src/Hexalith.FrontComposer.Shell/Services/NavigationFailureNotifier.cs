using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Publishes one support-safe navigation failure message per failed activation.</summary>
public sealed class NavigationFailureNotifier(IStringLocalizer<FcShellResources> localizer)
{
    private readonly List<(string Uri, TaskCompletionSource<bool> Completion)> _pendingConfirmations = [];
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
        _activeAttemptUri = null;
        _failedReturnUri = null;
        LastConfirmedUri = null;
        CurrentPageLabel = null;
        DestinationLabel = null;
        Clear();
    }

    /// <summary>Records a rendered route for recovery if a later route has no match.</summary>
    public void RememberSuccessfulRoute(string? pageLabel)
    {
        // Route headings can contain customer names or identifiers. Keep only fixed copy in
        // navigation status, even when a caller supplies a rendered heading.
        CurrentPageLabel = localizer["RouteNavigationCurrentPageFallbackLabel"].Value;
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
            RememberSuccessfulRoute(null);
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

    /// <summary>Records a caller-owned visible destination label without storing a route in a message.</summary>
    public void BeginAttempt(string? destinationLabel, string? routeUri = null)
    {
        AttemptVersion++;
        _activeAttemptUri = routeUri;
        _failedReturnUri = null;
        DestinationLabel = localizer["RouteNavigationUnknownDestinationLabel"].Value;
        // A repeated failure must insert fresh live-region text for this attempt.
        Clear();
    }

    /// <summary>Retires confirmations when another browser or app navigation supersedes them.</summary>
    public void ObserveLocation(string routeUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeUri);
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
    public void ReportFailure(string? destinationLabel = null)
    {
        _activeAttemptUri = null;
        _failedReturnUri = null;
        Message = localizer["RouteNavigationFailedText"].Value;
        DestinationLabel = null;
        foreach ((string requestedUri, TaskCompletionSource<bool> completion) in _pendingConfirmations)
        {
            completion.TrySetResult(false);
        }
        _pendingConfirmations.Clear();
        Changed?.Invoke();
    }

    private static bool IsCanonicalChildRoute(string requestedUri, string renderedUri)
    {
        if (!Uri.TryCreate(requestedUri, UriKind.Absolute, out Uri? requested)
            || !Uri.TryCreate(renderedUri, UriKind.Absolute, out Uri? rendered)
            || !string.Equals(requested.Scheme, rendered.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(requested.Authority, rendered.Authority, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Only the legacy Tenants workspace alias has a trusted query-to-tab
        // canonicalization. Opaque /tenants/{TenantId} detail URLs must never
        // complete a pending workspace activation.
        if (!string.Equals(requested.AbsolutePath.TrimEnd('/'), "/tenants", StringComparison.OrdinalIgnoreCase)
            && requested.AbsolutePath != "/")
        {
            return false;
        }

        string? tab = requested.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => string.Equals(Uri.UnescapeDataString(parts[0]), "tab", StringComparison.OrdinalIgnoreCase))
            .Select(parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty)
            .FirstOrDefault();
        string expectedPath = string.Equals(tab, "users", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tab, "workspace-users", StringComparison.OrdinalIgnoreCase)
            ? "/tenants/workspace-users"
            : "/tenants/tenants";
        return string.Equals(rendered.AbsolutePath, expectedPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAttempt(string requestedUri, string routeUri)
        => string.Equals(requestedUri, routeUri, StringComparison.OrdinalIgnoreCase)
            || IsCanonicalChildRoute(requestedUri, routeUri);

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
