using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Badges;

/// <summary>
/// Default <see cref="IBadgeCountService"/> implementation (Story 3-5 D1 / D4 / D5 / D6 / D7 / D20).
/// Fans out an initial parallel fetch across
/// <see cref="IActionQueueProjectionCatalog.ActionQueueTypes"/> under a single 5-second umbrella
/// timeout, then bridges <see cref="IProjectionChangeNotifier"/> events into per-type re-fetches.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lifetime:</b> Scoped — per-circuit in Blazor Server, per-user in WASM. A Singleton lifetime
/// would leak per-user <see cref="CountChanged"/> subscriptions across circuits and cause
/// <see cref="InvalidOperationException"/> from <c>StateHasChanged</c> calls into disposed render
/// trees (ADR-044).
/// </para>
/// <para>
/// <b>Ordering contract (D6):</b> <see cref="CountChanged"/> provides <i>last-writer-wins per
/// projection type</i>. Callers MUST NOT rely on causal ordering of emissions across different
/// types — when two notifications arrive for types A and B in rapid succession, the
/// <c>OnNext</c> ordering is whichever reader resolves first. Within a single type's update
/// history the last observed value is the most recent reader read (preserved by
/// <see cref="Interlocked.Exchange{T}(ref T, T)"/> + single-writer-per-type invariant).
/// Consumers that need a totally-ordered stream MUST compose their own sequencer on top.
/// </para>
/// <para>
/// <b>Producer/consumer isolation (D5):</b> <see cref="CountChanged"/> is exposed as
/// <see cref="IObservable{T}"/> via <c>_subject.AsObservable()</c>; external consumers cannot
/// downcast to <see cref="Subject{T}"/> and inject phantom <c>OnNext</c> events.
/// </para>
/// <para>
/// <b>Notifier subscription (D6):</b> <see cref="IProjectionChangeNotifier"/> is resolved via
/// nullable DI — when absent, initial fetch still runs and live deltas are silently unavailable
/// (a reasonable v1 state because Story 5-3 is the real producer).
/// </para>
/// </remarks>
public sealed class BadgeCountService : IBadgeCountService, IDisposable, IAsyncDisposable {
    private const int InitialFetchTimeoutSeconds = 5;

    private readonly IActionQueueProjectionCatalog _catalog;
    private readonly IActionQueueCountReader _reader;
    private readonly IProjectionChangeNotifier? _notifier;
    private readonly IProjectionFallbackRefreshScheduler? _reconciliationScheduler;
    private readonly IUserContextAccessor? _userContextAccessor;
    private readonly ILogger<BadgeCountService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Subject<BadgeCountChangedArgs> _subject = new();
    private readonly ConcurrentDictionary<string, byte> _unresolvedTypes =
        new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly object _unresolvedSync = new();
    private readonly ConcurrentDictionary<Type, IDisposable> _reconciliationRegistrations = new();

    private ImmutableDictionary<Type, int> _counts = ImmutableDictionary<Type, int>.Empty;
    private int _disposedFlag;

    private bool IsDisposed => Volatile.Read(ref _disposedFlag) != 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BadgeCountService"/> class.
    /// </summary>
    /// <param name="catalog">The catalog of ActionQueue-hinted projection types.</param>
    /// <param name="reader">The per-type count reader.</param>
    /// <param name="serviceProvider">Service provider for optional <see cref="IProjectionChangeNotifier"/> resolution.</param>
    /// <param name="logger">Logger for HFC2112 / HFC2113 diagnostics.</param>
    /// <param name="timeProvider">Time provider — injected for deterministic timeout tests (D4).</param>
    public BadgeCountService(
        IActionQueueProjectionCatalog catalog,
        IActionQueueCountReader reader,
        IServiceProvider serviceProvider,
        ILogger<BadgeCountService> logger,
        TimeProvider timeProvider) {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _catalog = catalog;
        _reader = reader;
        _logger = logger;
        _timeProvider = timeProvider;
        _notifier = serviceProvider.GetService<IProjectionChangeNotifier>();
        _reconciliationScheduler = serviceProvider.GetService<IProjectionFallbackRefreshScheduler>();
        _userContextAccessor = serviceProvider.GetService<IUserContextAccessor>();
        if (_notifier is not null) {
            _notifier.ProjectionChanged += OnProjectionChanged;
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Type, int> Counts => _counts;

    /// <inheritdoc />
    public IObservable<BadgeCountChangedArgs> CountChanged => _subject.AsObservable();

    /// <inheritdoc />
    public int TotalActionableItems => _counts.Values.Sum();

    /// <summary>
    /// Fans out an initial parallel fetch across every catalog entry under a 5-second umbrella
    /// timeout. Per-type exceptions are caught and logged as <c>HFC2112</c>; the offending type is
    /// excluded from <see cref="Counts"/> and the remaining projections publish normally.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default) {
        if (IsDisposed) {
            return;
        }

        using CancellationTokenSource timeoutCts = new(
            TimeSpan.FromSeconds(InitialFetchTimeoutSeconds),
            _timeProvider);
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCts.Token,
            timeoutCts.Token);

        IReadOnlyList<Type> types;
        try {
            types = _catalog.ActionQueueTypes;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Badge catalog enumeration threw — skipping initial fetch. ExceptionType={ExceptionType}; ExceptionMessage={ExceptionMessage}.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                ex.GetType().Name,
                ex.Message);
            return;
        }

