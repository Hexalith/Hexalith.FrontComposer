using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Registration;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Authorization;

public sealed class CommandDispatchAuthorizationGateTests {
    [Fact]
    public async Task EnsureAuthorizedAsync_UnprotectedCommand_DoesNotCallEvaluator() {
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        CommandDispatchAuthorizationGate sut = NewSut(Registry(policy: null), evaluator);

        await sut.EnsureAuthorizedAsync(new ProtectedCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        await evaluator.DidNotReceive().EvaluateAsync(
            Arg.Any<CommandAuthorizationRequest>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_ProtectedAllowed_UsesDirectDispatchSurface() {
        ProtectedCommand command = new();
        CommandAuthorizationRequest? captured = null;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Do<CommandAuthorizationRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Allowed("corr-allowed"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        await sut.EnsureAuthorizedAsync(command, TestContext.Current.CancellationToken).ConfigureAwait(true);

        captured.ShouldNotBeNull();
        captured.PolicyName.ShouldBe("OrderApprover");
        captured.SourceSurface.ShouldBe(CommandAuthorizationSurface.DirectDispatch);
        // Pass-4 BH-64 — assert the EXACT command instance flowed to the evaluator (not just type).
        captured.Command.ShouldBeSameAs(command);
        captured.BoundedContext.ShouldBe("Orders");
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_ProtectedDeclaredType_UsesDeclaredTypeWhenRuntimeTypeDiffers() {
        DerivedProtectedCommand command = new();
        CommandAuthorizationRequest? captured = null;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Do<CommandAuthorizationRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Allowed("corr-allowed"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        await sut.EnsureAuthorizedAsync<ProtectedCommand>(command, TestContext.Current.CancellationToken).ConfigureAwait(true);

        captured.ShouldNotBeNull();
        captured.CommandType.ShouldBe(typeof(ProtectedCommand));
        captured.Command.ShouldBeSameAs(command);
        captured.PolicyName.ShouldBe("OrderApprover");
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_ProtectedRuntimeType_FallsBackWhenDeclaredTypeIsBroad() {
        ProtectedCommand command = new();
        CommandAuthorizationRequest? captured = null;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Do<CommandAuthorizationRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Allowed("corr-allowed"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        await sut.EnsureAuthorizedAsync<object>(command, TestContext.Current.CancellationToken).ConfigureAwait(true);

        captured.ShouldNotBeNull();
        captured.CommandType.ShouldBe(typeof(ProtectedCommand));
        captured.Command.ShouldBeSameAs(command);
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_ProtectedPlainRegistry_UsesManifestPolicyFallback() {
        CommandAuthorizationRequest? captured = null;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Do<CommandAuthorizationRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Allowed("corr-allowed"));
        CommandDispatchAuthorizationGate sut = NewSut(PlainRegistry("OrderApprover"), evaluator);

        await sut.EnsureAuthorizedAsync(new ProtectedCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        captured.ShouldNotBeNull();
        captured.PolicyName.ShouldBe("OrderApprover");
        captured.BoundedContext.ShouldBe("Orders");
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_ProtectedDenied_ThrowsCommandWarningBeforeDispatch() {
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Denied("corr-denied"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        CommandWarningException ex = await Should.ThrowAsync<CommandWarningException>(
            async () => await sut.EnsureAuthorizedAsync(new ProtectedCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        ex.Kind.ShouldBe(CommandWarningKind.Forbidden);
        ex.Problem.Status.ShouldBe(403);
        // Pass-4 BH-31 / EH-41 — neither the policy name NOR the command FQN may leak in the
        // user-visible payload. The deny payload must stay opaque so attackers cannot enumerate
        // the policy graph by probing protected commands.
        ex.Problem.Detail!.ShouldNotContain("OrderApprover");
        ex.Problem.Detail!.ShouldNotContain(nameof(ProtectedCommand));
        ex.Problem.EntityLabel!.ShouldNotContain(nameof(ProtectedCommand));
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_ProtectedPending_ThrowsPendingNotForbidden() {
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Pending("corr-pending"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        CommandWarningException ex = await Should.ThrowAsync<CommandWarningException>(
            async () => await sut.EnsureAuthorizedAsync(new ProtectedCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        // Pass-4 DN-7-3-4-5 — Pending must surface as the dedicated retryable warning kind, not
        // collapse to Forbidden.
        ex.Kind.ShouldBe(CommandWarningKind.Pending);
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_EvaluatorThrows_ThrowsForbiddenWarning() {
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<CommandAuthorizationDecision>>(_ => throw new InvalidOperationException("broken evaluator"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        CommandWarningException ex = await Should.ThrowAsync<CommandWarningException>(
            async () => await sut.EnsureAuthorizedAsync(new ProtectedCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        ex.Kind.ShouldBe(CommandWarningKind.Forbidden);
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_Canceled_ThrowsOperationCanceledNotForbidden() {
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, "corr-cancel"));
        CommandDispatchAuthorizationGate sut = NewSut(Registry("OrderApprover"), evaluator);

        // Pass-4 DN-7-3-4-5 — Canceled decisions become OperationCanceledException so callers
        // can distinguish user-cancel from authorization denial.
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.EnsureAuthorizedAsync(new ProtectedCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);
    }

    [Fact]
    public async Task EnsureAuthorizedAsync_NullCommand_ThrowsArgumentNullException()
        => await Should.ThrowAsync<ArgumentNullException>(
            async () => await NewSut(Registry("OrderApprover"), Substitute.For<ICommandAuthorizationEvaluator>())
                .EnsureAuthorizedAsync<ProtectedCommand>(null!, TestContext.Current.CancellationToken)
                .ConfigureAwait(true)).ConfigureAwait(true);

    private static CommandDispatchAuthorizationGate NewSut(
        IFrontComposerRegistry registry,
        ICommandAuthorizationEvaluator evaluator) {
        ServiceCollection services = new();
        services.AddSingleton(evaluator);
        return new CommandDispatchAuthorizationGate(
            registry,
            services.BuildServiceProvider(),
            NullLogger<CommandDispatchAuthorizationGate>.Instance,
            new StubLocalizer());
    }

    private static IFrontComposerRegistry Registry(string? policy) {
        IFrontComposerCommandPolicyRegistry registry = Substitute.For<IFrontComposerCommandPolicyRegistry>();
        var manifest = new DomainManifest(
            "Orders",
            "Orders",
            [],
            [typeof(ProtectedCommand).FullName!],
            policy is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal) {
                    [typeof(ProtectedCommand).FullName!] = policy,
                });
        registry.GetManifests().Returns([manifest]);
        registry.TryGetCommandPolicy(typeof(ProtectedCommand).FullName!, out Arg.Any<string>(), out Arg.Any<string?>())
            .Returns(call => {
                call[1] = policy ?? string.Empty;
                call[2] = policy is null ? null : "Orders";
                return policy is not null;
            });
        return registry;
    }

    private static IFrontComposerRegistry PlainRegistry(string? policy)
        => new PlainFrontComposerRegistry(new DomainManifest(
            "Orders",
            "Orders",
            [],
            [typeof(ProtectedCommand).FullName!],
            policy is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal) {
                    [typeof(ProtectedCommand).FullName!] = policy,
                }));

    private class ProtectedCommand { }

    private sealed class DerivedProtectedCommand : ProtectedCommand { }

    private sealed class PlainFrontComposerRegistry(params DomainManifest[] manifests) : IFrontComposerRegistry {
        public void RegisterDomain(DomainManifest manifest) {
        }

        public void AddNavGroup(string name, string boundedContext) {
        }

        public IReadOnlyList<DomainManifest> GetManifests() => manifests;
    }

    /// <summary>Minimal IStringLocalizer that returns ResourceNotFound for every key, exercising the
    /// gate's static fallback strings (so tests don't depend on resx loading from another assembly).</summary>
    private sealed class StubLocalizer : IStringLocalizer<FcShellResources> {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: true);
        public LocalizedString this[string name, params object[] arguments] => new(name, name, resourceNotFound: true);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
