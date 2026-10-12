using System.Runtime.CompilerServices;

using Fluxor;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Remembers the producer scope of generated projection state within one Fluxor store.</summary>
public sealed class ProjectionStateScopeOwnership
{
    private static readonly ConditionalWeakTable<IDispatcher, ProjectionStateScopeOwnership> Owners = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, (string TenantId, string UserId, long? Generation, long? MinimumGeneration)> _scopes = new(StringComparer.Ordinal);

    /// <summary>Gets the scope owner registry for a store.</summary>
    public static ProjectionStateScopeOwnership For(IDispatcher dispatcher)
        => Owners.GetValue(dispatcher, static _ => new ProjectionStateScopeOwnership());

    /// <summary>Resolves a generated view's current scope, failing closed when the accessor throws.</summary>
    public static TenantContextSnapshot? ResolveCurrentScope(IFrontComposerTenantContextAccessor accessor,
        string? requestedTenantId, string operationKind)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        try
        {
            return accessor.TryGetContext(requestedTenantId, operationKind).Context;
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
        {
            return null;
        }
    }

    /// <summary>Checks current scope against action provenance, retaining legacy direct-action ownership.</summary>
    public bool IsCurrent(string surface, string? producerTenantId, string? producerUserId, TenantContextSnapshot current,
        long? pageScopeGeneration = null, bool canClaimUnstampedInitialState = false)
    {
        ArgumentNullException.ThrowIfNull(current);
        lock (_gate)
        {
            if (producerTenantId is not null || producerUserId is not null)
            {
                if (string.IsNullOrWhiteSpace(producerTenantId) || string.IsNullOrWhiteSpace(producerUserId))
                {
                    return false;
                }
                // A late producer from another scope cannot take ownership away from
                // the current user or raise that user's minimum page generation.
                if (!string.Equals(producerTenantId, current.TenantId, StringComparison.Ordinal)
                    || !string.Equals(producerUserId, current.UserId, StringComparison.Ordinal))
                {
                    return false;
                }
                if (!_scopes.TryGetValue(surface, out var prior)
                    || !string.Equals(prior.TenantId, producerTenantId, StringComparison.Ordinal)
                    || !string.Equals(prior.UserId, producerUserId, StringComparison.Ordinal))
                {
                    long? minimumGeneration = prior.Generation.HasValue && pageScopeGeneration.HasValue
                        ? Math.Max(pageScopeGeneration.Value, prior.Generation.Value + 1)
                        : pageScopeGeneration;
                    _scopes[surface] = (producerTenantId, producerUserId, pageScopeGeneration, minimumGeneration);
                }
            }
            else if (!_scopes.ContainsKey(surface))
            {
                // A blank initial state can establish a mounted legacy owner's scope.
                // Existing unstamped data has no provable producer and must not be
                // assigned to the user who happens to mount the first view.
                if (!canClaimUnstampedInitialState)
                {
                    return false;
                }
                _scopes[surface] = (current.TenantId, current.UserId, pageScopeGeneration, pageScopeGeneration);
            }

            var owner = _scopes[surface];
            bool currentOwner = string.Equals(owner.TenantId, current.TenantId, StringComparison.Ordinal)
                && string.Equals(owner.UserId, current.UserId, StringComparison.Ordinal);
            if (currentOwner && pageScopeGeneration.HasValue && pageScopeGeneration >= owner.MinimumGeneration
                && (!owner.Generation.HasValue || pageScopeGeneration > owner.Generation))
            {
                owner.Generation = pageScopeGeneration;
                _scopes[surface] = owner;
            }
            return currentOwner
                && (!owner.MinimumGeneration.HasValue || pageScopeGeneration >= owner.MinimumGeneration);
        }
    }
}