        if (types.Count == 0) {
            return;
        }

        Task[] tasks = new Task[types.Count];
        for (int i = 0; i < types.Count; i++) {
            RegisterReconciliationLane(types[i]);
            tasks[i] = FetchOneAsync(types[i], cts.Token);
        }

        try {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Individual failures are already caught inside FetchOneAsync; a top-level cancellation
            // here means the 5-second umbrella timeout fired. Partial results remain in _counts.
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposedFlag, 1) != 0) {
            return;
        }

        if (_notifier is not null) {
            _notifier.ProjectionChanged -= OnProjectionChanged;
        }

        try {
            _lifetimeCts.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed by a racing thread — ignore.
        }

        _lifetimeCts.Dispose();
        foreach (IDisposable registration in _reconciliationRegistrations.Values) {
            registration.Dispose();
        }

        _reconciliationRegistrations.Clear();

        _subject.OnCompleted();
        _subject.Dispose();
    }

    private async Task FetchOneAsync(Type projectionType, CancellationToken cancellationToken) {
        try {
            int count = await _reader.GetCountAsync(projectionType, cancellationToken).ConfigureAwait(false);
            if (IsDisposed) {
                return;
            }

            UpdateCount(projectionType, count);
        }
        catch (OperationCanceledException) {
            // Expected on dispose or 5-second umbrella timeout; no log.
        }
        catch (Exception ex) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Badge reader threw for projection '{ProjectionTypeName}'. ExceptionType={ExceptionType}; ExceptionMessage={ExceptionMessage}.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                projectionType.FullName,
                ex.GetType().Name,
                ex.Message);
        }
    }

    private bool UpdateCount(Type projectionType, int newCount) {
        if (newCount < 0) {
            // Reader contract implies non-negative counts; drop and log rather than publish
            // negative values through CountChanged (would skew TotalActionableItems sums).
            _logger.LogWarning(
                "{DiagnosticId}: Badge reader returned negative count {NewCount} for projection '{ProjectionTypeName}' — dropping emission.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                newCount,
                projectionType.FullName);
            return false;
        }

        ImmutableDictionary<Type, int> current;
        ImmutableDictionary<Type, int> next;
        while (true) {
            current = _counts;
            // Story 5-2 AC7 — suppress duplicate emissions when the value did not change.
            // 304 Not Modified responses (and 429 preserve-prior-count flows) MUST NOT emit
            // a CountChanged notification or trigger a badge animation.
            if (current.TryGetValue(projectionType, out int previous) && previous == newCount) {
                return false;
            }

            next = current.SetItem(projectionType, newCount);
            if (Interlocked.CompareExchange(ref _counts, next, current) == current) {
                break;
            }
        }

        if (IsDisposed) {
            return false;
        }

        try {
            _subject.OnNext(new BadgeCountChangedArgs(projectionType, newCount));
        }
        catch (ObjectDisposedException) {
            // Race with DisposeAsync — safe to drop the emission.
        }

        return true;
    }

    private void RegisterReconciliationLane(Type projectionType) {
        if (_reconciliationScheduler is null || _userContextAccessor is null || string.IsNullOrWhiteSpace(_userContextAccessor.TenantId)) {
            return;
        }

        if (string.IsNullOrWhiteSpace(projectionType.FullName)) {
            return;
        }

        _ = _reconciliationRegistrations.GetOrAdd(projectionType, type => _reconciliationScheduler.RegisterLane(
            new ProjectionFallbackLane(
                ViewKey: string.Concat("action-queue-count:", type.FullName),
                ProjectionType: type.FullName!,
                TenantId: _userContextAccessor.TenantId,
                Skip: 0,
                Take: 1,
                Filters: ImmutableDictionary<string, string>.Empty,
                SortColumn: null,
                SortDescending: false,
                SearchQuery: null,
                RefreshAsync: token => RefreshCountLaneAsync(type, token))));
    }

    private async ValueTask<ProjectionFallbackLaneRefreshOutcome> RefreshCountLaneAsync(Type projectionType, CancellationToken cancellationToken) {
        int count = await _reader.GetCountAsync(projectionType, cancellationToken).ConfigureAwait(false);
        if (IsDisposed) {
            return ProjectionFallbackLaneRefreshOutcome.Skipped;
        }

        return UpdateCount(projectionType, count)
            ? ProjectionFallbackLaneRefreshOutcome.Changed
            : ProjectionFallbackLaneRefreshOutcome.NotModified;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification =
            "D6 / Murat P0: an async-void event handler that escapes any exception kills the Blazor "
            + "Server circuit. We log and swallow every non-cancellation exception here.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "VSTHRD100:Avoid async void methods",
        Justification =
            "D6 canonical use-case: event handler for IProjectionChangeNotifier.ProjectionChanged. "
            + "Alternative `_ = Task.Run(...)` bypasses Blazor's sync context — strictly worse.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming",
        "IL2057:Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType'",
        Justification =
            "D7 / G24 — IProjectionChangeNotifier.ProjectionChanged delivers string type-names that "
            + "resolve via Type.GetType with throwOnError:false. Trim-enabled hosts MUST ship a "
            + "custom IProjectionChangeNotifier implementation that emits already-resolved types "
            + "(G24 v2 evolution) or accept that HFC2113 will log for trimmed types.")]
    private async void OnProjectionChanged(string projectionTypeName) {
        try {
            if (IsDisposed || string.IsNullOrWhiteSpace(projectionTypeName)) {
                return;
            }

            Type? resolved = Type.GetType(projectionTypeName, throwOnError: false, ignoreCase: false);
            if (resolved is null) {
                LogUnresolvedOnce(projectionTypeName);
                return;
            }

            if (!ContainsCatalogType(resolved)) {
                return;
            }

            int count = await _reader.GetCountAsync(resolved, _lifetimeCts.Token).ConfigureAwait(false);
            if (IsDisposed) {
                return;
            }

            UpdateCount(resolved, count);
        }
        catch (OperationCanceledException) {
            // Expected on dispose; no log.
        }
        catch (Exception ex) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Badge notifier handler threw for projection '{ProjectionTypeName}'. ExceptionType={ExceptionType}; ExceptionMessage={ExceptionMessage}.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                projectionTypeName,
                ex.GetType().Name,
                ex.Message);
        }
    }

    private void LogUnresolvedOnce(string projectionTypeName) {
        if (!_unresolvedTypes.TryAdd(projectionTypeName, 0)) {
            return;
        }

        // Double-guard with an explicit lock around the log call itself: ConcurrentDictionary's
        // TryAdd is atomic, so only one thread takes the "true" branch per distinct string — but
        // we keep the lock for future-proofing against split log lines under high concurrency.
        lock (_unresolvedSync) {
            _logger.LogInformation(
                "{DiagnosticId}: Projection type-name '{TypeNameString}' failed Type.GetType resolution — most likely an adopter mis-registration.",
                FcDiagnosticIds.HFC2113_ProjectionTypeUnresolvable,
                projectionTypeName);
        }
    }

    private bool ContainsCatalogType(Type resolved) {
        try {
            IReadOnlyList<Type> types = _catalog.ActionQueueTypes;
            for (int i = 0; i < types.Count; i++) {
                if (types[i] == resolved) {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // Catalog enumeration failed — treat as "not in catalog" (silent no-op) rather than
            // logging HFC2112 repeatedly on every notifier event.
            return false;
        }
    }
}
