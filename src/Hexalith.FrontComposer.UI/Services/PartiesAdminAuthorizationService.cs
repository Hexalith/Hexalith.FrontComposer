using System.Security.Claims;

using Hexalith.Parties.AdminPortal.Services;
using Hexalith.Parties.Contracts.Authorization;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.FrontComposer.UI.Services;

/// <summary>Resolves Parties administration from the signed-in caller's current Tenants projection.</summary>
internal sealed class PartiesAdminAuthorizationService(
    AuthenticationStateProvider authentication,
    ITenantQueryGateway tenants) : IAdminPortalAuthorizationService
{
    internal const string CurrentTenantClaim = "eventstore:current-tenant";

    /// <inheritdoc />
    public async Task<AdminPortalAuthorizationState> GetAuthorizationStateAsync(CancellationToken cancellationToken = default)
    {
        Task<AuthenticationState>? changed = null;
        void OnChanged(Task<AuthenticationState> state) => Volatile.Write(ref changed, state);

        authentication.AuthenticationStateChanged += OnChanged;
        try
        {
            Task<AuthenticationState> initial = authentication.GetAuthenticationStateAsync();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Task<AuthenticationState> selected = Volatile.Read(ref changed) ?? initial;
                AuthenticationState state = await selected.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!ReferenceEquals(selected, Volatile.Read(ref changed) ?? initial))
                {
                    continue;
                }

                AdminPortalAuthorizationState result = await ReadAsync(state.User, cancellationToken).ConfigureAwait(false);
                if (ReferenceEquals(selected, Volatile.Read(ref changed) ?? initial))
                {
                    return result;
                }
            }
        }
        finally
        {
            authentication.AuthenticationStateChanged -= OnChanged;
        }
    }

    private async Task<AdminPortalAuthorizationState> ReadAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return AdminPortalAuthorizationState.Unauthenticated;
        }

        // The current tenant is the host's single active selection. The realm's eventstore:tenant
        // claim lists all eligible tenants and must never determine which tenant is being administered.
        if (principal.Identities.Any(candidate => candidate.IsAuthenticated && !ReferenceEquals(candidate, identity)))
        {
            return new(true, false, false, "authenticated:ambiguous-identity");
        }

        Claim[] selection = identity.FindAll(CurrentTenantClaim).Take(2).ToArray();
        if (selection is not [var selected] || string.IsNullOrWhiteSpace(selected.Value))
        {
            return new(true, false, false, "authenticated:no-tenant");
        }

        string tenant = selected.Value;
        string? user = identity.TryGetUserId().Value;
        if (string.IsNullOrWhiteSpace(user))
        {
            return new(true, false, false, $"tenant:{tenant}:unavailable");
        }

        TenantCorrectionProjection evidence = await tenants
            .GetTenantCorrectionProjectionAsync(tenant, user, cancellationToken).ConfigureAwait(false);
        if (!evidence.IsCurrent || evidence.TenantStatus != TenantStatus.Active
            || !string.Equals(evidence.TenantId, tenant, StringComparison.Ordinal)
            || !string.Equals(evidence.TargetUserId, user, StringComparison.Ordinal))
        {
            return new(true, false, false, $"tenant:{tenant}:unavailable");
        }

        bool owner = evidence.IsAuthorized && evidence.CurrentRole == TenantRole.TenantOwner;
        return new(true, true, owner, $"tenant:{tenant}:user:{user}:{(owner ? "admin" : "not-admin")}");
    }
}
