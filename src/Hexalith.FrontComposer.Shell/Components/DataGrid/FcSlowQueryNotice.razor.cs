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
/// Story 4-4 T1.2 / D8 / D11 / AC3 — auto-dismissing <c>FluentMessageBar Intent="Info"</c> surfaced
/// when <c>LoadedPageState.LastElapsedMsByKey[ViewKey]</c> exceeds
/// <c>FcShellOptions.SlowQueryThresholdMs</c>. Auto-dismisses after 5 s via the injected
/// <see cref="TimeProvider"/> (<c>FakeTimeProvider.Advance(5_000)</c> in tests); dismiss state is
/// scoped to this component instance (not persisted across navigations).
/// </summary>
public partial class FcSlowQueryNotice : ComponentBase, IDisposable {
    private static readonly ConditionalWeakTable<TaskCompletionSource<object>, string> RequestIdentities = new();
    private bool _visible;
    private readonly Dictionary<TaskCompletionSource<object>, DateTimeOffset> _startedAt = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TaskCompletionSource<object>> _announced = new(ReferenceEqualityComparer.Instance);
    private TaskCompletionSource<object>? _earliest;
    private string? _activeSlowGroup;
    private string _boundViewKey = string.Empty;
    private ITimer? _slowTimer;
    private long _generation;
    private int _disposed;

    /// <summary>Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

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
            _visible = false;
            _boundViewKey = ViewKey;
        }
        if (string.IsNullOrWhiteSpace(ViewKey)) {
            _visible = false;
            return;
        }

        KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>>[] pendingEntries = LoadedPage.Value.PendingCompletionsByKey
            .Where(entry => string.Equals(entry.Key.ViewKey, ViewKey, StringComparison.Ordinal))
            .ToArray();
        TaskCompletionSource<object>[] pending = pendingEntries.Select(static entry => entry.Value).ToArray();
        var active = new HashSet<TaskCompletionSource<object>>(pending, ReferenceEqualityComparer.Instance);
        foreach (TaskCompletionSource<object> obsolete in _startedAt.Keys.Where(key => !active.Contains(key)).ToArray()) {
            _startedAt.Remove(obsolete);
            _announced.Remove(obsolete);
            if (ReferenceEquals(obsolete, _earliest) && _activeSlowGroup is { } group) {
                Announcements.Cancel("projection:" + _boundViewKey, group);
                _activeSlowGroup = null;
                _visible = false;
            }
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> entry in pendingEntries) {
            DateTimeOffset registeredAt = LoadedPage.Value.PendingStartedAtByKey.TryGetValue(entry.Key, out DateTimeOffset anchor)
                ? anchor : now;
            _startedAt.TryAdd(entry.Value, registeredAt);
        }
        _earliest = _startedAt.Count == 0 ? null : _startedAt.MinBy(static entry => entry.Value).Key;
        _slowTimer?.Dispose();
        _slowTimer = null;
        long generation = ++_generation;
        if (_earliest is null) {
            _visible = false;
            return;
        }

        if (_announced.Contains(_earliest)) {
            return;
        }

        DateTimeOffset dueAt = _startedAt[_earliest].AddMilliseconds(ShellOptions.CurrentValue.SlowQueryThresholdMs);
        TimeSpan remaining = dueAt - now;
        if (remaining <= TimeSpan.Zero) {
            ShowSlowQuery(generation);
            return;
        }
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

        _announced.Add(_earliest);
        _visible = true;
        string requestIdentity = RequestIdentities.GetValue(_earliest, static _ => Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        _activeSlowGroup = "slow-query:" + _boundViewKey + ":" + requestIdentity;
        Announcements.Announce(
            "projection:" + ViewKey,
            _activeSlowGroup,
            requestIdentity,
            Localizer["Am08SlowQuery"],
            immediate: true);
        StateHasChanged();
    }

    private void CancelActiveSlow()
    {
        if (_activeSlowGroup is { } group)
        {
            Announcements.Cancel("projection:" + _boundViewKey, group);
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
