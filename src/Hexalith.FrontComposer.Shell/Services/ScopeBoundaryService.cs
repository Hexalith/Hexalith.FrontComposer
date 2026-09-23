using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Owns the eager tenant and user boundary for one interactive circuit.</summary>
public sealed class ScopeBoundaryService(
    AuthenticationStateProvider authenticationStateProvider,
    IFrontComposerTenantContextAccessor tenantContextAccessor,
    IDispatcher dispatcher,
    IScopeReadinessGate readinessGate,
    IPendingCommandStateService pendingCommands,
    INewItemIndicatorStateService newItems,
    IBadgeCountService badgeCounts,
    ICommandExecutionAdmissionGate admissionGate,
    IServiceProvider services) : IDisposable {
    private readonly object _gate = new();
    private TenantContextSnapshot? _snapshot;
    private bool _started;
    private bool _disposed;

    /// <summary>Raised after a scope transition has cleared circuit state.</summary>
    public event EventHandler? Changed;

    /// <summary>Whether the current validated scope matches the last completed boundary.</summary>
    public bool IsCurrent {
        get {
            TenantContextSnapshot? current = ReadScope("scope-render");
            lock (_gate) {
                return _started && current is not null && _snapshot is not null
                    && SameScope(current, _snapshot);
            }
        }
    }

    /// <summary>Starts observing auth changes and captures the first valid scope.</summary>
    public void Start() {
        lock (_gate) {
            if (_started || _disposed) {
                return;
            }

            _started = true;
            _snapshot = ReadScope("scope-start");
            authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    /// <summary>Reconciles a change before the next scope is allowed to render.</summary>
    public void Synchronize() {
        TenantContextSnapshot? next = ReadScope("scope-change");
        SynchronizeCore(next);
    }

    private void SynchronizeCore(TenantContextSnapshot? next) {
        bool changed;
        lock (_gate) {
            if (_disposed || !_started) {
                return;
            }

            changed = (_snapshot is null) != (next is null)
                || (_snapshot is not null && next is not null && !SameScope(_snapshot, next));
            if (!changed) {
                return;
            }

            // Keep render guards closed until every prior-scope store is cleared.
            _snapshot = null;
            dispatcher.Dispatch(new ScopeChangedAction());
            dispatcher.Dispatch(new PaletteScopeChangedAction());
            if (readinessGate is ScopeReadinessGate concreteReadinessGate) {
                concreteReadinessGate.ResetForScopeChange();
            }
            pendingCommands.Clear("TenantOrUserTransition");
            newItems.Clear("TenantOrUserTransition");
            if (admissionGate is CommandExecutionAdmissionGate concreteAdmissionGate) {
                concreteAdmissionGate.ResetScope();
            }
            if (badgeCounts is BadgeCountService concrete) {
                concrete.ResetScope();
            }

            if (services.GetService<ProjectionSubscriptionService>() is { } subscriptions) {
                subscriptions.BlockStaleGroups();
                _ = subscriptions.BlockStaleGroupsAsync();
            }

            _snapshot = next;
            if (next is not null) {
                if (badgeCounts is BadgeCountService scopedBadges) {
                    _ = scopedBadges.InitializeAsync();
                }
                _ = readinessGate.EvaluateAsync(dispatcher);
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose() {
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }

    private static bool SameScope(TenantContextSnapshot left, TenantContextSnapshot right)
        => string.Equals(left.TenantId, right.TenantId, StringComparison.Ordinal)
            && string.Equals(left.UserId, right.UserId, StringComparison.Ordinal);

    private TenantContextSnapshot? ReadScope(string operationKind) {
        try {
            return tenantContextAccessor.TryGetContext(operationKind: operationKind).Context;
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            return null;
        }
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask) {
        try {
            _ = await authenticationStateTask.ConfigureAwait(false);
            Synchronize();
        }
        catch (ObjectDisposedException) {
            // Circuit teardown won the race with the authentication event.
        }
        catch (Exception) {
            // An unavailable authentication state is a blocked scope, never a reason to render old data.
            SynchronizeCore(null);
        }
    }
}
