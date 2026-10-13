using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

namespace Hexalith.FrontComposer.Shell.Services.Announcements;

/// <summary>Claims fresh-row speech once per resolved scope and entity transition, across view mounts.</summary>
internal sealed class FreshRowAnnouncementLedger(TimeProvider time, IUserContextAccessor context)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Dictionary<(string EntityKey, string MessageId), DateTimeOffset> _claims = [];
    private (string Tenant, string User)? _scope;
    private readonly ConditionalWeakTable<NewItemIndicatorEntry, FreshRowAnnouncementOrigin> _origins = new();
    private long _scopeGeneration;
    private readonly Dictionary<(string Entity, string Message, DateTimeOffset CreatedAt),
        (FreshRowAnnouncementOrigin Origin, DateTimeOffset ObservedAt)> _transitionOrigins = [];

    /// <summary>Binds a component entry to its first observed scope, before it may render or claim speech.</summary>
    internal bool IsOriginatingScope(NewItemIndicatorEntry entry)
    {
        lock (_gate)
        {
            bool resolved = ReconcileScope();
            DateTimeOffset now = time.GetUtcNow();
            foreach (var prior in _transitionOrigins.Where(prior => now - prior.Value.ObservedAt >= Lifetime).ToArray())
            {
                _transitionOrigins.Remove(prior.Key);
            }
            var key = (entry.EntityKey, entry.MessageId, entry.CreatedAt);
            FreshRowAnnouncementOrigin origin = _origins.GetValue(entry, _ =>
                _transitionOrigins.TryGetValue(key, out var prior)
                    ? prior.Origin : new FreshRowAnnouncementOrigin(_scope, _scopeGeneration));
            // Copies and other views retain the transition's origin during its active window;
            // weak object ownership also rejects an original entry retained beyond that window.
            _transitionOrigins.TryAdd(key, (origin, now));
            return resolved && origin.Scope == _scope && origin.Generation == _scopeGeneration
                && !string.IsNullOrWhiteSpace(entry.ViewKey)
                && !string.IsNullOrWhiteSpace(entry.EntityKey)
                && !string.IsNullOrWhiteSpace(entry.MessageId);
        }
    }

    /// <summary>Clears prior-scope claims before an entry can render and reports whether scope is resolved.</summary>
    internal bool HasResolvedScope()
    {
        lock (_gate)
        {
            return ReconcileScope();
        }
    }

    /// <summary>Claims this entity/message transition without depending on its view key.</summary>
    internal bool TryClaim(NewItemIndicatorEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_gate)
        {
            if (!IsOriginatingScope(entry))
            {
                return false;
            }

            DateTimeOffset now = time.GetUtcNow();
            foreach (var claim in _claims.Where(claim => now - claim.Value >= Lifetime).ToArray())
            {
                _claims.Remove(claim.Key);
            }

            return _claims.TryAdd((entry.EntityKey, entry.MessageId), now);
        }
    }

    private bool ReconcileScope()
    {
        (string Tenant, string User)? current = null;
        try
        {
            string? tenant = context.TenantId;
            string? user = context.UserId;
            if (!string.IsNullOrWhiteSpace(tenant) && !string.IsNullOrWhiteSpace(user))
            {
                current = (tenant, user);
            }
        }
        catch (Exception exception) when (!ExceptionGuard.IsFatal(exception))
        {
            // An unavailable accessor is unresolved scope and must fail closed.
        }

        if (_scope != current)
        {
            _claims.Clear();
            _scope = current;
            ++_scopeGeneration;
        }

        return current is not null;
    }
}
