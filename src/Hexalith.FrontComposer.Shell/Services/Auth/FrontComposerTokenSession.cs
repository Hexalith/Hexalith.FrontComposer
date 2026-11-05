using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>Identifies one authenticated sign-in and its credential authority.</summary>
/// <param name="SessionId">The opaque, server-generated sign-in identifier.</param>
/// <param name="UserId">The authenticated stable subject.</param>
/// <param name="TenantId">The validated current Tenant.</param>
public sealed record FrontComposerTokenSession(string SessionId, string UserId, string TenantId)
{
    /// <summary>The local claim persisted in the protected authentication ticket.</summary>
    public const string ClaimType = "hexalith:frontcomposer:token-session";

    /// <summary>Resolves the exact session binding, rejecting ambiguous or absent authority.</summary>
    internal static FrontComposerTokenSession? Resolve(ClaimsPrincipal? principal, FrontComposerAuthenticationOptions options)
    {
        string? sessionId = ResolveSessionId(principal);
        if (sessionId is null)
        {
            return null;
        }

        string? tenant = FrontComposerClaimExtractor.ExtractTenant(principal, [.. options.TenantClaimTypes]);
        Claim[] subjects = principal!.Claims.Where(c => c.Type is "sub" or ClaimTypes.NameIdentifier).ToArray();
        string? subject;
        if (subjects.Length == 0)
        {
            subject = FrontComposerClaimExtractor.Extract(principal, [.. options.TenantClaimTypes], [.. options.UserClaimTypes]).UserId;
        }
        else
        {
            // OIDC subjects are opaque strings, not application identifier segments. Preserve colons.
            subject = subjects[0].Value;
            if (string.IsNullOrWhiteSpace(subject) || subjects.GroupBy(c => c.Type).Any(group => group.Count() != 1)
                || subjects.Any(c => !string.Equals(c.Value, subject, StringComparison.Ordinal)))
            {
                return null;
            }
        }

        return tenant is not null && subject is not null ? new(sessionId, subject, tenant) : null;
    }

    /// <summary>Reads the sign-in identifier independently of mutable Tenant/user claims for revocation.</summary>
    internal static string? ResolveSessionId(ClaimsPrincipal? principal)
    {
        Claim[] sessions = principal?.FindAll(ClaimType).ToArray() ?? [];
        return principal?.Identity?.IsAuthenticated == true && sessions.Length == 1
            && !string.IsNullOrWhiteSpace(sessions[0].Value) ? sessions[0].Value : null;
    }

    /// <summary>Prefers the active circuit over a potentially stale request context.</summary>
    internal static async Task<ClaimsPrincipal?> ReadPrincipalAsync(
        IHttpContextAccessor httpContextAccessor, CircuitServicesAccessor circuitServicesAccessor)
    {
        if (circuitServicesAccessor.Services?.GetService<AuthenticationStateProvider>() is { } provider)
        {
            return (await provider.GetAuthenticationStateAsync().ConfigureAwait(false)).User;
        }

        return httpContextAccessor.HttpContext?.User;
    }
}
