using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Shortcuts;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Shortcuts;

/// <summary>
/// Scoped (per-circuit / per-user) implementation of <see cref="IShortcutService"/> (Story 3-4 D2).
/// </summary>
/// <remarks>
/// <para>
/// Backing store is a <see cref="ConcurrentDictionary{TKey, TValue}"/> keyed on the normalised
/// binding so late registrations from background-scheduled adopter code do not race the shell
/// registrar (D2 rationale).
/// </para>
/// <para>
/// Two-key chord state (D4) is guarded by <see cref="_chordSync"/>; the 1500 ms pending-key timer
/// is allocated via the injected <see cref="TimeProvider"/> so tests advance virtual time
/// (<c>FakeTimeProvider</c> per D22) instead of sleeping.
/// </para>
/// </remarks>
public sealed class ShortcutService : IShortcutService, IDisposable
{
    private const int ChordTimeoutMilliseconds = 1500;

    private readonly ConcurrentDictionary<string, ShortcutEntry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ShortcutService> _logger;
    private readonly object _chordSync = new();

    private string? _pendingFirstKey;
    private long _pendingGeneration;
    private ITimer? _chordTimer;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="ShortcutService"/> class.
    /// </summary>
    /// <param name="timeProvider">Time source used by the chord-timeout timer (D22). When <see langword="null"/> falls back to <see cref="TimeProvider.System"/>.</param>
    /// <param name="logger">Logger for HFC2108 (conflict) + HFC2109 (handler-fault) diagnostics.</param>
    public ShortcutService(TimeProvider? timeProvider, ILogger<ShortcutService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <inheritdoc />
    public IDisposable Register(
        string binding,
        string descriptionKey,
        Func<Task> handler,
        [CallerFilePath] string callSiteFile = "",
        [CallerLineNumber] int callSiteLine = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(binding);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionKey);
        ArgumentNullException.ThrowIfNull(handler);

        string normalised = ShortcutBinding.Normalize(binding);
        string label = ShortcutBinding.FormatLabel(normalised);
        ShortcutEntry entry = new(normalised, descriptionKey, label, handler, callSiteFile, callSiteLine);

        // P4 (2026-04-21 pass-3): atomic add-or-replace. The prior TryGetValue → indexer-assign
        // sequence was not atomic — two concurrent Register calls on the same binding could both
        // observe "no existing entry", both assign, and silently drop one registration without
        // logging HFC2108. AddOrUpdate serialises the compare + replace inside ConcurrentDictionary
        // so exactly one caller observes the previous entry.
        ShortcutEntry? previous = null;
        _ = _entries.AddOrUpdate(
            normalised,
            addValueFactory: static (_, newEntry) => newEntry,
            updateValueFactory: (_, existing, newEntry) =>
            {
                previous = existing;
                return newEntry;
            },
            factoryArgument: entry);

        if (previous is not null)
        {
            _logger.LogInformation(
                "{DiagnosticId}: Duplicate shortcut registration replaced. Binding={Binding} PreviousDescriptionKey={PreviousDescriptionKey} NewDescriptionKey={NewDescriptionKey} PreviousCallSiteFile={PreviousCallSiteFile} PreviousCallSiteLine={PreviousCallSiteLine} NewCallSiteFile={NewCallSiteFile} NewCallSiteLine={NewCallSiteLine}",
                FcDiagnosticIds.HFC2108_ShortcutConflict,
                normalised,
                previous.DescriptionKey,
                descriptionKey,
                previous.CallSiteFile,
                previous.CallSiteLine,
                callSiteFile,
                callSiteLine);
        }

        return new RegistrationDisposable(this, entry);
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutRegistration> GetRegistrations()
        => [.. _entries.Values
            .OrderBy(static e => e.Binding, StringComparer.Ordinal)
            .Select(static e => new ShortcutRegistration(e.Binding, e.DescriptionKey, e.NormalisedLabel))];

    /// <inheritdoc />
    public async Task<bool> TryInvokeAsync(KeyboardEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Post-dispose calls are benign (cleared _entries would miss every lookup), but explicitly
        // guarding keeps the chord-timer allocation path from racing circuit teardown. The
        // authoritative check also happens inside `_chordSync` below before any timer allocation.
        if (_disposed)
        {
            return false;
        }

        // Skip auto-repeat keystrokes — holding a chord-prefix letter like `g` would otherwise
        // allocate a fresh chord timer per tick, bump the generation, and reset pending state
        // unpredictably. The first press establishes the pending key; subsequent repeats no-op.
        if (e.Repeat)
        {
            return false;
        }

        if (!ShortcutBinding.TryFromKeyboardEvent(e, out string binding))
        {
            return false;
        }

        // Fast-path: modifier-bearing combos NEVER form chord continuations (D4 sub-decision d).
        bool hasModifier = e.CtrlKey || e.ShiftKey || e.AltKey || e.MetaKey;
        if (hasModifier)
        {
            ClearPending();
            return await TryInvokeBindingAsync(binding).ConfigureAwait(false);
        }

        // Bare-letter path: chord FSM.
        string? completed = null;
        lock (_chordSync)
        {
            // Re-check disposed inside the lock — circuit-teardown that flips `_disposed` between
            // the early guard and here must not be able to allocate a timer on a cleared service.
            if (_disposed)
            {
                return false;
            }

            if (_pendingFirstKey is { } pending)
            {
                string composite = $"{pending} {binding}";
                if (_entries.ContainsKey(composite))
                {
                    completed = composite;
                    DisposeTimerLocked();
                    _pendingFirstKey = null;
                }
                else
                {
                    DisposeTimerLocked();
                    _pendingFirstKey = null;

                    // Re-evaluate the new key as a fresh single-key lookup. Bare letters cannot be
                    // single-key bindings (Normalize forbids them), so this only fires if a future
                    // binding form is added — the lookup is a safe no-op today.
                }
            }

            if (completed is null && IsChordPrefix(binding))
            {
                long generation = ++_pendingGeneration;
                _pendingFirstKey = binding;
                DisposeTimerLocked();
                _chordTimer = _timeProvider.CreateTimer(
                    static state =>
                    {
                        ChordTimerState timerState = (ChordTimerState)state!;
                        ShortcutService self = timerState.Owner;
                        lock (self._chordSync)
                        {
                            if (timerState.Generation != self._pendingGeneration)
                            {
                                return;
                            }

                            self._pendingFirstKey = null;
                            self.DisposeTimerLocked();
                        }
                    },
                    new ChordTimerState(this, generation),
                    TimeSpan.FromMilliseconds(ChordTimeoutMilliseconds),
                    Timeout.InfiniteTimeSpan);
            }
        }

        if (completed is not null)
        {
            return await TryInvokeBindingAsync(completed).ConfigureAwait(false);
        }

        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_chordSync)
        {
            DisposeTimerLocked();
            _pendingFirstKey = null;
        }

        _entries.Clear();
    }

