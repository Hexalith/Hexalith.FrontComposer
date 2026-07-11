using System.Collections.Immutable;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Testing.Tests;

public sealed class TestingFailureModeTests {
    [Fact]
    public async Task TestCommandService_ConfiguredFailureModes_EmitOnlyImpliedLifecycleStates() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);

        host.CommandService.Reject("invalid", "fix it");
        CommandRejectedException rejection = await Should.ThrowAsync<CommandRejectedException>(
            () => host.CommandService.DispatchAsync(new TestCommand(), Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);
        rejection.Message.ShouldBe("invalid");
        rejection.Resolution.ShouldBe("fix it");
        host.CommandService.Evidence.Single().LifecycleStates.ShouldBe([CommandLifecycleState.Rejected]);

        host.CommandService.Timeout();
        await Should.ThrowAsync<TimeoutException>(
            () => host.CommandService.DispatchAsync(new TestCommand(), Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);
        host.CommandService.Evidence.Last().LifecycleStates.ShouldBe([CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing]);

        host.CommandService.StallAtSyncing();
        CommandResult stalled = await host.CommandService.DispatchAsync(new TestCommand(), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        stalled.Status.ShouldBe(CommandResultStatus.Accepted);
        host.CommandService.Evidence.Last().LifecycleStates.ShouldNotContain(CommandLifecycleState.Confirmed);
        host.CommandService.Evidence.Select(item => item.MessageId).ShouldBe(["test-message-0001", "test-message-0002", "test-message-0003"]);
    }

    [Fact]
    public async Task TestCommandService_LifecycleCallbackThrows_StillCapturesEvidence() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);

        await Should.ThrowAsync<InvalidOperationException>(() => host.CommandService.DispatchAsync(
            new TestCommand(),
            (_, _) => throw new InvalidOperationException("callback failed"),
            Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);

        host.CommandService.Evidence.Single().Status.ShouldBe("LifecycleCallbackFailed");
        host.CommandService.Evidence.Single().LifecycleStates.ShouldBe([CommandLifecycleState.Acknowledged]);
    }

    [Fact]
    public async Task TestQueryService_CallbackIsLastWriteWins_AndFailuresAreRecorded() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        host.QueryService.SucceedWith<string>(request => new([request.SearchQuery!], request.Take!.Value, "callback"));
        QueryRequest request = new("Projection", "tenant", Skip: 4, Take: 2, SearchQuery: "needle", SortColumn: "Name", SortDescending: true);

        QueryResult<string> result = await host.QueryService.QueryAsync<string>(request, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Items.ShouldBe(["needle"]);
        host.QueryService.Evidence.Last().Mode.ShouldBe("callback");

        host.QueryService.SucceedWith<string>(["static"]);
        result = await host.QueryService.QueryAsync<string>(request, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Items.ShouldBe(["static"]);

        host.QueryService.SucceedWith<string>(_ => throw new InvalidOperationException("configured failure"));
        await Should.ThrowAsync<InvalidOperationException>(
            () => host.QueryService.QueryAsync<string>(request, Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);
        host.QueryService.Evidence.Last().Mode.ShouldBe("callback-failed");
    }

    [Fact]
    public async Task TestProjectionPageLoader_CallbackReceivesAllInputs_AndStaticConfigurationWinsLast() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        ProjectionPageRequest? captured = null;
        host.PageLoader.SucceedWith("Projection", request => {
            captured = request;
            return new ProjectionPageResult([request.SearchQuery!], 12, "callback");
        });
        ImmutableDictionary<string, string> filters = ImmutableDictionary<string, string>.Empty.Add("Status", "Open");

        ProjectionPageResult result = await host.PageLoader.LoadPageAsync(
            "Projection", 6, 3, filters, "Name", true, "needle", Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        captured.ShouldNotBeNull();
        captured.ShouldBe(new ProjectionPageRequest("Projection", 6, 3, filters, "Name", true, "needle"));
        result.Items.ShouldBe(["needle"]);

        host.PageLoader.NotModified("Projection", ["cached"], 1, "etag");
        result = await host.PageLoader.LoadPageAsync(
            "Projection", 0, 10, ImmutableDictionary<string, string>.Empty, null, false, null, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.IsNotModified.ShouldBeTrue();
    }

    [Fact]
    public async Task QueryAndPageCallbacks_OperationCanceledException_ProduceCanceledTasksAndEvidence() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        using CancellationTokenSource canceled = new();
        canceled.Cancel();
        host.QueryService.SucceedWith<string>(_ => throw new OperationCanceledException(canceled.Token));
        host.PageLoader.SucceedWith("Projection", _ => throw new OperationCanceledException(canceled.Token));

        Task<QueryResult<string>> queryTask = host.QueryService.QueryAsync<string>(
            new QueryRequest("Projection", "tenant"), Xunit.TestContext.Current.CancellationToken);
        Task<ProjectionPageResult> pageTask = host.PageLoader.LoadPageAsync(
            "Projection", 0, 1, ImmutableDictionary<string, string>.Empty, null, false, null, Xunit.TestContext.Current.CancellationToken);
        await Should.ThrowAsync<OperationCanceledException>(() => queryTask).ConfigureAwait(true);
        await Should.ThrowAsync<OperationCanceledException>(() => pageTask).ConfigureAwait(true);
        queryTask.IsCanceled.ShouldBeTrue();
        pageTask.IsCanceled.ShouldBeTrue();
        host.QueryService.Evidence.Last().Mode.ShouldBe("callback-failed");
        host.PageLoader.Evidence.Last().Mode.ShouldBe("callback-failed");
    }

    [Fact]
    public async Task TestProjectionPageLoader_InvalidRequiredInputs_FailFast() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        await Should.ThrowAsync<ArgumentException>(() => host.PageLoader.LoadPageAsync(
            " ", 0, 1, ImmutableDictionary<string, string>.Empty, null, false, null, Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);
        await Should.ThrowAsync<ArgumentNullException>(() => host.PageLoader.LoadPageAsync(
            "Projection", 0, 1, null!, null, false, null, Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    [Fact]
    public async Task TestAuthorizationEvaluator_UnknownAndConfiguredStates_FailClosedOrMatchConfiguration() {
        TestAuthorizationEvaluator evaluator = new();
        CommandAuthorizationRequest request = new(typeof(TestCommand), "unknown", null, "Test", "Test");
        (await evaluator.EvaluateAsync(request, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).Reason.ShouldBe(CommandAuthorizationReason.MissingPolicy);

        evaluator.Allow("allowed");
        evaluator.Deny("denied");
        evaluator.Pending("pending");
        evaluator.Block("unauthenticated", CommandAuthorizationReason.Unauthenticated);
        evaluator.DecideWith("handler-failed", _ => throw new InvalidOperationException("failed"));
        (await evaluator.EvaluateAsync(request with { PolicyName = "allowed" }, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).Kind.ShouldBe(CommandAuthorizationDecisionKind.Allowed);
        (await evaluator.EvaluateAsync(request with { PolicyName = "denied" }, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).Kind.ShouldBe(CommandAuthorizationDecisionKind.Denied);
        (await evaluator.EvaluateAsync(request with { PolicyName = "pending" }, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).Kind.ShouldBe(CommandAuthorizationDecisionKind.Pending);
        (await evaluator.EvaluateAsync(request with { PolicyName = "unauthenticated" }, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).Reason.ShouldBe(CommandAuthorizationReason.Unauthenticated);
        (await evaluator.EvaluateAsync(request with { PolicyName = "handler-failed" }, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true)).Reason.ShouldBe(CommandAuthorizationReason.HandlerFailed);
    }

    [Fact]
    public void AddFrontComposerTestHost_ConfiguredAuthorization_RendersPolicyGatedRegion() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        host.AuthorizationEvaluator.Allow("Specimens.PolicyAllowed");

        IRenderedComponent<FcAuthorizedCommandRegion> rendered = context.Render<FcAuthorizedCommandRegion>(parameters => parameters
            .Add(component => component.CommandType, typeof(TestCommand))
            .Add(component => component.PolicyName, "Specimens.PolicyAllowed")
            .Add(component => component.Authorized, "allowed")
            .Add(component => component.Pending, "pending")
            .Add(component => component.NotAuthorized, "denied"));

        rendered.WaitForAssertion(() => rendered.Markup.ShouldContain("allowed"));
        rendered.Markup.ShouldNotContain("denied");
    }

    [Fact]
    public void AddFrontComposerTestHost_AuthorizationFailureStates_RenderExpectedBranches() {
        AssertRegion("denied", evaluator => evaluator.Deny("denied"), "denied");
        AssertRegion("pending", evaluator => evaluator.Pending("pending"), "pending");
        AssertRegion("", _ => { }, "denied");
        AssertRegion("unauthenticated", evaluator => evaluator.Block("unauthenticated", CommandAuthorizationReason.Unauthenticated), "denied");
        AssertRegion("handler-failed", evaluator => evaluator.DecideWith("handler-failed", _ => throw new InvalidOperationException("failed")), "denied");

        static void AssertRegion(string policy, Action<TestAuthorizationEvaluator> configure, string expected) {
            using BunitContext context = new();
            using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
            configure(host.AuthorizationEvaluator);
            IRenderedComponent<FcAuthorizedCommandRegion> rendered = context.Render<FcAuthorizedCommandRegion>(parameters => parameters
                .Add(component => component.CommandType, typeof(TestCommand))
                .Add(component => component.PolicyName, policy)
                .Add(component => component.Authorized, "allowed")
                .Add(component => component.Pending, "pending")
                .Add(component => component.NotAuthorized, "denied"));
            rendered.WaitForAssertion(() => rendered.Markup.ShouldContain(expected));
        }
    }

    [Fact]
    public void TestAuthorizationEvaluator_BlockRejectsContradictoryReasons() {
        TestAuthorizationEvaluator evaluator = new();
        Should.Throw<ArgumentOutOfRangeException>(() => evaluator.Block("policy", CommandAuthorizationReason.None));
        Should.Throw<ArgumentOutOfRangeException>(() => evaluator.Block("policy", CommandAuthorizationReason.Denied));
        Should.Throw<ArgumentOutOfRangeException>(() => evaluator.Block("policy", CommandAuthorizationReason.Pending));
    }

    [Fact]
    public void TestFaultEvidenceRecorder_CorrelationIdIsPlainBoundedAndRedacted() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context, options => {
            options.TestTenantId = "tenant-private";
            options.TestUserId = "user-private";
        });

        FaultEvidence ordinary = host.FaultRecorder.RecordDrop("correlation-1");
        FaultEvidence sensitive = host.FaultRecorder.RecordDelay("tenant-private/user-private");
        ordinary.CorrelationId.ShouldBe("correlation-1");
        sensitive.TenantId.ShouldBe("<tenant>");
        sensitive.UserId.ShouldBe("<user>");
        sensitive.CorrelationId.ShouldBe("<tenant>/<user>");
        sensitive.CorrelationId.ShouldNotContain("tenant-private");
        sensitive.CorrelationId.ShouldNotContain("user-private");
    }

    [Fact]
    public async Task QueryAndPageConfiguration_ConcurrentWrites_KeepOneAtomicFinalEntry() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        Task[] writers = Enumerable.Range(0, 32).Select(index => Task.Run(() => {
            if ((index & 1) == 0) {
                host.QueryService.SucceedWith<string>([index.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
                host.PageLoader.SucceedWith("Projection", [index]);
            }
            else {
                host.QueryService.SucceedWith<string>(_ => new(["callback"], 1, null));
                host.PageLoader.SucceedWith("Projection", _ => new ProjectionPageResult(["callback"], 1, null));
            }
        }, Xunit.TestContext.Current.CancellationToken)).ToArray();
        await Task.WhenAll(writers).ConfigureAwait(true);

        host.QueryService.NotModifiedWith<string>(["final"], "final");
        host.PageLoader.NotModified("Projection", ["final"], 1, "final");
        QueryResult<string> query = await host.QueryService.QueryAsync<string>(
            new QueryRequest("Projection", "tenant"), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        ProjectionPageResult page = await host.PageLoader.LoadPageAsync(
            "Projection", 0, 1, ImmutableDictionary<string, string>.Empty, null, false, null,
            Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        query.IsNotModified.ShouldBeTrue();
        query.Items.ShouldBe(["final"]);
        page.IsNotModified.ShouldBeTrue();
        page.Items.ShouldBe(["final"]);
    }

    [Fact]
    public void Builders_AndCommandEvidenceAssertions_WorkDirectly() {
        BuilderModel model = new ProjectionTestDataBuilder<BuilderModel>()
            .With(item => item.Name, "configured")
            .Build();
        model.Name.ShouldBe("configured");

        CommandDispatchEvidence evidence = new(
            typeof(TestCommand).FullName!, "<tenant>", "<user>", "Test", "Test", "message", "correlation",
            CommandResultStatus.Accepted, [CommandLifecycleState.Confirmed], DateTimeOffset.UnixEpoch, "{\"tenant\":\"<tenant>\"}");
        Should.NotThrow(() => CommandEvidenceAssertions.AssertLifecycleContains(evidence, CommandLifecycleState.Confirmed));
        Should.NotThrow(() => CommandEvidenceAssertions.AssertRedacted(evidence, "raw-tenant", "raw-user"));
    }

    [Fact]
    public void FrontComposerTestBase_DuringHostSetup_DirectsAdoptersToExplicitAsyncInitialization() {
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => new DuringSetupTestBase());
        exception.Message.ShouldContain("InitializeStoreAsync");
        exception.Message.ShouldContain(nameof(StoreInitializationMode.OnDemand));
    }

    [Fact]
    public async Task AddFrontComposerTestHostAsync_CanceledSetup_ThrowsCancellationAndPreservesCulture() {
        await using BunitContext context = new();
        System.Globalization.CultureInfo original = System.Globalization.CultureInfo.CurrentCulture;
        using CancellationTokenSource canceled = new();
        canceled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => context.Services.AddFrontComposerTestHostAsync(
            context,
            options => {
                options.StoreInitialization = StoreInitializationMode.DuringHostSetup;
                options.Culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            },
            canceled.Token)).ConfigureAwait(true);
        System.Globalization.CultureInfo.CurrentCulture.ShouldBe(original);
    }

    private sealed class TestCommand;

    private sealed class BuilderModel {
        public string? Name { get; set; }
    }

    private sealed class DuringSetupTestBase : FrontComposerTestBase {
        public DuringSetupTestBase()
            : base(options => options.StoreInitialization = StoreInitializationMode.DuringHostSetup) {
        }
    }
}
