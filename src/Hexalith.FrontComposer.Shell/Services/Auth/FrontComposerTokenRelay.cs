using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Process-wide store of per-user access tokens captured at OIDC sign-in. Keyed by the authenticated
/// user's stable identifier (the <c>sub</c>/NameIdentifier claim). It lets a Blazor Server circuit —
/// which has no <see cref="HttpContext"/> — relay the signed-in user's bearer token to a downstream
/// gateway. Tokens are overwritten on each sign-in and removed on sign-out.
/// </summary>
public sealed class FrontComposerUserTokenStore {
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);

    /// <summary>Stores (or overwrites) the access token for the given user id.</summary>
    public void Set(string userId, string accessToken) => _tokens[userId] = accessToken;

    /// <summary>Reads the stored access token for the given user id, if any.</summary>
    public bool TryGet(string userId, out string accessToken) => _tokens.TryGetValue(userId, out accessToken!);

    /// <summary>Removes the stored access token for the given user id (sign-out).</summary>
    public void Remove(string userId) => _tokens.TryRemove(userId, out _);
}

/// <summary>
/// Holds the current Blazor circuit's service provider in an <see cref="AsyncLocal{T}"/> so pooled
/// infrastructure (such as <see cref="DelegatingHandler"/> instances created by
/// <see cref="IHttpClientFactory"/>) can resolve circuit-scoped services while an inbound circuit
/// activity is executing. Registered as a singleton; the value is published per inbound activity by
/// <see cref="FrontComposerCircuitServicesHandler"/>.
/// </summary>
public sealed class CircuitServicesAccessor {
    // Instance field (the accessor is a DI singleton): AsyncLocal still flows per async/circuit
    // activity, and an instance member keeps the singleton's Services accessor non-static.
    private readonly AsyncLocal<IServiceProvider?> _current = new();

    /// <summary>The service provider scoped to the currently executing circuit activity, if any.</summary>
    public IServiceProvider? Services {
        get => _current.Value;
        set => _current.Value = value;
    }
}

/// <summary>
/// Publishes the circuit's scoped <see cref="IServiceProvider"/> into <see cref="CircuitServicesAccessor"/>
/// for the duration of each inbound circuit activity, enabling outbound HTTP handlers to read
/// circuit-scoped services (for example <see cref="AuthenticationStateProvider"/>).
/// </summary>
public sealed class FrontComposerCircuitServicesHandler(
    IServiceProvider circuitServices,
    CircuitServicesAccessor accessor) : CircuitHandler {
    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async context => {
            accessor.Services = circuitServices;
            try {
                await next(context).ConfigureAwait(false);
            }
            finally {
                accessor.Services = null;
            }
        };
}

/// <summary>
/// Attaches the signed-in user's access token as a bearer header on outbound gateway requests.
/// Resolves the current principal from <see cref="IHttpContextAccessor"/> during server-side render
/// and from the circuit's <see cref="AuthenticationStateProvider"/> (via
/// <see cref="CircuitServicesAccessor"/>) during interactive circuit activity. Anonymous requests are
/// left untouched, so the gateway returns 401 and the UI surfaces the sign-in state.
/// </summary>
public sealed class FrontComposerGatewayAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    FrontComposerUserTokenStore tokenStore) : DelegatingHandler {
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null) {
            string? userId = await ResolveUserIdAsync().ConfigureAwait(false);
            if (userId is not null && tokenStore.TryGet(userId, out string token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> ResolveUserIdAsync() {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) {
            if (circuitServicesAccessor.Services?.GetService(typeof(AuthenticationStateProvider)) is AuthenticationStateProvider provider) {
                AuthenticationState state = await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
                user = state.User;
            }
        }

        return user?.Identity?.IsAuthenticated == true
            ? user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }
}
