using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Authorization;

public sealed class CommandAuthorizationEvaluatorTests {
    [Fact]
    public async Task AuthenticationChangesDuringAuthorizationFailClosed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        MutableTestAuthenticationStateProvider provider = new();
        AuthenticationState state = new(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-a")], "Test")));
        provider.Publish(Task.FromResult(state));
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(_ => {
                provider.Publish(Task.FromResult(state));
                return Task.FromResult(AuthorizationResult.Success());
            });
        CommandAuthorizationEvaluator sut = new(authorization, provider, DefaultTenant(),
            NullLogger<CommandAuthorizationEvaluator>.Instance);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
    }

    [Fact]
    public async Task EvaluateAsync_NoPolicy_AllowsWithoutCallingAuthorizationService() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request(policy: null), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Reason.ShouldBe(CommandAuthorizationReason.NoPolicy);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public async Task EvaluateAsync_WhitespacePolicy_TreatedAsNoPolicy() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization);

        // NBSP-only is treated as whitespace and short-circuits to NoPolicy.
        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request(" "), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.Reason.ShouldBe(CommandAuthorizationReason.NoPolicy);
    }

    [Fact]
    public async Task EvaluateAsync_ProtectedCommand_Denied_FailsClosed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Failed(
                AuthorizationFailure.Failed([new TestRequirement()]))));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.Denied);
        result.Reason.ShouldBe(CommandAuthorizationReason.Denied);
    }

    [Fact]
    public async Task EvaluateAsync_ProtectedCommand_Allowed_PassesResourceWithTenantContext() {
        CommandAuthorizationResource? captured = null;
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Do<object>(r => captured = (CommandAuthorizationResource)r), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Success()));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured.CommandType.ShouldBe(typeof(TestCommand));
        captured.PolicyName.ShouldBe("OrderApprover");
        captured.BoundedContext.ShouldBe("Orders");
        captured.DisplayLabel.ShouldBe("Approve Order");
        captured.SourceSurface.ShouldBe(CommandAuthorizationSurface.DirectDispatch);
        captured.TenantContext.ShouldNotBeNull();
        captured.TenantContext.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task AllowedAuditIsOffByDefaultAsync()
    {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(AuthorizationResult.Success());
        CapturingLogger<CommandAuthorizationEvaluator> logger = new();
        CommandAuthorizationEvaluator sut = Create(authorization, logger: logger);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task OptedInAuditRecordsOnlySuccessfulNamedPoliciesAsync()
    {
        const string policy = "private-policy-name";
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), policy)
            .Returns(AuthorizationResult.Success());
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OtherPolicy")
            .Returns(AuthorizationResult.Failed());
        CapturingLogger<CommandAuthorizationEvaluator> logger = new();
        ServiceCollection services = [];
        services.AddLogging();
        services.AddHexalithFrontComposer();
        services.AddSingleton(authorization);
        services.AddScoped(_ => DefaultTenant());
        services.AddScoped<AuthenticationStateProvider>(_ => new FakeAuthenticationStateProvider(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-a")], "Test")))));
        services.AddSingleton<ILogger<CommandAuthorizationEvaluator>>(logger);
        services.Configure<FrontComposerAuthorizationOptions>(options => options.LogAllowedDecisions = true);
        await using ServiceProvider root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope scope = root.CreateAsyncScope();
        ICommandAuthorizationEvaluator sut = scope.ServiceProvider.GetRequiredService<ICommandAuthorizationEvaluator>();
        CommandAuthorizationRequest request = Request(policy) with {
            SourceSurface = CommandAuthorizationSurface.GeneratedForm,
            DisplayLabel = "private-display-label",
            Command = new TestCommand { SecretBlob = "private-command-payload" },
        };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        CommandAuthorizationDecision allowed = await sut.EvaluateAsync(request, cancellationToken);
        CommandAuthorizationDecision noPolicy = await sut.EvaluateAsync(request with { PolicyName = null }, cancellationToken);
        CommandAuthorizationDecision denied = await sut.EvaluateAsync(request with { PolicyName = "OtherPolicy" }, cancellationToken);

        allowed.IsAllowed.ShouldBeTrue();
        noPolicy.Reason.ShouldBe(CommandAuthorizationReason.NoPolicy);
        denied.IsAllowed.ShouldBeFalse();
        CapturedLogEntry entry = logger.Entries.Where(entry => entry.Level == LogLevel.Information).ShouldHaveSingleItem();
        entry.EventId.ShouldBe(new EventId(5692, "AuthorizationPolicySucceeded"));
        entry.Exception.ShouldBeNull();
        entry.State["Surface"].ShouldBe(nameof(CommandAuthorizationSurface.GeneratedForm));
        entry.State["CommandTypeDigest"].ShouldBe(ExpectedDigest(typeof(TestCommand).FullName!));
        entry.State["PolicyDigest"].ShouldBe(ExpectedDigest(policy));
        entry.State["CorrelationDigest"].ShouldBe(ExpectedDigest(allowed.CorrelationId));

        foreach (string rawValue in new[] { policy, "tenant-a", "user-a", "private-display-label",
            "private-command-payload", typeof(TestCommand).FullName!, allowed.CorrelationId })
        {
            entry.Message.ShouldNotContain(rawValue);
            entry.State.Values.OfType<string>().ShouldNotContain(text => text.Contains(rawValue, StringComparison.Ordinal));
        }

        static string ExpectedDigest(string value)
            => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 8)).ToLowerInvariant();
    }

    [Fact]
    public async Task OptionalAuditSinkFailurePreservesAllowedDecisionAsync()
    {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(AuthorizationResult.Success());
        ILogger<CommandAuthorizationEvaluator> logger = Substitute.For<ILogger<CommandAuthorizationEvaluator>>();
        logger.IsEnabled(LogLevel.Information).Returns(_ => throw new InvalidOperationException("Audit sink unavailable."));
        CommandAuthorizationEvaluator sut = Create(authorization, logger: logger,
            options: new FrontComposerAuthorizationOptions { LogAllowedDecisions = true });

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        logger.Received(1).IsEnabled(LogLevel.Information);
    }

    [Fact]
    public async Task PrincipalChangeDuringAuditStillFailsClosedAsync()
    {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(AuthorizationResult.Success());
        MutableTestAuthenticationStateProvider provider = new();
        provider.Publish(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity("Test")))));
        ILogger<CommandAuthorizationEvaluator> logger = Substitute.For<ILogger<CommandAuthorizationEvaluator>>();
        logger.IsEnabled(LogLevel.Information).Returns(_ => {
            provider.Publish(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
            return true;
        });
        CommandAuthorizationEvaluator sut = Create(authorization, provider, logger: logger,
            options: new FrontComposerAuthorizationOptions { LogAllowedDecisions = true });

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
    }

    [Fact]
    public async Task ContextTransitionDuringAuthorizationWithoutAuthEventFailsClosedBeforeAuditAsync()
    {
        TestTenantContextAccessor tenant = new()
        {
            TenantId = "tenant-a",
            UserId = "user-a",
        };
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(_ =>
            {
                tenant.TenantId = "tenant-b";
                return Task.FromResult(AuthorizationResult.Success());
            });
        CapturingLogger<CommandAuthorizationEvaluator> logger = new();
        CommandAuthorizationEvaluator sut = Create(
            authorization,
            tenant: tenant,
            logger: logger,
            options: new FrontComposerAuthorizationOptions { LogAllowedDecisions = true });

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
        logger.Entries.Where(entry => entry.Level == LogLevel.Information).ShouldBeEmpty();
        logger.Entries.Where(entry => entry.EventId.Id == 5672).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RevalidateNonFatalExceptionFailsClosedAsStaleTenantContextBeforeAuditAsync()
    {
        TenantContextSnapshot snapshot = new("tenant-a", "user-a", true, "corr-1");
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(TenantContextResult.Success(snapshot));
        tenant.Revalidate(Arg.Any<TenantContextSnapshot>(), Arg.Any<string>())
            .Returns<TenantContextResult>(_ => throw new InvalidOperationException("revalidation provider fault"));
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Success()));
        CapturingLogger<CommandAuthorizationEvaluator> logger = new();
        CommandAuthorizationEvaluator sut = Create(
            authorization,
            tenant: tenant,
            logger: logger,
            options: new FrontComposerAuthorizationOptions { LogAllowedDecisions = true });

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
        logger.Entries.Where(entry => entry.Level == LogLevel.Information).ShouldBeEmpty();
        logger.Entries.ShouldContain(entry => entry.EventId.Id == 5668);
    }

    [Fact]
    public async Task UncooperativeGetAuthenticationStateCancelsPromptlyUnsubscribesAndObservesLateFaultAsync()
    {
        InvalidOperationException lateFault = new("late uncooperative auth state fault");
        int unobserved = 0;
        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.Flatten().InnerExceptions.Any(ex => ReferenceEquals(ex, lateFault)))
            {
                Interlocked.Exchange(ref unobserved, 1);
                args.SetObserved();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            await RunUncooperativeGetAuthenticationStateAsync(lateFault, TestContext.Current.CancellationToken);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Volatile.Read(ref unobserved).ShouldBe(0);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    public async Task UncooperativeAuthorizeAsyncCancelsPromptlyUnsubscribesAndObservesLateFaultAsync()
    {
        InvalidOperationException lateFault = new("late uncooperative authorization fault");
        int unobserved = 0;
        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.Flatten().InnerExceptions.Any(ex => ReferenceEquals(ex, lateFault)))
            {
                Interlocked.Exchange(ref unobserved, 1);
                args.SetObserved();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            await RunUncooperativeAuthorizeAsync(lateFault, TestContext.Current.CancellationToken);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Volatile.Read(ref unobserved).ShouldBe(0);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    public async Task SynchronousGetAuthenticationStateCancelingTokenFailsClosedWithoutCallingTenantOrAuthorizationAsync()
    {
        using CancellationTokenSource cts = new();
        CallbackAuthenticationStateProvider provider = new(() =>
        {
            cts.Cancel();
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-a")], "Test"))));
        });
        IFrontComposerTenantContextAccessor tenant = DefaultTenant();
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization, provider, tenant);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), cts.Token);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
        _ = tenant.DidNotReceive().TryGetContext(Arg.Any<string?>(), Arg.Any<string>());
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ContextTransitionDuringAuditSinkWithoutAuthEventFailsClosedAsync()
    {
        TestTenantContextAccessor tenant = new()
        {
            TenantId = "tenant-a",
            UserId = "user-a",
        };
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(AuthorizationResult.Success());
        ILogger<CommandAuthorizationEvaluator> logger = Substitute.For<ILogger<CommandAuthorizationEvaluator>>();
        logger.IsEnabled(LogLevel.Information).Returns(_ =>
        {
            tenant.TenantId = "tenant-b";
            return true;
        });
        CommandAuthorizationEvaluator sut = Create(
            authorization,
            tenant: tenant,
            logger: logger,
            options: new FrontComposerAuthorizationOptions { LogAllowedDecisions = true });

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
    }

    [Fact]
    public async Task RevalidateReturningDifferentTenantIdFailsClosedAsStaleTenantContextBeforeAuditAsync()
    {
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(TenantContextResult.Success(new TenantContextSnapshot("tenant-a", "user-a", true, "corr-1")));
        tenant.Revalidate(Arg.Any<TenantContextSnapshot>(), Arg.Any<string>())
            .Returns(TenantContextResult.Success(new TenantContextSnapshot("tenant-b", "user-a", true, "corr-reval")));
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Success()));
        CapturingLogger<CommandAuthorizationEvaluator> logger = new();
        CommandAuthorizationEvaluator sut = Create(
            authorization,
            tenant: tenant,
            logger: logger,
            options: new FrontComposerAuthorizationOptions { LogAllowedDecisions = true });

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
        result.CorrelationId.ShouldBe("corr-reval");
        logger.Entries.Where(entry => entry.Level == LogLevel.Information).ShouldBeEmpty();
    }

    [Fact]
    public async Task FatalWrappedOperationCanceledAndFaultedFatalTasksPropagateAsync()
    {
        static OutOfMemoryException CreateFatal()
            => (OutOfMemoryException)RuntimeHelpers.GetUninitializedObject(typeof(OutOfMemoryException));

        CallbackAuthenticationStateProvider fatalCancelAuthProvider = new(
            () => throw new OperationCanceledException("canceled", CreateFatal()));
        CommandAuthorizationEvaluator authCancelSut = Create(Substitute.For<IAuthorizationService>(), fatalCancelAuthProvider);

        OperationCanceledException? authCancelEx = null;
        try
        {
            _ = await authCancelSut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            authCancelEx = ex;
        }

        authCancelEx.ShouldNotBeNull().InnerException.ShouldBeOfType<OutOfMemoryException>();

        IAuthorizationService fatalCancelAuthz = Substitute.For<IAuthorizationService>();
        fatalCancelAuthz.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns<Task<AuthorizationResult>>(_ => throw new OperationCanceledException("canceled", CreateFatal()));
        CommandAuthorizationEvaluator authzCancelSut = Create(fatalCancelAuthz);

        OperationCanceledException? authzCancelEx = null;
        try
        {
            _ = await authzCancelSut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            authzCancelEx = ex;
        }

        authzCancelEx.ShouldNotBeNull().InnerException.ShouldBeOfType<OutOfMemoryException>();

        using CancellationTokenSource authCts = new();
        CallbackAuthenticationStateProvider fatalFaultedAuthProvider = new(() =>
        {
            authCts.Cancel();
            return Task.FromException<AuthenticationState>(CreateFatal());
        });
        CommandAuthorizationEvaluator authFaultedSut = Create(Substitute.For<IAuthorizationService>(), fatalFaultedAuthProvider);

        _ = await Should.ThrowAsync<OutOfMemoryException>(
            async () => await authFaultedSut.EvaluateAsync(Request("OrderApprover"), authCts.Token).ConfigureAwait(false));

        using CancellationTokenSource authzCts = new();
        IAuthorizationService fatalFaultedAuthz = Substitute.For<IAuthorizationService>();
        fatalFaultedAuthz.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(_ =>
            {
                authzCts.Cancel();
                return Task.FromException<AuthorizationResult>(CreateFatal());
            });
        CommandAuthorizationEvaluator authzFaultedSut = Create(fatalFaultedAuthz);

        _ = await Should.ThrowAsync<OutOfMemoryException>(
            async () => await authzFaultedSut.EvaluateAsync(Request("OrderApprover"), authzCts.Token).ConfigureAwait(false));
    }

    [Fact]
    public async Task EvaluateAsync_UnauthenticatedPrincipal_FailsClosedBeforePolicyHandler() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization, authenticated: false);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Reason.ShouldBe(CommandAuthorizationReason.Unauthenticated);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public async Task EvaluateAsync_NullPrincipal_ReturnsPending() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        var stateProvider = new FakeAuthenticationStateProvider(state: null);
        CommandAuthorizationEvaluator sut = Create(authorization, stateProvider);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.Pending);
        result.Reason.ShouldBe(CommandAuthorizationReason.Pending);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationService_ThrowsInvalidOperationException_FailsClosedAsMissingPolicy() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "MissingPolicy")
            .Returns<Task<AuthorizationResult>>(_ => throw new InvalidOperationException("No policy found: MissingPolicy"));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("MissingPolicy"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.MissingPolicy);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationService_ReturnsFailedWithEmptyRequirements_MapsToMissingPolicy() {
        // Synthesize the rare case of an AuthorizationFailure with FailCalled=false and no failed
        // requirements (a registered policy that evaluates nothing). ASP.NET's
        // AuthorizationResult.Failed() with no args sets FailCalled=true, which is Denied not
        // MissingPolicy — the only way to reach the MissingPolicy-via-Failure branch is to provide
        // an explicit AuthorizationFailure with empty requirements collection.
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(Task.FromResult(AuthorizationResult.Failed(
                AuthorizationFailure.Failed(Array.Empty<IAuthorizationRequirement>()))));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.MissingPolicy);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationService_ReturnsNullResult_FailsClosedAsHandlerFailed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns(Task.FromResult<AuthorizationResult>(null!));
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.HandlerFailed);
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationHandler_ThrowsGenericException_FailsClosedAsHandlerFailed() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns<Task<AuthorizationResult>>(_ => throw new InvalidCastException("jwt.payload.signature"));
        CapturingLogger<CommandAuthorizationEvaluator> logger = new();
        CommandAuthorizationEvaluator sut = Create(authorization, logger: logger);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.HandlerFailed);
        CapturedLogEntry entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(5671);
        entry.Exception.ShouldBeNull();
        entry.Message.ShouldNotContain("jwt.payload.signature");
    }

    [Fact]
    public async Task EvaluateAsync_AuthorizationHandler_ThrowsOperationCanceled_MapsToCanceled() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object>(), "OrderApprover")
            .Returns<Task<AuthorizationResult>>(_ => throw new OperationCanceledException());
        CommandAuthorizationEvaluator sut = Create(authorization);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
    }

    [Fact]
    public async Task EvaluateAsync_TenantAccessorThrows_FailsClosedAsStaleTenantContext() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns<TenantContextResult>(_ => throw new NotImplementedException("buggy adopter accessor"));
        CommandAuthorizationEvaluator sut = Create(authorization, tenant: tenant);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
    }

    [Fact]
    public async Task EvaluateAsync_TenantContextStale_FailsClosedAsStaleTenantContext() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(TenantContextResult.Failure(TenantContextFailureCategory.SyntheticTenantRejected, "stale-corr"));
        CommandAuthorizationEvaluator sut = Create(authorization, tenant: tenant);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.Reason.ShouldBe(CommandAuthorizationReason.StaleTenantContext);
        result.CorrelationId.ShouldBe("stale-corr");
    }

    [Fact]
    public async Task EvaluateAsync_PreCancelled_ShortCircuitsAsCanceled() {
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), cts.Token);

        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
        await authorization.DidNotReceive().AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>());
    }

    [Fact]
    public void RequestRecord_RedactsCommandPayload_FromPrintMembers() {
        // Sentinel-laden command + principal claims must never appear in the request's auto-generated
        // ToString. Records auto-format every property; PrintMembers override redacts the Command field.
        const string sentinelToken = "JWT_LIKE_SENTINEL_eyJhbGciOiJI";
        const string sentinelClaim = "sentinel-tenant-id-leak";
        var command = new TestCommand { SecretBlob = sentinelToken };
        var request = new CommandAuthorizationRequest(
            typeof(TestCommand),
            "OrderApprover",
            command,
            "Orders",
            "Approve Order",
            CommandAuthorizationSurface.GeneratedForm);

        string text = request.ToString();

        text.ShouldNotContain(sentinelToken);
        text.ShouldNotContain(sentinelClaim);
        text.ShouldContain("<redacted>");
    }

    // Pass 3 / Pass-2 P5-partial — sentinel redaction across the second leak channel: the resource
    // record passed to IAuthorizationHandler. Custom handlers commonly log resource.ToString() at
    // Debug level; the auto-generated record formatter would otherwise expose the tenant snapshot.
    [Fact]
    public void ResourceRecord_RedactsTenantContext_FromPrintMembers() {
        const string sentinelTenant = "sentinel-tenant-id-leak";
        const string sentinelUser = "sentinel-user-id-leak";
        var resource = new CommandAuthorizationResource(
            typeof(TestCommand),
            "OrderApprover",
            "Orders",
            "Approve Order",
            CommandAuthorizationSurface.GeneratedForm,
            new TenantContextSnapshot(sentinelTenant, sentinelUser, true, "corr-sentinel"));

        string text = resource.ToString();

        text.ShouldNotContain(sentinelTenant);
        text.ShouldNotContain(sentinelUser);
        text.ShouldContain("<redacted>");
    }

    [Fact]
    public void ResourceRecord_WithNoTenantContext_FormatsAsNone() {
        var resource = new CommandAuthorizationResource(
            typeof(TestCommand),
            "OrderApprover",
            "Orders",
            "Approve Order",
            CommandAuthorizationSurface.GeneratedForm,
            TenantContext: null);

        string text = resource.ToString();

        text.ShouldContain("TenantContext = <none>");
    }

    // Pass 3 / Pass-2 P5-partial — third leak channel: structured-logger payload from LogBlocked.
    // Asserts no JWT/claim/tenant sentinels appear in the rendered log line.
    [Fact]
    public async Task EvaluateAsync_BlockedDecision_LogPayloadDoesNotLeakSentinels() {
        const string sentinelTenant = "sentinel-tenant-id-leak";
        const string sentinelUser = "sentinel-user-id-leak";
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        _ = authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed(AuthorizationFailure.ExplicitFail()));

        TenantContextSnapshot snapshot = new(sentinelTenant, sentinelUser, true, "corr-blocked");
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        _ = tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>())
            .Returns(TenantContextResult.Success(snapshot));
        _ = tenant.Revalidate(Arg.Any<TenantContextSnapshot>(), Arg.Any<string>())
            .Returns(TenantContextResult.Success(snapshot));

        CapturingLogger<CommandAuthorizationEvaluator> capturingLogger = new();
        ClaimsIdentity identity = new([new Claim(ClaimTypes.NameIdentifier, sentinelUser)], "Test");
        var stateProvider = new FakeAuthenticationStateProvider(new AuthenticationState(new ClaimsPrincipal(identity)));
        var sut = new CommandAuthorizationEvaluator(authorization, stateProvider, tenant, capturingLogger);

        CommandAuthorizationDecision result = await sut.EvaluateAsync(Request("OrderApprover"), TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        CapturedLogEntry entry = capturingLogger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(5672);
        entry.EventId.Name.ShouldBe("AuthorizationBlocked");
        entry.State["DiagnosticId"].ShouldBe("HFC2123");
        entry.Exception.ShouldBeNull();
        entry.Message.ShouldNotContain(sentinelTenant);
        entry.Message.ShouldNotContain(sentinelUser);
        entry.Message.ShouldNotContain("OrderApprover");
    }

    private static CommandAuthorizationEvaluator Create(
        IAuthorizationService authorizationService,
        AuthenticationStateProvider? stateProvider = null,
        IFrontComposerTenantContextAccessor? tenant = null,
        bool authenticated = true,
        ILogger<CommandAuthorizationEvaluator>? logger = null,
        FrontComposerAuthorizationOptions? options = null) {
        ArgumentNullException.ThrowIfNull(authorizationService);

        if (stateProvider is null) {
            ClaimsIdentity identity = authenticated
                ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-a")], "Test")
                : new ClaimsIdentity();
            stateProvider = new FakeAuthenticationStateProvider(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        tenant ??= DefaultTenant();

        return new CommandAuthorizationEvaluator(
            authorizationService,
            stateProvider,
            tenant,
            logger ?? NullLogger<CommandAuthorizationEvaluator>.Instance,
            Microsoft.Extensions.Options.Options.Create(options ?? new FrontComposerAuthorizationOptions()));
    }

    private static IFrontComposerTenantContextAccessor DefaultTenant()
    {
        TenantContextSnapshot snapshot = new("tenant-a", "user-a", true, "corr-1");
        IFrontComposerTenantContextAccessor tenant = Substitute.For<IFrontComposerTenantContextAccessor>();
        tenant.TryGetContext(Arg.Any<string?>(), Arg.Any<string>()).Returns(TenantContextResult.Success(snapshot));
        tenant.Revalidate(Arg.Any<TenantContextSnapshot>(), Arg.Any<string>()).Returns(TenantContextResult.Success(snapshot));
        return tenant;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task RunUncooperativeGetAuthenticationStateAsync(
        InvalidOperationException lateFault,
        CancellationToken testCancellationToken)
    {
        PendingAuthenticationStateProvider provider = new();
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        CommandAuthorizationEvaluator sut = Create(authorization, provider);
        using CancellationTokenSource cts = new();

        Task<CommandAuthorizationDecision> evaluation = sut.EvaluateAsync(Request("OrderApprover"), cts.Token);
        await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2), testCancellationToken).ConfigureAwait(false);
        AuthenticationStateChangedSubscriberCount(provider).ShouldBe(1);

        cts.Cancel();

        CommandAuthorizationDecision result = await evaluation.WaitAsync(TimeSpan.FromSeconds(2), testCancellationToken).ConfigureAwait(false);
        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
        AuthenticationStateChangedSubscriberCount(provider).ShouldBe(0);
        provider.Fault(lateFault);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task RunUncooperativeAuthorizeAsync(
        InvalidOperationException lateFault,
        CancellationToken testCancellationToken)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<AuthorizationResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IAuthorizationService authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), "OrderApprover")
            .Returns(_ =>
            {
                entered.TrySetResult();
                return completion.Task;
            });
        FakeAuthenticationStateProvider provider = new(new AuthenticationState(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-a")], "Test"))));
        CommandAuthorizationEvaluator sut = Create(authorization, provider);
        using CancellationTokenSource cts = new();

        Task<CommandAuthorizationDecision> evaluation = sut.EvaluateAsync(Request("OrderApprover"), cts.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), testCancellationToken).ConfigureAwait(false);
        AuthenticationStateChangedSubscriberCount(provider).ShouldBe(1);

        cts.Cancel();

        CommandAuthorizationDecision result = await evaluation.WaitAsync(TimeSpan.FromSeconds(2), testCancellationToken).ConfigureAwait(false);
        result.IsAllowed.ShouldBeFalse();
        result.Kind.ShouldBe(CommandAuthorizationDecisionKind.FailedClosed);
        result.Reason.ShouldBe(CommandAuthorizationReason.Canceled);
        AuthenticationStateChangedSubscriberCount(provider).ShouldBe(0);
        completion.TrySetException(lateFault).ShouldBeTrue();
    }

    private static int AuthenticationStateChangedSubscriberCount(AuthenticationStateProvider provider)
    {
        FieldInfo eventField = typeof(AuthenticationStateProvider)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(static field => typeof(Delegate).IsAssignableFrom(field.FieldType));
        return (eventField.GetValue(provider) as Delegate)?.GetInvocationList().Length ?? 0;
    }

    private static CommandAuthorizationRequest Request(string? policy)
        => new(typeof(TestCommand), policy, new TestCommand(), "Orders", "Approve Order");

    private sealed class TestCommand {
        public string? SecretBlob { get; set; }
    }

    private sealed class TestRequirement : IAuthorizationRequirement { }

    private sealed class FakeAuthenticationStateProvider(AuthenticationState? state) : AuthenticationStateProvider {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(state!);
    }

    private sealed class PendingAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly TaskCompletionSource<AuthenticationState> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Fault(Exception exception) => _completion.TrySetException(exception);

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            Entered.TrySetResult();
            return _completion.Task;
        }
    }

    private sealed class CallbackAuthenticationStateProvider(
        Func<Task<AuthenticationState>> getState) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => getState();
    }
}
