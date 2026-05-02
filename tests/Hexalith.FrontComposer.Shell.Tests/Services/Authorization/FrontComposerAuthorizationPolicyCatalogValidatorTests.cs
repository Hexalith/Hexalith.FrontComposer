using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Authorization;

public sealed class FrontComposerAuthorizationPolicyCatalogValidatorTests {
    [Fact]
    public async Task StartAsync_NoCatalog_DoesNotFail() {
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(new FrontComposerAuthorizationOptions());

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_StrictCatalogMissingPolicy_FailsClosedWithCommandAndPolicyOnly() {
        FrontComposerAuthorizationOptions options = new() { StrictPolicyCatalogValidation = true };
        options.KnownPolicies.Add("OtherPolicy");
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.StartAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("Orders.ApproveOrderCommand:OrderApprover");
        ex.Message.ShouldNotContain("tenant-a");
        ex.Message.ShouldNotContain("user-a");
    }

    [Fact]
    public async Task StartAsync_CatalogContainsPolicy_DoesNotFail() {
        FrontComposerAuthorizationOptions options = new();
        options.KnownPolicies.Add("OrderApprover");
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    private static FrontComposerAuthorizationPolicyCatalogValidator Create(FrontComposerAuthorizationOptions options) {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest(
                "Orders",
                "Orders",
                Projections: [],
                Commands: ["Orders.ApproveOrderCommand"],
                CommandPolicies: new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["Orders.ApproveOrderCommand"] = "OrderApprover",
                }),
        ]);

        return new FrontComposerAuthorizationPolicyCatalogValidator(
            registry,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<FrontComposerAuthorizationPolicyCatalogValidator>.Instance);
    }
}
