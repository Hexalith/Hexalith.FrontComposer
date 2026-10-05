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
    private bool _visible;
    private readonly Dictionary<TaskCompletionSource<object>, DateTimeOffset> _startedAt = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TaskCompletionSource<object>> _announced = new(ReferenceEqualityComparer.Instance);
    private TaskCompletionSource<object>? _earliest;
    private string? _activeSlowGroup;
    private ITimer? _slowTimer;
    private long _generation;
    private int _disposed;

    /// <summary>Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

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
                Announcements.Cancel("projection:" + ViewKey, group);
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
            _ => _ = InvokeAsync(() => ShowSlowQuery(generation)),
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
        _activeSlowGroup = "slow-query:" + ViewKey + ":" + generation.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Announcements.Announce(
            "projection:" + ViewKey,
            _activeSlowGroup,
            generation.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Localizer["Am08SlowQuery"],
            immediate: true);
        StateHasChanged();
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }
        LoadedPage.StateChanged -= OnStateChanged;
        _slowTimer?.Dispose();
        if (_activeSlowGroup is { } group) {
            Announcements.Cancel("projection:" + ViewKey, group);
        }
        GC.SuppressFinalize(this);
    }
}
