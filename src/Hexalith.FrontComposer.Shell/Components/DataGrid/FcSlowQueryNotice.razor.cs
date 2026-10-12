using System.Runtime.CompilerServices;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Shows a slow-query notice when a pending page request reaches
/// <see cref="FcShellOptions.SlowQueryThresholdMs"/> from its registration time.
/// The notice remains until that request settles; its timing and visibility are scoped
/// to the current view and the injected <see cref="TimeProvider"/>.
/// </summary>
public partial class FcSlowQueryNotice : ComponentBase, IDisposable {
    private static readonly ConditionalWeakTable<TaskCompletionSource<object>, string> RequestIdentities = new();
    private bool _visible;
    private readonly Dictionary<TaskCompletionSource<object>, DateTimeOffset> _startedAt = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<string> _announced = new(StringComparer.Ordinal);
    private TaskCompletionSource<object>? _earliest;
    private string? _earliestIdentity;
    private string? _activeSlowGroup;
    private IDisposable? _activeClaim;
    private string _boundViewKey = string.Empty;
    private ITimer? _slowTimer;
    private long _generation;
    private int _disposed;

    /// <summary>Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Displayed page offset from the grid's actual scroll position; null keeps standalone view-wide behavior.</summary>
    [Parameter]
    public int? VisiblePageSkip { get; set; }

    /// <summary>Uses the parent surface's persistent status node when true; direct callers own one by default.</summary>
    [Parameter]
    public bool SharedStatusOwner { get; set; }

    [Inject]
    private IState<LoadedPageState> LoadedPage { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    [Inject]
    private ISurfaceAnnouncementCoordinator Announcements { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        LoadedPage.StateChanged += OnStateChanged;
        ReconcileVisibility();
    }

    /// <inheritdoc />
    protected override void OnParametersSet() => ReconcileVisibility();

    private void OnStateChanged(object? sender, EventArgs e) {
        if (Volatile.Read(ref _disposed) != 0) {
            return;
        }
        _ = InvokeAsync(() => {
            if (Volatile.Read(ref _disposed) != 0) {
                return;
            }
            ReconcileVisibility();
            StateHasChanged();
        });
    }

    private void ReconcileVisibility() {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }
        if (!string.Equals(_boundViewKey, ViewKey, StringComparison.Ordinal))
        {
            _slowTimer?.Dispose();
            _slowTimer = null;
            ++_generation;
            CancelActiveSlow();
            _startedAt.Clear();
            _announced.Clear();
            _earliest = null;
            _earliestIdentity = null;
            _visible = false;
            _boundViewKey = ViewKey;
        }
        if (string.IsNullOrWhiteSpace(ViewKey)) {
            _visible = false;
            return;
        }

        KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>>[] pendingEntries = LoadedPage.Value.PendingCompletionsByKey
            .Where(entry => string.Equals(entry.Key.ViewKey, ViewKey, StringComparison.Ordinal)
                && (VisiblePageSkip is null || entry.Key.Skip == VisiblePageSkip.Value))
            .ToArray();
        var active = new HashSet<TaskCompletionSource<object>>(pendingEntries.Select(static entry => entry.Value), ReferenceEqualityComparer.Instance);
        HashSet<string> activeIdentities = new(pendingEntries.Select(entry => SpeechIdentity(entry.Key, entry.Value)), StringComparer.Ordinal);
        foreach (TaskCompletionSource<object> obsolete in _startedAt.Keys.Where(key => !active.Contains(key)).ToArray()) {
            _startedAt.Remove(obsolete);
        }
        _announced.RemoveWhere(identity => !activeIdentities.Contains(identity));
        if (_activeSlowGroup is not null && !activeIdentities.Any(identity =>
            _activeSlowGroup.Contains(":" + identity + ":observed:", StringComparison.Ordinal))) {
            CancelActiveSlow();
            _visible = false;
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> entry in pendingEntries) {
            _ = SurfaceAnnouncementGroupOwnership.For(Announcements)
                .ObserveRequest("projection:" + _boundViewKey, entry.Value);
            DateTimeOffset registeredAt = LoadedPage.Value.PendingStartedAtByCompletion.TryGetValue(entry.Value, out DateTimeOffset providerAnchor)
                ? providerAnchor : LoadedPage.Value.PendingStartedAtByKey.TryGetValue(entry.Key, out DateTimeOffset anchor)
                ? anchor : now;
            _startedAt.TryAdd(entry.Value, registeredAt);
        }
        KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> earliestEntry = pendingEntries.Length == 0
            ? default : pendingEntries.MinBy(entry => _startedAt[entry.Value]);
        _earliest = earliestEntry.Value;
        _earliestIdentity = _earliest is null ? null : SpeechIdentity(earliestEntry.Key, _earliest);
        _slowTimer?.Dispose();
        _slowTimer = null;
        long generation = ++_generation;
        if (_earliest is null) {
            _visible = false;
            return;
        }

        DateTimeOffset dueAt = _startedAt[_earliest].AddMilliseconds(ShellOptions.CurrentValue.SlowQueryThresholdMs);
        TimeSpan remaining = dueAt - now;
        if (remaining <= TimeSpan.Zero) {
            ShowSlowQuery(generation);
            return;
        }
        // A promoted co-mounted provider keeps the page's speech identity, but its
        // visible slow threshold belongs to its own registration time.
        _visible = false;
        CancelActiveSlow();
        _slowTimer = TimeProvider.CreateTimer(
            _ =>
            {
                if (Volatile.Read(ref _disposed) == 0)
                {
                    _ = InvokeAsync(() => ShowSlowQuery(generation));
                }
            },
            null,
            remaining,
            Timeout.InfiniteTimeSpan);
    }

    private void ShowSlowQuery(long generation) {
        if (Volatile.Read(ref _disposed) != 0 || generation != _generation || _earliest is null || _earliest.Task.IsCompleted) {
            return;
        }

        string requestIdentity = _earliestIdentity ?? RequestIdentities.GetValue(_earliest, static _ => Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        long requestOrder = SurfaceAnnouncementGroupOwnership.For(Announcements)
            .ObserveRequest("projection:" + _boundViewKey, _earliest);
        _visible = true;
        if (!_announced.Add(requestIdentity)) {
            StateHasChanged();
            return;
        }
        // A newly earlier request supersedes this component's old claim. Other mounted
        // owners retain their own claim on the previous request.
        CancelActiveSlow();
        _activeSlowGroup = "slow-query:" + _boundViewKey + ":scope:"
            + LoadedPage.Value.ScopeGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":started:"
            + _startedAt[_earliest].UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + requestIdentity + ":observed:"
            + requestOrder.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _activeClaim = SurfaceAnnouncementGroupOwnership.For(Announcements).Claim("projection:" + _boundViewKey, _activeSlowGroup);
        Announcements.Announce(
            "projection:" + ViewKey,
            _activeSlowGroup,
            requestIdentity,
            Localizer["Am08SlowQuery"],
            immediate: true);
        StateHasChanged();
    }

    private string SpeechIdentity((string ViewKey, int Skip) key, TaskCompletionSource<object> completion)
        => LoadedPage.Value.PendingSpeechIdentityByKey.TryGetValue(key, out string? identity)
            ? identity : RequestIdentities.GetValue(completion, static _ => Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));

    private void CancelActiveSlow()
    {
        if (_activeSlowGroup is { } group)
        {
            _activeClaim?.Dispose();
            _activeClaim = null;
            _activeSlowGroup = null;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }
        LoadedPage.StateChanged -= OnStateChanged;
        _slowTimer?.Dispose();
        CancelActiveSlow();
        GC.SuppressFinalize(this);
    }
}
