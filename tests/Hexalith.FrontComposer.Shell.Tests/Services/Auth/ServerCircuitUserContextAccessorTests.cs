using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

/// <summary>
/// Verifies the Blazor Server circuit-aware <see cref="ServerCircuitUserContextAccessor"/>: it reads
/// the request principal during server-side render and the circuit's
/// <see cref="AuthenticationStateProvider"/> when <c>HttpContext</c> is null (the interactive-circuit
/// fix), while preserving the deliberate fail-closed coupling of the tenant/user claim extraction.
/// </summary>
public sealed class ServerCircuitUserContextAccessorTests {
    [Fact]
    public void Accessor_ReadsHttpContextPrincipal_WhenPresent() {
        ServerCircuitUserContextAccessor accessor = Build(
            httpPrincipal: Principal(new("eventstore:tenant", "system"), new("sub", "alice")),
            circuitPrincipal: null,
            out _);

        accessor.TenantId.ShouldBe("system");
        accessor.UserId.ShouldBe("alice");
    }

    [Fact]
    public void Accessor_ReadsCircuitPrincipal_WhenHttpContextHasNoAuthenticatedUser() {
        // The interactive Server fix: HttpContext is null in the circuit, so the principal must be
        // resolved from the circuit's AuthenticationStateProvider via CircuitServicesAccessor.
        ServerCircuitUserContextAccessor accessor = Build(
            httpPrincipal: null,
            circuitPrincipal: Principal(new("eventstore:tenant", "tenant-a"), new("sub", "bob")),
            out _);

        accessor.TenantId.ShouldBe("tenant-a");
        accessor.UserId.ShouldBe("bob");
    }

    [Fact]
    public void Accessor_FailsClosed_WhenNoPrincipalAvailable() {
        ServerCircuitUserContextAccessor accessor = Build(httpPrincipal: null, circuitPrincipal: null, out _);

        accessor.TenantId.ShouldBeNull();
        accessor.UserId.ShouldBeNull();
    }

    [Fact]
    public void Accessor_FailsClosed_AndNullsUserId_WhenTenantClaimIsMultiValued() {
        // Preserves the deliberate D31 coupling: a multi-valued (ambiguous) tenant claim yields no
        // context at all — UserId is null too — even though the user claim itself is valid. The
        // circuit-aware seam must not weaken this fail-closed contract.
        ServerCircuitUserContextAccessor accessor = Build(
            httpPrincipal: null,
            circuitPrincipal: Principal(
                new("eventstore:tenant", "system"),
                new("eventstore:tenant", "tenant-a"),
                new("sub", "carol")),
            out CapturingLogger<ServerCircuitUserContextAccessor> logger);

        accessor.TenantId.ShouldBeNull();
        accessor.UserId.ShouldBeNull();
        logger.Messages.ShouldContain(m => m.Contains(FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed, StringComparison.Ordinal));
    }

    private static ServerCircuitUserContextAccessor Build(
        ClaimsPrincipal? httpPrincipal,
        ClaimsPrincipal? circuitPrincipal,
        out CapturingLogger<ServerCircuitUserContextAccessor> logger) {
        HttpContextAccessor http = new() {
            HttpContext = httpPrincipal is null ? null : new DefaultHttpContext { User = httpPrincipal },
        };

        CircuitServicesAccessor circuitServices = new();
        if (circuitPrincipal is not null) {
            ServiceProvider provider = new ServiceCollection()
                .AddScoped<AuthenticationStateProvider>(_ => new StubAuthenticationStateProvider(circuitPrincipal))
                .BuildServiceProvider();
            circuitServices.Services = provider;
        }

        FrontComposerAuthenticationOptions options = new();
        options.TenantClaimTypes.Clear();
        options.UserClaimTypes.Clear();
        options.TenantClaimTypes.Add("eventstore:tenant");
        options.UserClaimTypes.Add("sub");

        logger = new();
        return new ServerCircuitUserContextAccessor(http, circuitServices, Microsoft.Extensions.Options.Options.Create(options), logger);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "Test"));

    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }
}
