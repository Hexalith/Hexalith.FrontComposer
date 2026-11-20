using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.UI.Services;
using Hexalith.Parties.AdminPortal.Services;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.UI.Tests;

public sealed class PartiesAdminAuthorizationTests : BunitContext
{
    private readonly BunitAuthorizationContext _authentication;
    private readonly ITenantQueryGateway _tenants = Substitute.For<ITenantQueryGateway>();
    private readonly PartiesAdminAuthorizationService _service;

    public PartiesAdminAuthorizationTests()
    {
        _authentication = AddAuthorization();
        _service = new(Services.GetRequiredService<AuthenticationStateProvider>(), _tenants);
    }

    [Fact]
    public async Task CurrentOwnerOfSelectedTenantCanAdminister()
    {
        SignIn();
        _tenants.GetTenantCorrectionProjectionAsync("selected", "operator", Arg.Any<CancellationToken>())
            .Returns(CurrentOwner);

        AdminPortalAuthorizationState result = await _service.GetAuthorizationStateAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsAdmin.ShouldBeTrue();
        result.HasTenantContext.ShouldBeTrue();
    }

    [Theory]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-user")]
    [InlineData("missing-membership")]
    [InlineData("stale")]
    [InlineData("unconfirmed")]
    [InlineData("disabled")]
    [InlineData("unavailable")]
    [InlineData("global-admin-reader")]
    [InlineData("no-authority")]
    public async Task InsufficientEvidenceDeniesEvenWithAnAdminClaim(string scenario)
    {
        SignIn();
        TenantCorrectionProjection evidence = scenario switch
        {
            "wrong-tenant" => CurrentOwner with { TenantId = "other" },
            "wrong-user" => CurrentOwner with { TargetUserId = "other" },
            "missing-membership" => CurrentOwner with { HasVerifiedMembership = false },
            "stale" => CurrentOwner with { Freshness = ReadModelFreshnessState.Stale },
            "unconfirmed" => CurrentOwner with { Provenance = QueryResponseProvenance.Unknown },
            "disabled" => CurrentOwner with { TenantStatus = TenantStatus.Disabled },
            "unavailable" => TenantCorrectionProjection.Unavailable("selected", "operator"),
            "global-admin-reader" => CurrentOwner with { CurrentRole = TenantRole.TenantReader, IsGlobalAdministrator = true },
            "no-authority" => CurrentOwner with { IsAuthorized = false },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
        _tenants.GetTenantCorrectionProjectionAsync("selected", "operator", Arg.Any<CancellationToken>())
            .Returns(evidence);

        (await _service.GetAuthorizationStateAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public async Task AmbiguousCurrentTenantDoesNotDispatchAQuery()
    {
        _authentication.SetAuthorized("operator");
        _authentication.SetClaims(new Claim("sub", "operator"),
            new Claim(PartiesAdminAuthorizationService.CurrentTenantClaim, "selected"),
            new Claim(PartiesAdminAuthorizationService.CurrentTenantClaim, "other"));

        (await _service.GetAuthorizationStateAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).IsAdmin.ShouldBeFalse();

        _tenants.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task SigningOutDuringTheReadDiscardsTheOldPrincipalsOwnerGrant()
    {
        SignIn();
        var pending = new TaskCompletionSource<TenantCorrectionProjection>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _tenants.GetTenantCorrectionProjectionAsync("selected", "operator", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                started.SetResult();
                return pending.Task;
            });

        Task<AdminPortalAuthorizationState> authorization = _service.GetAuthorizationStateAsync(Xunit.TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        _authentication.SetNotAuthorized();
        pending.SetResult(CurrentOwner);

        (await authorization.ConfigureAwait(true)).ShouldBe(AdminPortalAuthorizationState.Unauthenticated);
    }

    private static TenantCorrectionProjection CurrentOwner => new(
        "selected", "operator", TenantStatus.Active, TenantRole.TenantOwner, false, true, false, true,
        ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);

    private void SignIn()
    {
        _authentication.SetAuthorized("operator");
        _authentication.SetClaims(new Claim("sub", "operator"),
            new Claim("roles", "Admin"),
            new Claim("eventstore:tenant", "other"),
            new Claim(PartiesAdminAuthorizationService.CurrentTenantClaim, "selected"));
    }
}