    private async Task<bool> TryInvokeBindingAsync(string binding)
    {
        if (!_entries.TryGetValue(binding, out ShortcutEntry? entry))
        {
            return false;
        }

        try
        {
            await entry.Handler().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Registered shortcut handler threw. Binding={Binding} DescriptionKey={DescriptionKey} ExceptionType={ExceptionType} ExceptionMessage={ExceptionMessage}",
                FcDiagnosticIds.HFC2109_ShortcutHandlerFault,
                entry.Binding,
                entry.DescriptionKey,
                ex.GetType().FullName,
                ex.Message);
            return true;
        }
    }

    private bool IsChordPrefix(string binding)
    {
        string prefix = binding + " ";
        foreach (string key in _entries.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearPending()
    {
        lock (_chordSync)
        {
            _pendingFirstKey = null;
            DisposeTimerLocked();
        }
    }

    private void DisposeTimerLocked()
    {
        ITimer? timer = _chordTimer;
        if (timer is null)
        {
            return;
        }

        _chordTimer = null;
        try
        {
            timer.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by a racing callback — safe to ignore.
        }
    }

    private void RemoveIfMatches(ShortcutEntry entry)
    {
        // Only remove the binding slot if the live entry is the one being disposed — a replacement
        // registration must survive disposal of the original (D3 sub-decision: dispose targets the
        // original entry, not the slot).
        if (_entries.TryGetValue(entry.Binding, out ShortcutEntry? current) && ReferenceEquals(current, entry))
        {
            _ = _entries.TryRemove(entry.Binding, out _);
        }
    }

    private sealed record ShortcutEntry(
        string Binding,
        string DescriptionKey,
        string NormalisedLabel,
        Func<Task> Handler,
        string CallSiteFile,
        int CallSiteLine);

    private sealed record ChordTimerState(ShortcutService Owner, long Generation);

    private sealed class RegistrationDisposable(ShortcutService owner, ShortcutEntry entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.RemoveIfMatches(entry);
            }
        }
    }
}
