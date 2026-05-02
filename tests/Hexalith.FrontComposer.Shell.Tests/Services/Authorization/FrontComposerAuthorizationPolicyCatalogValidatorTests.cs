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
    public async Task StartAsync_StrictCatalogMissingPolicy_FailsClosedWithSanitizedPolicyNamesOnly() {
        // Pass 3 DN-7-3-3-4 — the strict-mode exception payload includes the missing policy NAMES only.
        // Command FQNs are deliberately omitted so that orchestration logs cannot leak command identifiers
        // for adopters whose policy names happen to be PII-free but whose command FQNs encode customer data.
        FrontComposerAuthorizationOptions options = new() { StrictPolicyCatalogValidation = true };
        options.KnownPolicies.Add("OtherPolicy");
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.StartAsync(TestContext.Current.CancellationToken));

        ex.Message.ShouldContain("OrderApprover");
        ex.Message.ShouldNotContain("Orders.ApproveOrderCommand");
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

    [Fact]
    public async Task StartAsync_NullKnownPolicies_TreatedAsEmpty_DoesNotNullReference() {
        // Pass 3 (E3) — `"KnownPolicies": null` in appsettings binds the property to null. Validator
        // must coalesce to an empty enumerable rather than NRE during host startup.
        FrontComposerAuthorizationOptions options = new() { KnownPolicies = null! };
        FrontComposerAuthorizationPolicyCatalogValidator sut = Create(options);

        await sut.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_StrictCatalogWithNullKnownPolicies_DoesNotThrow() {
        // Pass 3 (E3) follow-up — even with strict mode on, a null KnownPolicies binding is treated
        // identically to an empty list (no catalog configured). The Information/Warning path runs.
        FrontComposerAuthorizationOptions options = new() {
            KnownPolicies = null!,
            StrictPolicyCatalogValidation = true,
        };
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
