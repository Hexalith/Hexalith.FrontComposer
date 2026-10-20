using System.Collections.Immutable;
using System.Reflection;
using System.Globalization;
using System.Text.Json;

using AngleSharp.Dom;

using Hexalith.FrontComposer.Shell.Components.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.EventStore;
using Hexalith.FrontComposer.Shell.Components.Forms;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Announcements;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Validation;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

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
        host.CommandService.Evidence[^1].LifecycleStates.ShouldBe([CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing]);

        host.CommandService.StallAtSyncing();
        CommandResult stalled = await host.CommandService.DispatchAsync(new TestCommand(), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        stalled.Status.ShouldBe(CommandResultStatus.Accepted);
        host.CommandService.Evidence[^1].LifecycleStates.ShouldNotContain(CommandLifecycleState.Confirmed);
        host.CommandService.Evidence.Select(item => item.MessageId).Distinct(StringComparer.Ordinal).Count().ShouldBe(3);
    }

    [Theory]
    [InlineData(TestCommandOutcome.Success)]
    [InlineData(TestCommandOutcome.Rejected)]
    [InlineData(TestCommandOutcome.Timeout)]
    [InlineData(TestCommandOutcome.StallAtSyncing)]
    public async Task TestCommandService_SerializationFailure_PreservesEveryConfiguredOutcome(TestCommandOutcome outcome) {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        ConfigureOutcome(host.CommandService, outcome);
        CyclicPayload command = new();
        command.Self = command;
        CommandResult? commandResult = null;

        Exception? exception = await Record.ExceptionAsync(async () =>
            commandResult = await host.CommandService.DispatchAsync(command, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true));

        switch (outcome) {
            case TestCommandOutcome.Success:
            case TestCommandOutcome.StallAtSyncing:
                exception.ShouldBeNull();
                commandResult.ShouldNotBeNull();
                commandResult.Status.ShouldBe(CommandResultStatus.Accepted);
                break;
            case TestCommandOutcome.Rejected:
                _ = exception.ShouldBeOfType<CommandRejectedException>();
                commandResult.ShouldBeNull();
                break;
            case TestCommandOutcome.Timeout:
                _ = exception.ShouldBeOfType<TimeoutException>();
                commandResult.ShouldBeNull();
                break;
            default:
                throw new InvalidOperationException("Unsupported test outcome.");
        }

        CommandDispatchEvidence evidence = host.CommandService.Evidence.ShouldHaveSingleItem();
        evidence.RedactedPayload.ShouldBe("<serialization-unavailable>");
        evidence.Status.ShouldBe(outcome switch {
            TestCommandOutcome.Success => CommandResultStatus.Accepted,
            TestCommandOutcome.Rejected => CommandResultStatus.Rejected,
            _ => outcome.ToString(),
        });
    }

    [Theory]
    [InlineData("cyclic")]
    [InlineData("unsupported")]
    [InlineData("throwing")]
    [InlineData("aggregate")]
    public void RedactedEvidenceFormatter_NonFatalSerializationFailures_ReturnStablePayloadFreeMarker(string failureMode) {
        object payload = failureMode switch {
            "cyclic" => CreateCyclicPayload(),
            "unsupported" => new UnsupportedPayload(),
            "throwing" => new ThrowingPayload(),
            "aggregate" => new ExceptionalPayload(new AggregateException(
                "aggregate-secret",
                new InvalidOperationException("payload-secret"))),
            _ => throw new InvalidOperationException("Unsupported serialization fixture."),
        };

        string evidence = RedactedEvidenceFormatter.Format(payload, new FrontComposerTestOptions());

        evidence.ShouldBe("<serialization-unavailable>");
        evidence.ShouldNotContain("payload-secret");

        RedactedEvidenceFormatter.Format(payload, new FrontComposerTestOptions {
            TestTenantId = "serialization",
        }).ShouldBe("<serialization-unavailable>");
        RedactedEvidenceFormatter.Format(payload, new FrontComposerTestOptions {
            TestUserId = "serialization-unavailable",
        }).ShouldBe("<serialization-unavailable>");
        RedactedEvidenceFormatter.Format(payload, new FrontComposerTestOptions {
            MaxDiagnosticPayloadCharacters = 1,
        }).ShouldBe("<serialization-unavailable>");
    }

    [Theory]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(StackOverflowException))]
    [InlineData(typeof(System.Threading.ThreadAbortException))]
    [InlineData(typeof(AccessViolationException))]
    public void RedactedEvidenceFormatter_FatalSerializationFailure_Propagates(Type exceptionType) {
        Exception expected = (Exception)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(exceptionType);

        Exception? actual = Record.Exception(() =>
            RedactedEvidenceFormatter.Format(new ExceptionalPayload(expected), new FrontComposerTestOptions()));

        actual.ShouldNotBeNull();
        actual.ShouldBeSameAs(expected);
        actual.GetType().ShouldBe(exceptionType);

        AggregateException aggregate = new(
            "Aggregate serialization failure.",
            new InvalidOperationException("Non-fatal sibling."),
            expected);
        Exception? aggregateActual = Record.Exception(() =>
            RedactedEvidenceFormatter.Format(new ExceptionalPayload(aggregate), new FrontComposerTestOptions()));
        aggregateActual.ShouldBeSameAs(aggregate);

        InvalidOperationException wrapper = new("Wrapped fatal serialization failure.", expected);
        Exception? wrapperActual = Record.Exception(() =>
            RedactedEvidenceFormatter.Format(new ExceptionalPayload(wrapper), new FrontComposerTestOptions()));
        wrapperActual.ShouldBeSameAs(wrapper);
    }

    [Fact]
    public void RedactedEvidenceFormatter_OperationCanceledSerializationFailure_Propagates() {
        OperationCanceledException expected = new("Serialization was canceled.");

        Exception? actual = Record.Exception(() =>
            RedactedEvidenceFormatter.Format(new ExceptionalPayload(expected), new FrontComposerTestOptions()));

        actual.ShouldBeSameAs(expected);

        AggregateException aggregate = new(
            "Aggregate serialization cancellation.",
            new InvalidOperationException("Non-fatal sibling."),
            expected);
        Exception? aggregateActual = Record.Exception(() =>
            RedactedEvidenceFormatter.Format(new ExceptionalPayload(aggregate), new FrontComposerTestOptions()));
        aggregateActual.ShouldBeSameAs(aggregate);

        InvalidOperationException wrapper = new("Wrapped serialization cancellation.", expected);
        Exception? wrapperActual = Record.Exception(() =>
            RedactedEvidenceFormatter.Format(new ExceptionalPayload(wrapper), new FrontComposerTestOptions()));
        wrapperActual.ShouldBeSameAs(wrapper);
    }

    [Fact]
    public async Task TestCommandService_ExhaustedIdentitySequence_FailsInsteadOfWrapping() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        FieldInfo sequence = typeof(TestCommandService).GetField("_dispatchSequence", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Test command sequence field was not found.");
        sequence.SetValue(host.CommandService, long.MaxValue);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            host.CommandService.DispatchAsync(new TestCommand(), Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);

        exception.Message.ShouldContain("exhausted");
        host.CommandService.Evidence.ShouldBeEmpty();
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
        host.QueryService.SucceedWith<string>(request => new([request.Criteria.SearchQuery!], request.Criteria.Take!.Value, "callback"));
        QueryRequest request = QueryRequest.Create(new ProjectionQuery("Projection", Skip: 4, Take: 2, SearchQuery: "needle", SortColumn: "Name", SortDescending: true), "tenant");

        QueryResult<string> result = await host.QueryService.QueryAsync<string>(request, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Items.ShouldBe(["needle"]);
        host.QueryService.Evidence[^1].Mode.ShouldBe("callback");

        host.QueryService.SucceedWith<string>(["static"]);
        result = await host.QueryService.QueryAsync<string>(request, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Items.ShouldBe(["static"]);

        host.QueryService.SucceedWith<string>(_ => throw new InvalidOperationException("configured failure"));
        await Should.ThrowAsync<InvalidOperationException>(
            () => host.QueryService.QueryAsync<string>(request, Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);
        host.QueryService.Evidence[^1].Mode.ShouldBe("callback-failed");
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
            QueryRequest.Create(new ProjectionQuery("Projection"), "tenant"), Xunit.TestContext.Current.CancellationToken);
        Task<ProjectionPageResult> pageTask = host.PageLoader.LoadPageAsync(
            "Projection", 0, 1, ImmutableDictionary<string, string>.Empty, null, false, null, Xunit.TestContext.Current.CancellationToken);
        await Should.ThrowAsync<OperationCanceledException>(() => queryTask).ConfigureAwait(true);
        await Should.ThrowAsync<OperationCanceledException>(() => pageTask).ConfigureAwait(true);
        queryTask.IsCanceled.ShouldBeTrue();
        pageTask.IsCanceled.ShouldBeTrue();
        host.QueryService.Evidence[^1].Mode.ShouldBe("callback-failed");
        host.PageLoader.Evidence[^1].Mode.ShouldBe("callback-failed");
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
            QueryRequest.Create(new ProjectionQuery("Projection"), "tenant"), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
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
        BunitContext context = new();
        await using var contextLifetime = context.ConfigureAwait(true);
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

    [Fact]
    public async Task UxAssertions_UnmappedRejection_UsesCanonicalSequenceAndRecoveryState()
    {
        using BunitContext context = new();
        TestTimeProvider clock = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context, o => o.TimeProvider = clock);
        ILifecycleStateService lifecycle = context.Services.GetRequiredService<ILifecycleStateService>();
        ISurfaceAnnouncementCoordinator announcements = context.Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        const string operation = "rejected-operation";
        UxAnnouncement syncing = UxAnnouncementCatalog.Expected("AM-12", operation, "Syncing");
        UxAnnouncement rejected = UxAnnouncementCatalog.Expected("AM-14", operation, "Rejected");
        List<UxAnnouncement> spoken = [];
        using IDisposable subscription = announcements.Subscribe("lifecycle:" + operation, text =>
        {
            spoken.Add((text == rejected.Copy ? rejected : syncing) with { Copy = text });
        });
        IRenderedComponent<FcLifecycleWrapper> cut = context.Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, operation).Add(c => c.RejectionMessage, "Review the command and retry."));
        await cut.InvokeAsync(() =>
        {
            lifecycle.Transition(operation, CommandLifecycleState.Submitting, "message");
            lifecycle.Transition(operation, CommandLifecycleState.Acknowledged, "message");
            lifecycle.Transition(operation, CommandLifecycleState.Syncing, "message");
            UxAnnouncementAssertions.AssertCoalescingBoundary(clock, () => spoken, syncing);
            lifecycle.Transition(operation, CommandLifecycleState.Rejected, "message");
            UxAnnouncementAssertions.AssertImmediateCancellation(clock, () => spoken, rejected, syncing);
        });
        IElement status = cut.Find("[role='status']");
        UxAnnouncementAssertions.AssertChannel(rejected, new(status.GetAttribute("role")!, status.TextContent.Trim(), false,
            status.GetAttribute("aria-live"), status.GetAttribute("aria-atomic")), status.TextContent.Trim());
        UxSurfaceSnapshot state = new("Rejected", true, new("group", "Command recovery", false), lifecycle.GetState(operation), Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true });
        UxStateAssertions.AssertState("SS-20", state);
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-19", state));
        lifecycle.GetState(operation).ShouldBe(CommandLifecycleState.Rejected);
        cut.FindAll("[role='group'][aria-live='polite']").ShouldBeEmpty();
    }

    [Fact]
    public async Task UxAssertions_Offline_RecordsEveryAeDimensionWithoutSensitiveText()
    {
        using BunitContext context = new();
        TestTimeProvider clock = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context, o =>
        {
            o.TimeProvider = clock;
            o.MaxDiagnosticPayloadCharacters = 8_192;
            o.MaxEvidenceRecords = 9;
        });
        IRenderedComponent<FcProjectionConnectionStatus> cut = context.Render<FcProjectionConnectionStatus>();
        await cut.InvokeAsync(() => cut.Instance.OnBrowserConnectivityChanged(false));
        UxAnnouncement offline = UxAnnouncementCatalog.Expected("AM-30", "connection", "offline", variant: "offline");
        cut.FindAll("[role='group']").ShouldContain(e => e.TextContent.Contains(offline.Copy, StringComparison.Ordinal));
        UxStateAssertions.AssertState("SS-35", new("Offline", false, new("group", offline.Copy, false), Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, StateCueVisible = true }));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-08", new("Offline", false, new("group", offline.Copy, false), Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, StateCueVisible = true })));

        AccessibilityTargetEvidence target = new(1, new(20, 100, 18, 18), [UxKeyboardKey.Tab, UxKeyboardKey.Enter],
            UxTargetSizeExceptionKind.Equivalent, UxTargetSizeRationale.SameFunctionConformingControl,
            EquivalentControlNumber: 2, EquivalentControlBounds: new(60, 100, 32, 32));
        UxAccessibilityAssertions.AssertTargetSize(target);
        foreach (UxRect undersized in new[] { new UxRect(20, 100, 32, 12), new UxRect(20, 100, 12, 32) })
        {
            Should.Throw<InvalidOperationException>(() => UxAccessibilityAssertions.AssertTargetSize(new(3, undersized, [UxKeyboardKey.Tab])));
            Should.Throw<InvalidOperationException>(() => UxAccessibilityAssertions.AssertTargetSize(target with { EquivalentControlBounds = undersized }));
        }
        UxAccessibilityAssertions.AssertTargetSize(new(3, new(20, 100, 24, 24), [UxKeyboardKey.Tab]));
        UxAccessibilityAssertions.AssertTargetSize(target with { EquivalentControlBounds = new(60, 100, 24, 24) });
        Should.Throw<InvalidOperationException>(() => UxAccessibilityAssertions.AssertTargetSize(target with { EquivalentControlBounds = new(60, 100, 12, 12) }));
        UxAccessibilityAssertions.AssertTargetSize(new(3, new(100, 100, 32, 32), [UxKeyboardKey.Tab]));
        foreach ((UxTargetSizeExceptionKind kind, UxTargetSizeRationale rationale) in new[] {
            (UxTargetSizeExceptionKind.Inline, UxTargetSizeRationale.InlineText),
            (UxTargetSizeExceptionKind.UserAgentControl, UxTargetSizeRationale.UserAgentOwned),
            (UxTargetSizeExceptionKind.Essential, UxTargetSizeRationale.EssentialPresentation) })
        {
            UxAccessibilityAssertions.AssertTargetSize(target with { Exception = kind, Rationale = rationale });
        }
        AccessibilityTargetEvidence spaced = target with { Exception = UxTargetSizeExceptionKind.Spacing,
            Rationale = UxTargetSizeRationale.SeparatedFromNeighbors, Neighbors = [new(100, 100, 18, 18)] };
        UxAccessibilityAssertions.AssertTargetSize(spaced);
        UxAccessibilityAssertions.AssertTargetSize(spaced with { Neighbors = [] });
        Should.Throw<InvalidOperationException>(() => UxAccessibilityAssertions.AssertTargetSize(spaced with { Neighbors = [new(30, 100, 18, 18)] }));
        foreach ((AccessibilityTargetEvidence incomplete, string check) in new[] {
            (target with { Rationale = UxTargetSizeRationale.InlineText }, "EquivalentException"),
            (target with { Exception = UxTargetSizeExceptionKind.Inline, Rationale = UxTargetSizeRationale.UserAgentOwned }, "InlineException"),
            (spaced with { Neighbors = null }, "SpacingException"),
            // Clears the 12 px circle, but the undersized neighbour's centre is 22 px away.
            (spaced with { Neighbors = [new(42, 100, 18, 18)] }, "SpacingException"),
            (new AccessibilityTargetEvidence(3, new(20, 100, 24, 24), [UxKeyboardKey.Tab], UxTargetSizeExceptionKind.Inline, UxTargetSizeRationale.InlineText), "UnneededException"),
            (target with { KeyboardPath = [] }, "KeyboardPath"),
            (target with { EquivalentControlNumber = 1 }, "EquivalentException"),
            (target with { TargetNumber = 0 }, "TargetNumber"),
            (target with { Bounds = new(20, 100, 0, 0), Exception = UxTargetSizeExceptionKind.Inline, Rationale = UxTargetSizeRationale.InlineText }, "Bounds") })
        {
            InvalidOperationException failure = Should.Throw<InvalidOperationException>(() => UxAccessibilityAssertions.AssertTargetSize(incomplete));
            failure.Message.ShouldContain(check);
            failure.Message.ShouldNotContain("incomplete");
        }
        TestAccessibilityEvidenceRecorder recorder = new(host.Options);
        foreach (AccessibilityEvidenceRow row in Enum.GetValues<AccessibilityEvidenceRow>())
        {
            UxTheme theme = row == AccessibilityEvidenceRow.AE07 ? UxTheme.ForcedColors : UxTheme.Light;
            AccessibilityEvidence record = recorder.Record(row, new(1, 2, theme), new()
            {
                Passed = true, ViewportWidth = 320, ZoomPercent = 400, HorizontalOverflow = 0,
                ContentLoss = false, Clipping = false, Overlap = false, ControlTruncation = false,
                OperationsAvailable = true, ReadingOrderLogical = true, SelectedTabVisible = true,
                ForcedColorCuesPreserved = true, NonEssentialAnimationsSuppressed = true, StateCuesPreserved = true,
                FocusChangesImmediate = true, ExpirySilent = true, KeyboardPassed = true, AxePassed = true, NoHoverOnlyAction = true,
                ViewportAndScrollports = [new(0, 0, 320, 640)], Occluders = [new(0, 0, 320, 64)],
                LineHeight = 1.5, ParagraphSpacing = 2, LetterSpacing = .12, WordSpacing = .16,
                ContrastRatio = 4.5, ForcedColors = row == AccessibilityEvidenceRow.AE07, ReducedMotion = true, FindingCount = 0,
                CriticalFindingCount = 0, Wcag22Aa = true, AnnouncementCount = 1,
                FocusTarget = new(20, 100, 18, 18), FocusIndicator = new(18, 98, 22, 22), Targets = [target],
            });
            record.Row.ShouldBe(row);
            record.CapturedAtUtc.ShouldBe(clock.GetUtcNow());
            record.Context.ShouldBe(new(1, 2, theme));
            using JsonDocument measured = JsonDocument.Parse(record.RedactedMeasurements);
            JsonElement data = measured.RootElement;
            data.GetProperty("ForcedColors").GetBoolean().ShouldBe(row == AccessibilityEvidenceRow.AE07);
            foreach (string flag in new[] { "Passed", "OperationsAvailable", "ReadingOrderLogical", "SelectedTabVisible", "ReducedMotion",
                "Wcag22Aa", "ForcedColorCuesPreserved", "NonEssentialAnimationsSuppressed", "StateCuesPreserved", "FocusChangesImmediate",
                "ExpirySilent", "KeyboardPassed", "AxePassed", "NoHoverOnlyAction" }) { data.GetProperty(flag).GetBoolean().ShouldBeTrue(); }
            foreach (string flag in new[] { "ContentLoss", "Clipping", "Overlap", "ControlTruncation" }) { data.GetProperty(flag).GetBoolean().ShouldBeFalse(); }
            foreach ((string key, double value) in new[] { ("ViewportWidth", 320d), ("ZoomPercent", 400d), ("HorizontalOverflow", 0d), ("LineHeight", 1.5),
                ("ParagraphSpacing", 2d), ("LetterSpacing", .12), ("WordSpacing", .16), ("ContrastRatio", 4.5), ("FindingCount", 0d),
                ("CriticalFindingCount", 0d), ("AnnouncementCount", 1d) }) { data.GetProperty(key).GetDouble().ShouldBe(value); }
            data.GetProperty("FocusTarget").Deserialize<UxRect>().ShouldBe(new(20, 100, 18, 18));
            data.GetProperty("FocusIndicator").Deserialize<UxRect>().ShouldBe(new(18, 98, 22, 22));
            data.GetProperty("ViewportAndScrollports").Deserialize<UxRect[]>().ShouldBe([new(0, 0, 320, 640)]);
            data.GetProperty("Occluders").Deserialize<UxRect[]>().ShouldBe([new(0, 0, 320, 64)]);
            JsonElement t = data.GetProperty("Targets")[0];
            t.GetProperty("TargetNumber").GetInt32().ShouldBe(1);
            t.GetProperty("Bounds").Deserialize<UxRect>().ShouldBe(target.Bounds);
            t.GetProperty("EquivalentControlNumber").GetInt32().ShouldBe(2);
            t.GetProperty("EquivalentControlBounds").Deserialize<UxRect>().ShouldBe(target.EquivalentControlBounds);
            t.GetProperty("KeyboardPath").EnumerateArray().Select(x => x.GetString()).ShouldBe(["Tab", "Enter"]);
            t.GetProperty("Exception").GetString().ShouldBe("Equivalent");
            t.GetProperty("Rationale").GetString().ShouldBe("SameFunctionConformingControl");
            record.RedactedMeasurements.ShouldContain("Equivalent");
            record.RedactedMeasurements.ShouldContain("SameFunctionConformingControl");
            record.RedactedMeasurements.ShouldContain("KeyboardPath");
            record.RedactedMeasurements.ShouldNotContain(host.Options.TestTenantId);
            record.RedactedMeasurements.ShouldNotContain(host.Options.TestUserId);
        }
        recorder.Evidence.Count.ShouldBe(9);
        AccessibilityEvidence failed = recorder.Record(AccessibilityEvidenceRow.AE09, new(1, 3, UxTheme.Dark), new()
        { Passed = false, FindingCount = 2, CriticalFindingCount = 1, Wcag22Aa = true, AnnouncementCount = 3,
            KeyboardPassed = false, AxePassed = false, NoHoverOnlyAction = false });
        failed.Context.ShouldBe(new(1, 3, UxTheme.Dark));
        using (JsonDocument failureData = JsonDocument.Parse(failed.RedactedMeasurements))
        {
            JsonElement data = failureData.RootElement;
            data.GetProperty("Passed").GetBoolean().ShouldBeFalse();
            data.GetProperty("FindingCount").GetInt32().ShouldBe(2);
            data.GetProperty("CriticalFindingCount").GetInt32().ShouldBe(1);
            data.GetProperty("AnnouncementCount").GetInt32().ShouldBe(3);
            data.GetProperty("Wcag22Aa").GetBoolean().ShouldBeTrue();
            foreach (string flag in new[] { "KeyboardPassed", "AxePassed", "NoHoverOnlyAction" }) { data.GetProperty(flag).GetBoolean().ShouldBeFalse(); }
        }
        recorder.Evidence.Count.ShouldBe(9);
        recorder.Reset();
        recorder.Evidence.ShouldBeEmpty();

        string redacted = RedactedEvidenceFormatter.Format(new
        {
            Jwt = "jwt.payload.signature", Token = "token-value", TenantPayload = new { Account = "private" },
            StackTrace = "at Backend.Run()", BackendMetadata = "backend-private", Email = "person@example.test",
            Phone = "0102030405", FullName = "Private Person", ShippingAddress = "1 Hidden Street", PersonName = "Pat Example",
        }, host.Options);
        foreach (string secret in new[] { "jwt.payload.signature", "token-value", "private", "Backend.Run", "backend-private", "person@example.test", "0102030405", "Private Person",
            "1 Hidden Street", "Pat Example" })
        {
            redacted.ShouldNotContain(secret);
        }
    }

    [Fact]
    public async Task UxAssertions_AuthorizationDenial_UsesFocusOnlyHeadingAndOriginFallback()
    {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        host.AuthorizationEvaluator.Deny("denied");
        IRenderedComponent<FcAuthorizedCommandRegion> cut = context.Render<FcAuthorizedCommandRegion>(p => p
            .Add(c => c.CommandType, typeof(TestCommand)).Add(c => c.PolicyName, "denied")
            .Add(c => c.Authorized, "allowed").Add(c => c.NotAuthorized, "<h1 tabindex='-1'>Permission required</h1><h2 tabindex='-1'>Command form</h2>"));
        await cut.InvokeAsync(() => { });
        IElement heading = cut.Find("h1");
        UxAnnouncement denial = UxAnnouncementCatalog.Expected("AM-26", "attempt", "denied", variant: "UnauthorizedCommandWarningTitle");
        UxAccessibilitySnapshot semantic = new("heading", heading.TextContent, true, heading.GetAttribute("aria-live"),
            heading.GetAttribute("aria-atomic"), heading.GetAttribute("tabindex"));
        UxAnnouncementAssertions.AssertChannel(denial, semantic, denial.Copy);
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(denial, semantic with { AriaLive = "polite" }, denial.Copy));
        InvalidOperationException atomic = Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(denial, semantic with { AriaAtomic = "true" }, denial.Copy));
        atomic.Message.ShouldContain("AriaAtomic");
        atomic.Message.ShouldNotContain("AriaLive");
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(denial, semantic with { Name = "Command form" }, "Command form"))
            .Message.ShouldContain("HeadingCopy");
        UxStateAssertions.AssertState("SS-38", new("AuthorizationDenied", true, semantic, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true }));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-38", new("AuthorizationDenied", true, semantic, Dispatched: true, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })));

        UxFocusTarget route = new(heading, "heading", heading.TextContent, "h1");
        IElement formHeading = cut.Find("h2");
        UxFocusTarget form = new(formHeading, "heading", formHeading.TextContent, "h2");
        foreach (string row in new[] { "FM-04", "FM-05", "FM-11" })
        {
            UxFocusTarget fallback = row == "FM-11" ? form : route;
            foreach (string location in new[] { "Shell navigation", "Page content" })
            {
                UxFocusTarget origin = UxFocusAssertions.CaptureOrigin(new(new object(), "button", location, "button", TestId: "contract-invoker"));
                UxFocusAssertions.AssertRestored(origin, origin, origin, fallback, row);
                foreach (UxFocusTarget unavailable in new[] { origin with { Connected = false }, origin with { Enabled = false }, origin with { Visible = false } })
                {
                    UxFocusAssertions.AssertRestored(origin, unavailable, fallback, fallback, row);
                    Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertRestored(origin, unavailable,
                        new(new object(), "document", "body", "body"), fallback, row));
                }
                UxFocusAssertions.AssertRestored(origin, null, fallback, fallback, row);
                UxFocusTarget button = new(new object(), "button", "Other", "button");
                Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertRestored(origin, null, button, button, row));
                if (row != "FM-11")
                {
                    Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertRestored(origin, null, form, form, row));
                }
                Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertRestored(origin, origin,
                    origin with { Handle = new object() }, fallback, row));
            }
        }
        Should.Throw<InvalidOperationException>(() => UxFocusAssertions.CaptureOrigin(route with { Enabled = false }));
        UxFocusAssertions.AssertUnobscured(new(20, 100, 120, 24), new(18, 98, 124, 28),
            [new(0, 0, 320, 640), new(0, 80, 320, 400)], [new(0, 0, 320, 64), new(0, 560, 320, 80)]);
        Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertUnobscured(new(20, 100, 120, 24), new(18, 98, 124, 28),
            [new(0, 0, 320, 640)], [new(0, 96, 320, 10)]));
        Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertUnobscured(new(20, 100, 120, 24), new(18, 98, 124, 28),
            [new(0, 100, 320, 400)], []));
        Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertUnobscured(new(double.NaN, 0, 10, 10), new(0, 0, 10, 10), [new(0, 0, 320, 640)], []));
        // A hidden occluder measures as a zero-size rectangle and paints nothing; negative sizes remain invalid geometry.
        UxFocusAssertions.AssertUnobscured(new(20, 100, 120, 24), new(18, 98, 124, 28), [new(0, 0, 320, 640)], [new(30, 105, 0, 0), new(30, 105, 40, 0)]);
        // Isolate zero width with positive height directly over the target.
        UxFocusAssertions.AssertUnobscured(new(20, 100, 120, 24), new(18, 98, 124, 28), [new(0, 0, 320, 640)], [new(30, 105, 0, 10)]);
        Should.Throw<InvalidOperationException>(() => UxFocusAssertions.AssertUnobscured(new(20, 100, 120, 24), new(18, 98, 124, 28),
            [new(0, 0, 320, 640)], [new(250, 300, -4, 10)]));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public async Task UxAssertions_ValidationFailure_LinksCompleteSummaryAndPreservesInput(bool mapped, int errorMode)
    {
        using BunitContext context = new();
        TestTimeProvider clock = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context, o => o.TimeProvider = clock);
        BuilderModel model = new() { Name = "useful unsaved input", Code = "useful code" };
        EditContext edit = new(model);
        ValidationMessageStore errors = new(edit);
        FcValidationFieldDescriptor[] descriptors = [new("Name", "Name", "name-input"), new("Code", "Code", "code-input")];
        IRenderedComponent<ValidationConsumer> cut = context.Render<ValidationConsumer>(p => p
            .Add(c => c.FormContext, edit).Add(c => c.Descriptors, descriptors));
        UxFieldSnapshot[] firstRender = MeasureValidationFields(cut, descriptors);
        UxFieldIdentity[] prior = firstRender.Select(f => new UxFieldIdentity(f.FieldKey, f.ControlId, f.ErrorId)).ToArray();
        prior.ShouldAllBe(f => f.ErrorId.Length == 0);
        Dictionary<string, string?> before = MeasureInputValues(cut, descriptors);
        ILifecycleStateService lifecycle = context.Services.GetRequiredService<ILifecycleStateService>();
        ISurfaceAnnouncementCoordinator coordinator = context.Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> liveObservations = [];
        using IDisposable subscription = coordinator.Subscribe("lifecycle:mapped-operation", liveObservations.Add);
        Dictionary<string, IReadOnlyList<string>> fieldErrors = new(StringComparer.Ordinal);
        if (errorMode != 2) { fieldErrors["Name"] = errorMode == 1 ? ["Review this value.", "The name is unavailable."] : ["Review this value."]; }
        if (errorMode == 1) { fieldErrors["Code"] = ["Review this code."]; }
        string[] formErrors = errorMode == 0 ? [] : ["Review the form-level decision."];
        if (mapped)
        {
            ProblemDetailsPayload problem = new("Rejected", "Review this value.", 409, null, fieldErrors, formErrors);
            CommandRejectedException authoritative = CommandRejectedException.FromProblem("Rejected", "Review and retry.", problem);
            ServerValidationApplicationResult applied = ServerValidationApplicator.ApplyRejection(errors, authoritative,
                new ReflectionCommandValidationFieldAllowlist<BuilderModel>(), model);
            applied.HasMappedFieldErrors.ShouldBeTrue();
            applied.MappedFieldCount.ShouldBe(errorMode == 1 ? 2 : 1);
            cut.Render(p => p.Add(c => c.FormErrors, applied.UnmappedMessages));
        }
        else
        {
            foreach ((string key, IReadOnlyList<string> messages) in fieldErrors)
            { foreach (string message in messages) { errors.Add(edit.Field(key), message); } }
            foreach (string message in formErrors) { errors.Add(new FieldIdentifier(model, string.Empty), message); }
        }
        IRenderedComponent<FcLifecycleWrapper> recovery = context.Render<FcLifecycleWrapper>(p => p
            .Add(c => c.CorrelationId, "mapped-operation").Add(c => c.MappedRejection, mapped));
        if (mapped)
        {
            await recovery.InvokeAsync(() =>
            {
                lifecycle.Transition("mapped-operation", CommandLifecycleState.Submitting, "message");
                lifecycle.Transition("mapped-operation", CommandLifecycleState.Rejected, "message");
            });
            lifecycle.GetState("mapped-operation").ShouldBe(CommandLifecycleState.Rejected);
            recovery.FindAll("[data-testid='fc-rejected']").ShouldBeEmpty();
            _ = recovery.Find("[data-testid='fc-rejected-mapped']");
        }
        await cut.InvokeAsync(edit.NotifyValidationStateChanged);
        await cut.InvokeAsync(() => cut.Instance.ShowAsync(mapped ? FcValidationSummaryKind.MappedServerRejection : FcValidationSummaryKind.ClientValidation));
        IElement summary = cut.Find("[role='group'][tabindex='-1']");
        string name = cut.Find("#" + summary.GetAttribute("aria-labelledby")).TextContent;
        string description = cut.Find("#" + summary.GetAttribute("aria-describedby")).TextContent;
        UxValidationSummarySnapshot measured = new(new(summary.GetAttribute("role")!, name, true,
            summary.GetAttribute("aria-live"), summary.GetAttribute("aria-atomic"), summary.GetAttribute("tabindex")), description, summary.QuerySelectorAll("li").Length);
        UxFieldSnapshot[] fields = MeasureValidationFields(cut, descriptors);
        string[] links = summary.QuerySelectorAll("a[href]").Select(a => Uri.UnescapeDataString(a.GetAttribute("href")!.Split('#')[^1])).ToArray();
        UxFieldDeclaration[] declared = [new("Name", "Name", "command"), new("Code", "Code", "command")];
        UxFieldGroupDeclaration[] groups = [new("command", "Command fields", "Command fields", "Editable command details", ["Name", "Code"])];
        UxValidationAssertions.AssertLinkedSummary(measured, name, fields, links, declared, groups, prior, mapped);
        measured.ErrorCount.ShouldBe(errorMode == 1 ? 4 : 1);
        links.Length.ShouldBe(errorMode == 1 ? 3 : errorMode == 2 ? 0 : 1);
        UxValidationAssertions.AssertInputPreserved(before, MeasureInputValues(cut, descriptors));
        Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(measured with { Text = "Wrong" }, name, fields, links, declared, groups, prior, mapped));
        if (errorMode != 2)
        {
            Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(measured, name,
                [fields[0] with { DescribedBy = "wrong-error" }, fields[1]], links, declared, groups, prior, mapped));
            Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(measured, name, fields, [], declared, groups, prior, mapped));
        }
        if (errorMode == 1)
        {
            Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(measured, name, fields,
                links.Reverse().ToArray(), declared, groups, prior, mapped));
        }
        if (mapped)
        {
            await recovery.InvokeAsync(() => clock.Advance(UxTiming.CoalescingWindow));
            liveObservations.ShouldBeEmpty();
            UxAnnouncementAssertions.AssertSequence([new("AM-19", description, UxAnnouncementChannel.FocusedSummary, "mapped-operation", "Rejected")],
                UxAnnouncementCatalog.Expected("AM-19", "mapped-operation", "Rejected", [measured.ErrorCount], variant: measured.ErrorCount == 1 ? "single" : null));
        }
        // A corrected field removes its error target while the form-level error keeps the summary available.
        UxFieldIdentity[] invalidRender = fields.Select(f => new UxFieldIdentity(f.FieldKey, f.ControlId, f.ErrorId)).ToArray();
        await cut.InvokeAsync(() =>
        {
            errors.Clear();
            errors.Add(new FieldIdentifier(model, string.Empty), "Review the remaining form decision.");
            edit.NotifyValidationStateChanged();
        });
        cut.Render(p => p.Add(c => c.FormErrors, Array.Empty<string>()));
        await cut.InvokeAsync(() => cut.Instance.ShowAsync(FcValidationSummaryKind.ClientValidation));
        summary = cut.Find("[role='group'][tabindex='-1']");
        string correctedName = cut.Find("#" + summary.GetAttribute("aria-labelledby")).TextContent;
        UxValidationSummarySnapshot corrected = new(new("group", correctedName, true, TabIndex: "-1"),
            cut.Find("#" + summary.GetAttribute("aria-describedby")).TextContent, summary.QuerySelectorAll("li").Length);
        UxValidationAssertions.AssertLinkedSummary(corrected, correctedName, MeasureValidationFields(cut, descriptors), [], declared, groups, invalidRender);
        InvalidOperationException mismatch = Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertInputPreserved(before, new Dictionary<string, string?> { ["Name"] = "lost" }));
        mismatch.Message.ShouldNotContain(model.Name);
    }

    private static UxFieldSnapshot[] MeasureValidationFields(IRenderedComponent<ValidationConsumer> cut, IReadOnlyList<FcValidationFieldDescriptor> descriptors)
        => descriptors.Select(field =>
        {
            IElement input = cut.Find("#" + field.InputId);
            IElement group = input.Closest("fieldset")!;
            string refs = input.GetAttribute("aria-describedby") ?? "";
            string errorId = refs.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(id => cut.FindAll("#" + id).Count > 0) ?? "";
            string groupLabel = cut.Find("#" + group.GetAttribute("aria-labelledby")).TextContent;
            return new UxFieldSnapshot(field.FieldName, input.Id!, cut.Find("label[for='" + input.Id + "']").TextContent,
                input.GetAttribute("aria-label")!, group.Id!, group.QuerySelector("legend")!.TextContent, groupLabel,
                cut.Find("#" + group.GetAttribute("aria-describedby")).TextContent, "group", input.GetAttribute("aria-invalid") == "true", errorId, refs);
        }).ToArray();

    private static Dictionary<string, string?> MeasureInputValues(IRenderedComponent<ValidationConsumer> cut, IReadOnlyList<FcValidationFieldDescriptor> descriptors)
        => descriptors.ToDictionary(f => f.FieldName, f => cut.Find("#" + f.InputId).GetAttribute("value"), StringComparer.Ordinal);

    [Fact]
    public async Task UxAssertions_PollingExhaustion_UsesExactBoundaryAndClosesWithoutSuccess()
    {
        using BunitContext context = new();
        TestTimeProvider clock = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context, o => o.TimeProvider = clock);
        host.CommandService.StallAtSyncing();
        CommandResult dispatched = await host.CommandService.DispatchAsync(new TestCommand(), Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        ILifecycleStateService lifecycle = context.Services.GetRequiredService<ILifecycleStateService>();
        string operation = dispatched.CorrelationId.ShouldNotBeNull();
        IRenderedComponent<FcLifecycleWrapper> cut = context.Render<FcLifecycleWrapper>(p => p.Add(c => c.CorrelationId, operation));
        await cut.InvokeAsync(() =>
        {
            lifecycle.Transition(operation, CommandLifecycleState.Submitting, dispatched.MessageId);
            lifecycle.Transition(operation, CommandLifecycleState.Acknowledged, dispatched.MessageId);
            UxTimingAssertions.AssertBoundary(clock, UxTiming.PollingExhaustion,
                () => lifecycle.GetState(operation) == CommandLifecycleState.DegradedExhausted);
        });
        UxStateAssertions.AssertState("SS-26", new("DegradedExhausted", true, new("group", "Command status", false), lifecycle.GetState(operation), Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, PollingActive = false }));
        ISurfaceAnnouncementCoordinator announcements = context.Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        announcements.Current("lifecycle:" + operation).ShouldBe(UxAnnouncementCatalog.Expected("AM-24", operation, "exhausted").Copy);
        await cut.InvokeAsync(() => clock.Advance(TimeSpan.FromMinutes(5)));
        lifecycle.GetState(operation).ShouldBe(CommandLifecycleState.DegradedExhausted);
    }

    [Fact]
    public void UxAssertions_BlockedAttempt_RejectsQueuedDispatchAndDuplicateFeedback()
    {
        using BunitContext context = new();
        TestTimeProvider clock = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context, o => o.TimeProvider = clock);
        PendingCommandStateService pending = new(Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            context.Services.GetRequiredService<ILifecycleStateService>(), host.UserContext, clock);
        using var pendingLifetime = pending;
        CommandExecutionAdmissionGate gate = new(pending, clock);
        using CommandExecutionAdmission active = gate.TryAcquire(new("ActiveCommand"));
        active.IsAdmitted.ShouldBeTrue();
        using CommandExecutionAdmission denied = gate.TryAcquire(new("SecondCommand"));
        denied.IsAdmitted.ShouldBeFalse();
        denied.DenialReason.ShouldBe(CommandExecutionAdmissionDenialReason.AdmissionAlreadyInProgress);
        host.CommandService.Evidence.ShouldBeEmpty();
        pending.Snapshot().ShouldBeEmpty();
        UxAnnouncement blocked = UxAnnouncementCatalog.Expected("AM-20", "blocked-attempt", "blocked");
        UxSurfaceSnapshot state = new("BlockedSubmit", true, new("group", "Command not run", false), Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, OriginalOperationVisible = true });
        UxFocusTarget submit = new(new object(), "button", "Submit", "button");
        UxStateAssertions.AssertBlockedSubmit(state, submit, submit, [blocked]);
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertBlockedSubmit(state, submit with { Handle = null! }, submit with { Handle = null! }, [blocked]));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertBlockedSubmit(state with { Queued = true }, submit, submit, [blocked]));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertBlockedSubmit(state, submit, submit, [blocked, blocked]));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertBlockedSubmit(state, submit, submit with { Enabled = false }, [blocked]));
    }

    [Fact]
    public void UxAnnouncements_CoalescedFailureAndStaleResult_EnforceExactSequenceAndRedactedDrift()
    {
        TestTimeProvider clock = new();
        using SurfaceAnnouncementCoordinator coordinator = new(clock);
        UxAnnouncement acknowledged = UxAnnouncementCatalog.Expected("AM-11", "operation", "Acknowledged");
        UxAnnouncement syncing = UxAnnouncementCatalog.Expected("AM-12", "operation", "Syncing");
        UxAnnouncement rejected = UxAnnouncementCatalog.Expected("AM-14", "operation", "Rejected");
        List<UxAnnouncement> spoken = [];
        using IDisposable subscription = coordinator.Subscribe("command", text => spoken.Add(
            (text == rejected.Copy ? rejected : text == syncing.Copy ? syncing : acknowledged) with { Copy = text }));
        coordinator.Announce("command", "lifecycle:operation", "Acknowledged", acknowledged.Copy);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        coordinator.Announce("command", "lifecycle:operation", "Syncing", syncing.Copy);
        UxAnnouncementAssertions.AssertCoalescingBoundary(clock, () => spoken, syncing);
        coordinator.Announce("command", "lifecycle:operation", "late", acknowledged.Copy);
        coordinator.Announce("command", "lifecycle:operation", "Rejected", rejected.Copy, terminal: true);
        UxAnnouncementAssertions.AssertImmediateCancellation(clock, () => spoken, rejected, syncing);
        coordinator.Announce("command", "lifecycle:operation", "stale-result", syncing.Copy);
        clock.Advance(UxTiming.CoalescingWindow);
        UxAnnouncementAssertions.AssertSequence(spoken, syncing, rejected);
        foreach (UxAnnouncement[] drift in new[] {
            new[] { rejected, syncing }, new[] { syncing }, new[] { syncing, rejected, rejected },
            new[] { syncing with { Copy = "token jwt tenant payload stack trace person@example.test" }, rejected },
            new[] { syncing with { OperationId = "other-operation" }, rejected } })
        {
            InvalidOperationException failure = Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertSequence(drift, syncing, rejected));
            failure.Message.ShouldContain("AM-");
            failure.Message.ShouldContain("PoliteStatus");
            failure.Message.ShouldNotContain("person@example.test");
            failure.Message.ShouldNotContain("jwt");
        }
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertDistinctChannels([rejected, rejected with { Channel = UxAnnouncementChannel.FocusedSummary }]));
        UxAnnouncement confirmed = UxAnnouncementCatalog.Expected("AM-13", "operation", "Confirmed");
        UxAnnouncementAssertions.AssertSequence([acknowledged, confirmed], acknowledged, confirmed);
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertDistinctChannels([confirmed, confirmed]));
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertDistinctChannels([confirmed, rejected]));
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertDistinctChannels([confirmed,
            UxAnnouncementCatalog.Expected("AM-13", "operation", "IdempotentConfirmed")]));
        foreach ((string first, string second) in new[] { ("AM-24", "AM-13"), ("AM-15", "AM-14"), ("AM-16", "AM-13"), ("AM-19", "AM-14") })
        {
            UxAnnouncement concluded = UxAnnouncementCatalog.Expected(first, "operation", first, first == "AM-19" ? [2] : null);
            UxAnnouncementAssertions.AssertDistinctChannels([concluded]);
            InvalidOperationException terminal = Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertDistinctChannels([concluded,
                UxAnnouncementCatalog.Expected(second, "operation", second)]));
            terminal.Message.ShouldContain("second terminal outcome");
            terminal.Message.ShouldNotContain("wrong channel");
        }
        InvalidOperationException named = Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertSequence(
            [syncing with { OperationId = "other-operation" }, rejected], syncing, rejected));
        named.Message.ShouldContain("differing OperationId");
        named.Message.ShouldNotContain("other-operation");
    }

    [Theory]
    [InlineData("AM-19")]
    [InlineData("AM-23")]
    public void UxAnnouncements_DecisiveFailure_CancelsPendingIntermediate(string row)
    {
        TestTimeProvider clock = new();
        using SurfaceAnnouncementCoordinator coordinator = new(clock);
        UxAnnouncement pending = UxAnnouncementCatalog.Expected("AM-12", "attempt", "Syncing");
        UxAnnouncement decisive = UxAnnouncementCatalog.Expected(row, "attempt", "failure", row == "AM-19" ? [2] : ["Orders", "Home"]);
        List<UxAnnouncement> spoken = [];
        using IDisposable subscription = coordinator.Subscribe("surface", text => spoken.Add(decisive with { Copy = text }));
        coordinator.Announce("surface", "attempt", "pending", pending.Copy);
        if (row == "AM-19") { coordinator.Cancel("surface", "attempt"); }
        // A focused summary is observed through focus; navigation failure uses the immediate status path.
        if (row == "AM-19") { spoken.Add(decisive); }
        else { coordinator.Announce("surface", "attempt", "failure", decisive.Copy, immediate: true); }
        UxAnnouncementAssertions.AssertImmediateCancellation(clock, () => spoken, decisive);
    }

    [Fact]
    public void UxAnnouncements_PaletteFailure_RejectsWrongChannelAndDuplicateSpeechOwners()
    {
        UxAnnouncement empty = UxAnnouncementCatalog.Expected("AM-28", "palette-session", "zero-result");
        UxAccessibilitySnapshot status = new("status", empty.Copy, false, "polite", "true");
        UxAnnouncementAssertions.AssertChannel(empty, status, empty.Copy);
        foreach (string malformed in new[] { "AM-+1", "AM- 1", "AM-١١" })
        {
            UxAnnouncement bad = empty with { RowId = malformed, Channel = UxAnnouncementChannel.PoliteStatus };
            Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertDistinctChannels([bad]));
            InvalidOperationException safe = Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertSequence([bad]));
            safe.Message.ShouldContain("<unknown AM row>");
            safe.Message.ShouldNotContain(malformed);
        }
        UxAnnouncementAssertions.AssertChannel(empty, status with { Name = "" }, "");
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty, status, empty.Copy, speechOwnerCount: 2));
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty, status, empty.Copy, inheritedResultCountSpeechCount: 1));
        // role=status is implicitly polite and atomic; only an explicit override removes either property.
        UxAnnouncementAssertions.AssertChannel(empty, status with { AriaLive = null, AriaAtomic = null }, empty.Copy);
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty, status with { AriaAtomic = "false" }, empty.Copy))
            .Message.ShouldContain("AriaAtomic");
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty, status with { AriaLive = "off" }, empty.Copy))
            .Message.ShouldContain("AriaLive");
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty, status with { Role = "group", AriaLive = null }, empty.Copy));
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty with { Channel = UxAnnouncementChannel.PoliteStatus }, status, empty.Copy));
        Should.Throw<InvalidOperationException>(() => UxAnnouncementAssertions.AssertChannel(empty, status with { Name = "different" }, empty.Copy));
    }

    [Fact]
    public void UxTiming_FailureBudgets_RejectEarlyAndLateEmitsAndDriveRecurringTimers()
    {
        foreach (TimeSpan budget in new[] { UxTiming.SlowQuery, UxTiming.PollingExhaustion, UxTiming.Degraded,
            UxTiming.RecoveryNotice, UxTiming.StatusPolling, UxTiming.ProjectionPolling, UxTiming.FreshRowLifetime,
            UxTiming.CoalescingWindow, UxTiming.PostAcceptanceRetry, UxTiming.ProjectionRetryCap, UxTiming.ConnectionRestart, UxTiming.Abandonment })
        {
            TestTimeProvider clock = new();
            bool fired = false;
            using ITimer timer = clock.CreateTimer(_ => fired = true, null, budget, Timeout.InfiniteTimeSpan);
            UxTimingAssertions.AssertBoundary(clock, budget, () => fired);
            foreach (int offset in new[] { -1, 1 })
            {
                TestTimeProvider driftClock = new();
                bool driftFired = false;
                using ITimer drift = driftClock.CreateTimer(_ => driftFired = true, null, budget + TimeSpan.FromMilliseconds(offset), Timeout.InfiniteTimeSpan);
                Should.Throw<InvalidOperationException>(() => UxTimingAssertions.AssertBoundary(driftClock, budget, () => driftFired));
            }
        }
        TestTimeProvider offsetClock = new(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.FromHours(2)));
        offsetClock.GetUtcNow().Offset.ShouldBe(TimeSpan.Zero);
        offsetClock.GetUtcNow().Hour.ShouldBe(8);
        offsetClock.GetTimestamp().ShouldBe(0);
        TestTimeProvider recurring = new();
        List<long> ticks = [];
        using ITimer repeated = recurring.CreateTimer(_ => ticks.Add(recurring.GetTimestamp()), null, UxTiming.StatusPolling, UxTiming.StatusPolling);
        recurring.Advance(TimeSpan.FromSeconds(3));
        ticks.ShouldBe([TimeSpan.FromSeconds(1).Ticks, TimeSpan.FromSeconds(2).Ticks, TimeSpan.FromSeconds(3).Ticks]);
        repeated.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan).ShouldBeTrue();
        recurring.Advance(TimeSpan.FromSeconds(3));
        ticks.Count.ShouldBe(3);
        repeated.Dispose();
        repeated.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() => recurring.Advance(TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void UxTiming_CanonicalDefaults_MatchShellRuntimeWithoutChangingConstants()
    {
        FcShellOptions options = new();
        UxTiming.PostAcceptanceRetry.ShouldBe(TimeSpan.FromMilliseconds(options.CommandDispatchRetryDelayMs));
        UxTiming.SlowQuery.ShouldBe(TimeSpan.FromMilliseconds(options.SlowQueryThresholdMs));
        UxTiming.RecoveryNotice.ShouldBe(TimeSpan.FromMilliseconds(options.ProjectionReconnectedNoticeDurationMs));
        UxTiming.StatusPolling.ShouldBe(TimeSpan.FromMilliseconds(options.PendingCommandPollingIntervalMs));
        UxTiming.PollingExhaustion.ShouldBe(TimeSpan.FromMilliseconds(options.MaxPendingCommandPollingDurationMs));
        UxTiming.ProjectionPolling.ShouldBe(TimeSpan.FromSeconds(options.ProjectionFallbackPollingIntervalSeconds));
        UxTiming.Abandonment.ShouldBe(TimeSpan.FromSeconds(options.FormAbandonmentThresholdSeconds));
        Assembly shell = typeof(FcLifecycleWrapper).Assembly;
        object Read(string type, string field) => shell.GetType(type, throwOnError: true)!
            .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        UxTiming.CoalescingWindow.ShouldBe((TimeSpan)Read("Hexalith.FrontComposer.Shell.Services.Announcements.SurfaceAnnouncementCoordinator", "Window"));
        UxTiming.FreshRowLifetime.ShouldBe((TimeSpan)Read("Hexalith.FrontComposer.Shell.Services.Announcements.FreshRowAnnouncementLedger", "Lifetime"));
        UxTiming.ProjectionRetryCap.ShouldBe(TimeSpan.FromMilliseconds((int)Read("Hexalith.FrontComposer.Shell.Infrastructure.EventStore.ProjectionHubRetryPolicy", "MaxDelayMilliseconds")));
        UxTiming.ConnectionRestart.ShouldBe((TimeSpan)Read("Hexalith.FrontComposer.Shell.Infrastructure.EventStore.ProjectionSubscriptionService", "ClosedRestartTimeout"));
        UxTiming.Degraded.ShouldBe(TimeSpan.FromMilliseconds((int)Read("Hexalith.FrontComposer.Shell.Components.Lifecycle.FcLifecycleWrapper", "DegradedAfterAcceptanceMs")));
    }

    [Fact]
    public void UxCatalog_AllRows_ResolveCanonicalLocalizedCopyAndDistinctLifecycleIdentity()
    {
        for (int n = 1; n <= 31; n++)
        {
            string row = "AM-" + n.ToString("00", CultureInfo.InvariantCulture);
            object[] args = n switch { 1 or 2 or 3 or 21 or 27 or 31 => ["Orders"], 9 or 18 or 19 => [2], 23 => ["Orders", "Home"], _ => [] };
            UxAnnouncement expected = UxAnnouncementCatalog.Expected(row, "failed-operation", "state", args,
                CultureInfo.GetCultureInfo("fr-FR"), n == 26 ? "RouteForbiddenHeading" : null);
            expected.Copy.ShouldNotBeNullOrWhiteSpace();
            UxAnnouncementAssertions.AssertDistinctChannels([expected]);
        }
        UxAnnouncement confirmed = UxAnnouncementCatalog.Expected("AM-13", "operation", "Confirmed");
        UxAnnouncement idempotent = UxAnnouncementCatalog.Expected("AM-13", "operation", "IdempotentConfirmed");
        confirmed.Copy.ShouldBe(idempotent.Copy);
        confirmed.StateIdentity.ShouldNotBe(idempotent.StateIdentity);
        foreach ((string row, string? variant) in new (string, string?)[] { ("AM-01", "offline"), ("AM-30", "single"), ("AM-18", "offline"), ("AM-26", null), ("AM-26", "Am01Loading") })
        {
            Should.Throw<ArgumentException>(() => UxAnnouncementCatalog.Expected(row, "operation", "state", ["Orders"], variant: variant));
        }
    }

    [Fact]
    public async Task UxClock_ConcurrentChangeDispose_UsesCreationContextOutsideClockLock()
    {
        TestTimeProvider clock = new();
        AsyncLocal<string?> ambient = new() { Value = "creation" };
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        string? observed = null;
        using ITimer timer = clock.CreateTimer(_ =>
        {
            observed = ambient.Value;
            entered.SetResult();
            release.Task.GetAwaiter().GetResult();
        }, null, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
        ambient.Value = "advancement";
        Task advance = Task.Run(() => clock.Advance(TimeSpan.FromMilliseconds(1)), Xunit.TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        // This must complete while the callback is active; otherwise callback/owner lock inversion is possible.
        await Task.Run(() =>
        {
            // A second advancement while a callback is active is rejected without moving time.
            Should.Throw<InvalidOperationException>(() => clock.Advance(TimeSpan.FromMilliseconds(1)));
            clock.GetTimestamp().ShouldBe(TimeSpan.FromMilliseconds(1).Ticks);
            timer.Change(TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan).ShouldBeTrue();
            timer.Dispose();
            timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan).ShouldBeFalse();
        }, Xunit.TestContext.Current.CancellationToken);
        release.SetResult();
        await advance.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        observed.ShouldBe("creation");
        ambient.Value.ShouldBe("advancement");
        clock.GetTimestamp().ShouldBe(TimeSpan.FromMilliseconds(1).Ticks);
        List<int> order = [];
        using ITimer first = clock.CreateTimer(_ => order.Add(1), null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        using ITimer second = clock.CreateTimer(_ => order.Add(2), null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        clock.Advance(TimeSpan.Zero);
        order.ShouldBe([1, 2]);
        // Nested advancement from a callback is rejected, so the outer advance cannot move time backwards.
        InvalidOperationException? nested = null;
        using ITimer reentrant = clock.CreateTimer(_ => nested = Should.Throw<InvalidOperationException>(() => clock.Advance(TimeSpan.FromMilliseconds(5))),
            null, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
        clock.Advance(TimeSpan.FromMilliseconds(2));
        nested.ShouldNotBeNull();
        clock.GetTimestamp().ShouldBe(TimeSpan.FromMilliseconds(3).Ticks);
    }

    [Fact]
    public void UxValidation_DeclaredGroupsAndStableTargets_RejectMeasuredRelationshipDrift()
    {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        UxFieldSnapshot valid = new("code", "code-input", "Code", "Code", "identity", "Identity", "Identity", "Identity details", "group", false, "", "");
        UxFieldSnapshot invalid = new("name", "name-input", "Name", "Name (required)", "identity", "Identity", "Identity", "Identity details", "group", true, "name-error", "help name-error");
        UxFieldDeclaration[] declared = [new("code", "Code", "identity"), new("name", "Name", "identity")];
        UxFieldGroupDeclaration[] groups = [new("identity", "Identity", "Identity", "Identity details", ["code", "name"])];
        UxFieldIdentity[] prior = [new("code", "code-input", ""), new("name", "name-input", "name-error")];
        UxValidationSummarySnapshot summary = new(new("group", "Validation failed", true, TabIndex: "-1"),
            "Correct the errors before submitting. One error.", 1);
        void Check(UxFieldSnapshot field, UxValidationSummarySnapshot? measured = null,
            UxFieldGroupDeclaration[]? expectedGroups = null, string[]? links = null)
            => UxValidationAssertions.AssertLinkedSummary(measured ?? summary, "Validation failed", [valid, field],
                links ?? ["name-input"], declared, expectedGroups ?? groups, prior);
        foreach (char separator in new[] { ' ', '\t', '\n', '\r', '\f' })
        {
            Check(invalid with { DescribedBy = "help" + separator + "name-error" });
        }
        Check(invalid);
        foreach (string visibleLabel in new[] { "First\nName", "First\t  Name" })
        {
            UxFieldDeclaration[] named = [declared[0], new("name", visibleLabel, "identity")];
            foreach (string accessibleName in new[] { "First Name (required)", " First\n\tName  (required) " })
            {
                UxValidationAssertions.AssertLinkedSummary(summary, "Validation failed", [valid, invalid with
                    { VisibleLabel = visibleLabel, AccessibleName = accessibleName }], ["name-input"], named, groups, prior);
            }
            foreach (string missingWords in new[] { "First (required)", "Name (required)", "First Last" })
            {
                InvalidOperationException label = Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(summary,
                    "Validation failed", [valid, invalid with { VisibleLabel = visibleLabel, AccessibleName = missingWords }],
                    ["name-input"], named, groups, prior));
                label.Message.ShouldContain("Field[2].Label");
                label.Message.ShouldNotContain("First");
                label.Message.ShouldNotContain("Name");
            }
            // Whitespace normalization does not relax the declared visible-label identity.
            Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(summary, "Validation failed",
                [valid, invalid with { VisibleLabel = "First Name", AccessibleName = "First Name" }], ["name-input"], named, groups, prior))
                .Message.ShouldContain("Field[2].Label");
        }
        foreach (string collision in new[] { "name-input", "code-input" })
        {
            UxFieldSnapshot collided = invalid with { ErrorId = collision, DescribedBy = collision };
            UxFieldIdentity[] absentPrior = [new("code", "code-input", ""), new("name", "name-input", "")];
            Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(summary, "Validation failed",
                [valid, collided], ["name-input"], declared, groups, absentPrior));
        }
        foreach (UxFieldSnapshot drift in new[] {
            invalid with { ControlId = "regenerated-input", ErrorId = "regenerated-error", DescribedBy = "regenerated-error" },
            invalid with { ErrorId = "regenerated-error", DescribedBy = "regenerated-error" },
            invalid with { VisibleLabel = "Wrong" }, invalid with { AccessibleName = "Missing visible label" },
            invalid with { GroupId = "other-group" }, invalid with { GroupName = "Wrong" },
            invalid with { GroupDescription = "Wrong" }, invalid with { GroupRole = "button" },
            invalid with { GroupVisibleLabel = "Wrong" }, invalid with { DescribedBy = "other-error" } })
        {
            Should.Throw<InvalidOperationException>(() => Check(drift));
        }
        Should.Throw<InvalidOperationException>(() => Check(invalid, summary with { Text = "Incorrect copy" }));
        Should.Throw<InvalidOperationException>(() => Check(invalid, summary with { ErrorCount = 2 }));
        Should.Throw<InvalidOperationException>(() => Check(invalid, expectedGroups: [groups[0] with { FieldKeys = ["name", "code"] }]));
        Should.Throw<InvalidOperationException>(() => Check(invalid, links: ["code-input", "name-input"]));
        Should.Throw<InvalidOperationException>(() => Check(invalid, links: []));
        // A corrected (valid) field must not keep a stale error target; diagnostics name the position, not the field.
        InvalidOperationException stale = Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(summary, "Validation failed",
            [valid with { ErrorId = "code-error", DescribedBy = "code-error" }, invalid], ["name-input"], declared, groups, prior));
        stale.Message.ShouldContain("Field[1].ErrorLink");
        stale.Message.ShouldNotContain("code");
        // Non-structural aggregate failures must not hide either field's independent failures.
        foreach (string aggregate in new[] { "Summary", "Links", "ErrorTargets" })
        {
            UxFieldSnapshot first = valid with { VisibleLabel = "private-label" };
            if (aggregate == "ErrorTargets") { first = first with { Invalid = true, ErrorId = invalid.ErrorId }; }
            InvalidOperationException all = Should.Throw<InvalidOperationException>(() => UxValidationAssertions.AssertLinkedSummary(
                aggregate == "Summary" ? summary with { Text = "private-summary" } : summary, "Validation failed",
                [first, invalid with { DescribedBy = "private-error" }], aggregate == "Links" ? [] : ["name-input"], declared, groups, prior));
            foreach (string check in new[] { aggregate, "Field[1].Label", "Field[2].ErrorLink" }) { all.Message.ShouldContain(check); }
            foreach (string value in new[] { "private-label", "private-summary", "private-error", "name-input" }) { all.Message.ShouldNotContain(value); }
        }
    }

    [Fact]
    public void UxEvidence_SameInstantThemesAndFailedOutcomes_RetainSafeAttributionAndFacts()
    {
        TestTimeProvider clock = new();
        TestAccessibilityEvidenceRecorder recorder = new(new() { TimeProvider = clock, MaxDiagnosticPayloadCharacters = 8192 });
        AccessibilityEvidence light = recorder.Record(AccessibilityEvidenceRow.AE06, new(2, 3, UxTheme.Light), new() { Passed = true, ContrastRatio = 4.5 });
        AccessibilityEvidence dark = recorder.Record(AccessibilityEvidenceRow.AE06, new(2, 3, UxTheme.Dark), new() { Passed = false, ContrastRatio = 2.1 });
        light.CapturedAtUtc.ShouldBe(dark.CapturedAtUtc);
        light.Context.ShouldNotBe(dark.Context);
        foreach (UxTheme theme in new[] { UxTheme.Light, UxTheme.Dark })
        {
            _ = recorder.Record(AccessibilityEvidenceRow.AE06, new(2, 3, theme), new() { Passed = true, ContrastRatio = 3 });
            foreach (double ratio in new[] { 1.2, 2.999 })
            {
                ArgumentException contrast = Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06,
                    new(2, 3, theme), new() { Passed = true, ContrastRatio = ratio }));
                contrast.Message.ShouldContain("ContrastRatio");
                contrast.Message.ShouldNotContain(ratio.ToString(System.Globalization.CultureInfo.InvariantCulture));
                _ = recorder.Record(AccessibilityEvidenceRow.AE06, new(2, 3, theme), new() { Passed = false, ContrastRatio = ratio });
            }
        }
        JsonSerializer.Deserialize<AccessibilityEvidence>(JsonSerializer.Serialize(dark)).ShouldBe(dark);
        using (JsonDocument data = JsonDocument.Parse(dark.RedactedMeasurements))
        {
            data.RootElement.GetProperty("Passed").GetBoolean().ShouldBeFalse();
            data.RootElement.GetProperty("ContrastRatio").GetDouble().ShouldBe(2.1);
        }
        AccessibilityMeasurements loss = new() { Passed = false, ViewportWidth = 320, ZoomPercent = 400, HorizontalOverflow = 12,
            ContentLoss = true, Clipping = true, Overlap = true, ControlTruncation = true,
            OperationsAvailable = false, ReadingOrderLogical = false, SelectedTabVisible = false,
            LineHeight = 1.5, ParagraphSpacing = 2, LetterSpacing = .12, WordSpacing = .16,
            ForcedColors = true, ForcedColorCuesPreserved = false, ReducedMotion = true,
            NonEssentialAnimationsSuppressed = false, StateCuesPreserved = false, FocusChangesImmediate = false, ExpirySilent = false };
        foreach (AccessibilityEvidenceRow row in new[] { AccessibilityEvidenceRow.AE01, AccessibilityEvidenceRow.AE02, AccessibilityEvidenceRow.AE03,
            AccessibilityEvidenceRow.AE07, AccessibilityEvidenceRow.AE08 })
        {
            AccessibilityEvidence packet = recorder.Record(row, new(2, 4, UxTheme.ForcedColors), loss);
            using JsonDocument data = JsonDocument.Parse(packet.RedactedMeasurements);
            foreach (string key in new[] { "ContentLoss", "Clipping", "Overlap", "ControlTruncation", "ForcedColors", "ReducedMotion" })
            { data.RootElement.GetProperty(key).GetBoolean().ShouldBeTrue(); }
            foreach (string key in new[] { "Passed", "OperationsAvailable", "ReadingOrderLogical", "SelectedTabVisible", "ForcedColorCuesPreserved",
                "NonEssentialAnimationsSuppressed", "StateCuesPreserved", "FocusChangesImmediate", "ExpirySilent" })
            { data.RootElement.GetProperty(key).GetBoolean().ShouldBeFalse(); }
            data.RootElement.GetProperty("HorizontalOverflow").GetDouble().ShouldBe(12);
        }
        foreach (AccessibilityEvidenceRow row in Enum.GetValues<AccessibilityEvidenceRow>())
        { Should.Throw<ArgumentException>(() => recorder.Record(row, new(1, 1, UxTheme.Light), new())); }
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(0, 1, UxTheme.Light), new() { ContrastRatio = 4.5 }));
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(1, 1, (UxTheme)99), new() { ContrastRatio = 4.5 }));
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(1, 1, UxTheme.ForcedColors), new() { ContrastRatio = 4.5 }));
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE09, new(1, 1, UxTheme.Light), new() { Passed = true, Wcag22Aa = true, FindingCount = 1, CriticalFindingCount = 1, AnnouncementCount = 0, KeyboardPassed = true, AxePassed = false, NoHoverOnlyAction = true }));
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(1, 0, UxTheme.Light), new() { ContrastRatio = 4.5 }));
        // Each failed outcome alone contradicts Passed; the same packet stays recordable as a failure.
        foreach ((AccessibilityEvidenceRow row, string fact, object failedValue) in new (AccessibilityEvidenceRow, string, object)[] {
            (AccessibilityEvidenceRow.AE01, "ContentLoss", true), (AccessibilityEvidenceRow.AE01, "Clipping", true),
            (AccessibilityEvidenceRow.AE01, "OperationsAvailable", false), (AccessibilityEvidenceRow.AE01, "ReadingOrderLogical", false),
            (AccessibilityEvidenceRow.AE01, "SelectedTabVisible", false), (AccessibilityEvidenceRow.AE02, "HorizontalOverflow", 1d),
            (AccessibilityEvidenceRow.AE03, "Overlap", true), (AccessibilityEvidenceRow.AE03, "ControlTruncation", true),
            (AccessibilityEvidenceRow.AE07, "ForcedColorCuesPreserved", false),
            (AccessibilityEvidenceRow.AE08, "NonEssentialAnimationsSuppressed", false), (AccessibilityEvidenceRow.AE08, "StateCuesPreserved", false),
            (AccessibilityEvidenceRow.AE08, "FocusChangesImmediate", false), (AccessibilityEvidenceRow.AE08, "ExpirySilent", false),
            (AccessibilityEvidenceRow.AE09, "KeyboardPassed", false), (AccessibilityEvidenceRow.AE09, "AxePassed", false),
            (AccessibilityEvidenceRow.AE09, "NoHoverOnlyAction", false), (AccessibilityEvidenceRow.AE09, "Wcag22Aa", false),
            (AccessibilityEvidenceRow.AE09, "CriticalFindingCount", 1),
            (AccessibilityEvidenceRow.AE04, "Targets", new AccessibilityTargetEvidence[] { new(1, new(0, 0, 12, 12), [UxKeyboardKey.Tab]) }),
            (AccessibilityEvidenceRow.AE04, "Targets", new AccessibilityTargetEvidence[] { new(1, new(0, 0, 0, 0), [UxKeyboardKey.Tab],
                UxTargetSizeExceptionKind.Inline, UxTargetSizeRationale.InlineText) }),
            (AccessibilityEvidenceRow.AE05, "Occluders", new UxRect[] { new(0, 90, 320, 64) }) })
        {
            AccessibilityMeasurements measured = CompleteAccessibilityMeasurements(row);
            _ = recorder.Record(row, ContextFor(row), measured);
            typeof(AccessibilityMeasurements).GetProperty(fact)!.SetValue(measured, failedValue);
            ArgumentException contradiction = Should.Throw<ArgumentException>(() => recorder.Record(row, ContextFor(row), measured));
            contradiction.Message.ShouldContain("cannot be Passed");
            contradiction.Message.ShouldContain(fact);
            if (fact == "Targets" && measured.Targets[0].Bounds.Width == 0) { contradiction.Message.ShouldContain("Bounds"); }
            typeof(AccessibilityMeasurements).GetProperty("Passed")!.SetValue(measured, false);
            _ = recorder.Record(row, ContextFor(row), measured);
        }
        // Measurements taken under another row's canonical setup, or a theme that contradicts forced colors, are not that row's evidence.
        foreach ((AccessibilityEvidenceRow row, string setup, object? value) in new (AccessibilityEvidenceRow, string, object?)[] {
            (AccessibilityEvidenceRow.AE01, "ViewportWidth", 1280d), (AccessibilityEvidenceRow.AE01, "ViewportWidth", 0d),
            (AccessibilityEvidenceRow.AE01, "ViewportWidth", -1d), (AccessibilityEvidenceRow.AE01, "ViewportWidth", 320.01),
            (AccessibilityEvidenceRow.AE02, "ViewportWidth", 1280d), (AccessibilityEvidenceRow.AE02, "ViewportWidth", 0d),
            (AccessibilityEvidenceRow.AE02, "ViewportWidth", -1d), (AccessibilityEvidenceRow.AE02, "ViewportWidth", 320.01),
            (AccessibilityEvidenceRow.AE02, "ZoomPercent", 399.99), (AccessibilityEvidenceRow.AE02, "ZoomPercent", 200d),
            (AccessibilityEvidenceRow.AE03, "LineHeight", 1.2), (AccessibilityEvidenceRow.AE03, "WordSpacing", .1),
            (AccessibilityEvidenceRow.AE03, "ParagraphSpacing", 1.99), (AccessibilityEvidenceRow.AE03, "LetterSpacing", .119),
            (AccessibilityEvidenceRow.AE07, "ForcedColors", false), (AccessibilityEvidenceRow.AE08, "ReducedMotion", false),
            (AccessibilityEvidenceRow.AE06, "ForcedColors", true) })
        {
            AccessibilityMeasurements wrong = CompleteAccessibilityMeasurements(row);
            typeof(AccessibilityMeasurements).GetProperty(setup)!.SetValue(wrong, value);
            ArgumentException setupFailure = Should.Throw<ArgumentException>(() => recorder.Record(row, ContextFor(row), wrong));
            setupFailure.Message.ShouldContain("another row's setup");
            setupFailure.Message.ShouldContain(setup);
        }
        // A non-forced-colours measurement also contradicts a ForcedColors context on another row.
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE01, new(1, 1, UxTheme.ForcedColors),
            CompleteAccessibilityMeasurements(AccessibilityEvidenceRow.AE01))).Message.ShouldContain("ForcedColors");
        foreach (AccessibilityEvidenceRow row in new[] { AccessibilityEvidenceRow.AE01, AccessibilityEvidenceRow.AE02 })
        {
            foreach (double width in new[] { 319d, 320d })
            {
                AccessibilityMeasurements boundary = CompleteAccessibilityMeasurements(row);
                typeof(AccessibilityMeasurements).GetProperty("ViewportWidth")!.SetValue(boundary, width);
                _ = recorder.Record(row, ContextFor(row), boundary);
            }
        }
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE07, new(1, 1, UxTheme.Light),
            CompleteAccessibilityMeasurements(AccessibilityEvidenceRow.AE07)));
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE07, new(1, 1, UxTheme.Light),
            CompleteAccessibilityMeasurements()));
        Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE04, new(1, 1, UxTheme.Light),
            new() { Targets = [new(0, new(0, 0, 24, 24), [UxKeyboardKey.Tab])] })).Message.ShouldContain("positive target");
        foreach (int ordinal in new[] { 0, -1 })
        {
            Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE04, new(1, 1, UxTheme.Light),
                new() { Targets = [new(1, new(0, 0, 12, 12), [UxKeyboardKey.Tab], UxTargetSizeExceptionKind.Equivalent,
                    UxTargetSizeRationale.SameFunctionConformingControl, EquivalentControlNumber: ordinal, EquivalentControlBounds: new(40, 0, 24, 24))] }))
                .Message.ShouldContain("EquivalentControlNumber");
        }
    }

    [Theory]
    [MemberData(nameof(CanonicalSurfaceCases))]
    public void UxStates_AllCanonicalRows_RequireIndependentMeasuredSemantics(string row, UxSurfaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        UxStateAssertions.AssertState(row, snapshot);
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { State = "wrong" }));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Terminal = !snapshot.Terminal }));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Facts = null }));
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Facts = snapshot.Facts! with { SuccessClaim = !snapshot.Facts!.SuccessClaim } }));
        if (snapshot.Lifecycle is not null)
        { Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Lifecycle = CommandLifecycleState.Idle })); }
        if (row is "SS-25" or "SS-36" or "SS-37" or "SS-38")
        {
            Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Lifecycle = CommandLifecycleState.Submitting }))
                .Message.ShouldContain("DidNotRun");
        }
        foreach (PropertyInfo property in typeof(UxSurfaceFacts).GetProperties())
        {
            if (property.GetValue(snapshot.Facts) is not bool value) { continue; }
            UxSurfaceFacts drift = snapshot.Facts! with { };
            property.SetValue(drift, !value);
            InvalidOperationException failure = Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Facts = drift }));
            failure.Message.ShouldContain("Facts." + property.Name);
            // A null fact is unmeasured, and must fail independently of every measured neighbour.
            property.SetValue(drift, null);
            Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Facts = drift }))
                .Message.ShouldContain("Facts." + property.Name);
        }
        if (row is "SS-31" or "SS-32" or "SS-33")
        { Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Semantics = snapshot.Semantics with { Role = "button" } })); }
        if (row is "SS-05" or "SS-06" or "SS-28" or "SS-37" or "SS-38")
        { Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row, snapshot with { Semantics = snapshot.Semantics with { AriaLive = "polite" } })); }
    }

    [Fact]
    public void UxStates_HiddenDeniedEntriesAndSelectedDisabledTab_AllowCanonicalAlternatives()
    {
        UxSurfaceSnapshot hidden = new("NoAccess", true, new("", "", false), Facts: new()
            { Visible = false, PolicyHidden = true, SuccessClaim = false });
        UxStateAssertions.AssertState("SS-06", hidden);
        foreach (string role in new[] { "alert", "status", "log" })
        {
            Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-06", hidden with { Semantics = hidden.Semantics with { Role = role } }));
            foreach (object[] item in CanonicalSurfaceCases().Where(c => c[0] is "SS-31" or "SS-33" or "SS-36" or "SS-39" or "SS-40" or "SS-43" or "SS-44" or "SS-45" or "SS-46" or "SS-47"))
            {
                UxSurfaceSnapshot snapshot = (UxSurfaceSnapshot)item[1];
                Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState((string)item[0], snapshot with
                    { Semantics = snapshot.Semantics with { Role = role } }));
            }
        }
        UxSurfaceSnapshot ready = (UxSurfaceSnapshot)CanonicalSurfaceCases().Single(c => c[0] is "SS-36")[1];
        UxStateAssertions.AssertState("SS-36", ready with { Lifecycle = CommandLifecycleState.Idle });
        foreach (object[] item in CanonicalSurfaceCases().Where(c => c[0] is "SS-31" or "SS-33" or "SS-36" or "SS-39" or "SS-40" or "SS-43" or "SS-44" or "SS-45" or "SS-46" or "SS-47"))
        {
            string row = (string)item[0];
            UxSurfaceSnapshot snapshot = (UxSurfaceSnapshot)item[1];
            UxStateAssertions.AssertState(row, snapshot with { Semantics = snapshot.Semantics with { AriaLive = "off" } });
            foreach (string live in new[] { "polite", "assertive" })
            {
                Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState(row,
                    snapshot with { Semantics = snapshot.Semantics with { AriaLive = live } })).Message.ShouldContain("Semantics.Silent");
            }
        }
        foreach (string role in new[] { "status", "log" })
        {
            UxStateAssertions.AssertState("SS-36", ready with { Semantics = ready.Semantics with { Role = role, AriaLive = "off" } });
            foreach (string? live in new string?[] { null, "polite", "assertive" })
            {
                Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-36",
                    ready with { Semantics = ready.Semantics with { Role = role, AriaLive = live } })).Message.ShouldContain("Semantics.Silent");
            }
        }
        foreach (object[] item in CanonicalSurfaceCases().Where(c => c[0] is "SS-05" or "SS-06" or "SS-28" or "SS-37" or "SS-38"))
        {
            UxSurfaceSnapshot snapshot = (UxSurfaceSnapshot)item[1];
            foreach (string live in new[] { "off", "polite", "assertive" })
            {
                Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState((string)item[0],
                    snapshot with { Semantics = snapshot.Semantics with { AriaLive = live } })).Message.ShouldContain("Semantics.FocusOnly");
            }
            Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState((string)item[0],
                snapshot with { Semantics = snapshot.Semantics with { AriaAtomic = "true" } })).Message.ShouldContain("Semantics.FocusOnly");
        }
        foreach (UxSurfaceSnapshot contradiction in new[] { ready with { Lifecycle = CommandLifecycleState.Submitting }, ready with { Dispatched = true }, ready with { Queued = true } })
        { Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-36", contradiction)); }
        foreach (object[] item in CanonicalSurfaceCases().Where(c => c[0] is "SS-40" or "SS-43" or "SS-44"))
        {
            UxSurfaceSnapshot snapshot = (UxSurfaceSnapshot)item[1];
            Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState((string)item[0], snapshot with { Semantics = snapshot.Semantics with { Role = "button" } }));
        }
        foreach (string malformed in new[] { "SS-+1", "SS- 1", "SS-١١" })
        { Should.Throw<ArgumentOutOfRangeException>(() => UxStateAssertions.AssertState(malformed, ready)); }
        Should.Throw<InvalidOperationException>(() => UxStateAssertions.AssertState("SS-06", hidden with
            { Semantics = hidden.Semantics with { IsFocused = true } }));
        UxStateAssertions.AssertState("SS-33", new("DisabledTab", true, new("tab", "Previously selected", false), Facts: new()
            { Visible = true, SuccessClaim = false, Disabled = true, Selected = true }));
    }

    public static IEnumerable<object[]> CanonicalSurfaceCases()
    {
        yield return ["SS-01", new UxSurfaceSnapshot("NoRegistrations", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Empty = true, FailureVisible = false })];
        yield return ["SS-02", new UxSurfaceSnapshot("Loading", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, LoadingVisible = true })];
        yield return ["SS-03", new UxSurfaceSnapshot("Empty", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Empty = true, FailureVisible = false })];
        yield return ["SS-04", new UxSurfaceSnapshot("Data", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, DataVisible = true, Empty = false, FailureVisible = false })];
        yield return ["SS-05", new UxSurfaceSnapshot("NoTenant", true, new("heading", "Measured surface", true, TabIndex: "-1"), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })];
        yield return ["SS-06", new UxSurfaceSnapshot("NoAccess", true, new("heading", "Measured surface", true, TabIndex: "-1"), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })];
        yield return ["SS-07", new UxSurfaceSnapshot("Loading", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, LoadingVisible = true })];
        yield return ["SS-08", new UxSurfaceSnapshot("Empty", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Empty = true, FailureVisible = false })];
        yield return ["SS-09", new UxSurfaceSnapshot("Data", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, DataVisible = true, Empty = false, FailureVisible = false })];
        yield return ["SS-10", new UxSurfaceSnapshot("Stale", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-11", new UxSurfaceSnapshot("Reconnecting", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-12", new UxSurfaceSnapshot("FallbackPolling", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-13", new UxSurfaceSnapshot("SlowQuery", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-14", new UxSurfaceSnapshot("MaxItems", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-15", new UxSurfaceSnapshot("FilterNoMatches", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Empty = true, FiltersVisible = true, FailureVisible = false })];
        yield return ["SS-16", new UxSurfaceSnapshot("Submitting", false, new("group", "Measured surface", false), CommandLifecycleState.Submitting, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-17", new UxSurfaceSnapshot("Acknowledged", false, new("group", "Measured surface", false), CommandLifecycleState.Acknowledged, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-18", new UxSurfaceSnapshot("Syncing", false, new("group", "Measured surface", false), CommandLifecycleState.Syncing, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-19", new UxSurfaceSnapshot("Confirmed", true, new("group", "Measured surface", false), CommandLifecycleState.Confirmed, Facts: new() { Visible = true, SuccessClaim = true, StateCueVisible = true, FailureVisible = false })];
        yield return ["SS-20", new UxSurfaceSnapshot("Rejected", true, new("group", "Measured surface", false), CommandLifecycleState.Rejected, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })];
        yield return ["SS-21", new UxSurfaceSnapshot("IdempotentConfirmed", true, new("group", "Measured surface", false), CommandLifecycleState.IdempotentConfirmed, Facts: new() { Visible = true, SuccessClaim = true, StateCueVisible = true, FailureVisible = false })];
        yield return ["SS-22", new UxSurfaceSnapshot("NeedsReview", true, new("group", "Measured surface", false), CommandLifecycleState.NeedsReview, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })];
        yield return ["SS-23", new UxSurfaceSnapshot("Warning", true, new("group", "Measured surface", false), CommandLifecycleState.Warning, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true })];
        yield return ["SS-24", new UxSurfaceSnapshot("Degraded", false, new("group", "Measured surface", false), CommandLifecycleState.Degraded, Facts: new() { Visible = true, SuccessClaim = false, StateCueVisible = true, PollingActive = true, FailureVisible = true })];
        yield return ["SS-25", new UxSurfaceSnapshot("BlockedSubmit", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, OriginalOperationVisible = true })];
        yield return ["SS-26", new UxSurfaceSnapshot("DegradedExhausted", true, new("group", "Measured surface", false), CommandLifecycleState.DegradedExhausted, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, PollingActive = false })];
        yield return ["SS-27", new UxSurfaceSnapshot("Bootstrap", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, LoadingVisible = true })];
        yield return ["SS-28", new UxSurfaceSnapshot("StartupFailure", true, new("heading", "Measured surface", true, TabIndex: "-1"), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })];
        yield return ["SS-29", new UxSurfaceSnapshot("RouteFailure", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, CurrentRoutePreserved = true })];
        yield return ["SS-30", new UxSurfaceSnapshot("Loading", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, LoadingVisible = true })];
        yield return ["SS-31", new UxSurfaceSnapshot("SelectedTab", true, new("tab", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Selected = true, Disabled = false, PanelVisible = true, PanelOwnedByTab = true })];
        yield return ["SS-32", new UxSurfaceSnapshot("InvalidTabFallback", true, new("tab", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Selected = true, Disabled = false, PanelVisible = true, PanelOwnedByTab = true, InvalidTargetSelected = false })];
        yield return ["SS-33", new UxSurfaceSnapshot("DisabledTab", true, new("tab", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Disabled = true })];
        yield return ["SS-34", new UxSurfaceSnapshot("QueryFailure", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, Empty = false })];
        yield return ["SS-35", new UxSurfaceSnapshot("Offline", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, StateCueVisible = true })];
        yield return ["SS-36", new UxSurfaceSnapshot("Ready", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Editable = true })];
        yield return ["SS-37", new UxSurfaceSnapshot("ValidationBlocked", true, new("group", "Measured surface", true, TabIndex: "-1"), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, Editable = true })];
        yield return ["SS-38", new UxSurfaceSnapshot("AuthorizationDenied", true, new("heading", "Measured surface", true, TabIndex: "-1"), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true })];
        yield return ["SS-39", new UxSurfaceSnapshot("AbandonmentDecision", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, InFlow = true, Modal = false, Editable = true })];
        yield return ["SS-40", new UxSurfaceSnapshot("PaletteOpen", false, new("dialog", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Modal = true })];
        yield return ["SS-41", new UxSurfaceSnapshot("PaletteNoResults", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Empty = true, Editable = true })];
        yield return ["SS-42", new UxSurfaceSnapshot("PaletteActivationFailure", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, FailureVisible = true, CurrentRoutePreserved = true })];
        yield return ["SS-43", new UxSurfaceSnapshot("SettingsOpen", false, new("dialog", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Modal = true })];
        yield return ["SS-44", new UxSurfaceSnapshot("DestructiveConfirmationOpen", false, new("dialog", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, Modal = true })];
        yield return ["SS-45", new UxSurfaceSnapshot("AbandonmentWarning", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, InFlow = true, Modal = false, Editable = true })];
        yield return ["SS-46", new UxSurfaceSnapshot("OverlayClosed", true, new("button", "Measured surface", true), null, Facts: new() { Visible = true, SuccessClaim = false, FocusRestored = true, CurrentRoutePreserved = true })];
        yield return ["SS-47", new UxSurfaceSnapshot("OriginRemoved", true, new("heading", "Measured surface", true, TabIndex: "-1"), null, Facts: new() { Visible = true, SuccessClaim = false, FocusRestored = true, CurrentRoutePreserved = true, Disabled = false })];
        yield return ["SS-48", new UxSurfaceSnapshot("FreshRow", false, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, UpdatedCueVisible = true })];
        yield return ["SS-49", new UxSurfaceSnapshot("FilterHiddenDetail", true, new("group", "Measured surface", false), null, Facts: new() { Visible = true, SuccessClaim = false, DetailHidden = true, FiltersVisible = true })];
    }

    [Theory]
    [MemberData(nameof(CanonicalAnnouncementCases))]
    public void UxCatalog_EveryResourceAndVariant_MatchesIndependentLocalizedCopy(string row, string resource, string? variant,
        string english, string french, UxAnnouncementChannel channel)
    {
        int number = int.Parse(row.AsSpan(3), CultureInfo.InvariantCulture);
        object[] args = number switch { 1 or 2 or 3 or 21 or 27 or 31 => ["Orders"], 9 or 18 or 19 => [2], 23 => ["Orders", "Home"], _ => [] };
        System.Resources.ResourceManager resources = new(typeof(Hexalith.FrontComposer.Shell.Resources.FcShellResources));
        foreach ((string culture, string literal) in new[] { ("en-US", english), ("fr-FR", french) })
        {
            CultureInfo locale = CultureInfo.GetCultureInfo(culture);
            string independentlyExpected = string.Format(locale, literal, args);
            string.Format(locale, resources.GetString(resource, locale)!, args).ShouldBe(independentlyExpected);
            UxAnnouncement result = UxAnnouncementCatalog.Expected(row, "operation", "state", args, locale, variant);
            result.Copy.ShouldBe(independentlyExpected);
            result.Channel.ShouldBe(channel);
            result.RowId.ShouldBe(row);
        }
    }

    public static IEnumerable<object?[]> CanonicalAnnouncementCases()
    {
        yield return ["AM-01", "Am01Loading", null, "Loading {0}.", "Chargement de {0}.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-02", "Am02Empty", null, "No {0} available.", "Aucun résultat disponible pour les {0}.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-03", "Am03Loaded", null, "{0} loaded.", "Chargement des {0} terminé.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-04", "Am04Stale", null, "Data may be out of date.", "Les données peuvent être obsolètes.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-05", "Am05Reconnecting", null, "Connection lost. Reconnecting.", "Connexion perdue. Reconnexion en cours.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-06", "Am06Fallback", null, "Live updates are unavailable. Checking for updates.", "Les mises à jour en direct sont indisponibles. Recherche de mises à jour.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-07", "Am07Recovery", null, "Connection restored. Data refreshed.", "Connexion rétablie. Données actualisées.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-08", "Am08SlowQuery", null, "This is taking longer than expected.", "Cette opération prend plus de temps que prévu.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-09", "Am09MaxItems", null, "Showing the first {0} items. Refine filters to narrow the result.", "Affichage des {0} premiers éléments. Affinez les filtres pour réduire le résultat.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-10", "Am10Submitting", null, "Submitting command.", "Envoi de la commande.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-11", "Am11Acknowledged", null, "Command accepted. Waiting for confirmation.", "Commande acceptée. En attente de confirmation.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-12", "Am12Syncing", null, "Command accepted. Updating the view.", "Commande acceptée. Mise à jour de la vue.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-13", "Am13Confirmed", null, "Command confirmed.", "Commande confirmée.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-14", "Am14Rejected", null, "Command rejected. Review the message and try again.", "Commande rejetée. Consultez le message et réessayez.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-15", "Am15NeedsReview", null, "Command needs review before its result can be confirmed.", "La commande doit être examinée avant que son résultat puisse être confirmé.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-16", "Am16Warning", null, "Command completed with a warning. Review the details.", "Commande terminée avec un avertissement. Consultez les détails.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-17", "Am17Degraded", null, "Confirmation is taking longer than expected. Review status or continue working.", "La confirmation prend plus de temps que prévu. Consultez l’état ou poursuivez votre travail.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-18", "ValidationSummaryMessage", null, "Correct the errors before submitting. {0} errors.", "Corrigez les erreurs avant l'envoi. {0} erreurs.", UxAnnouncementChannel.FocusedSummary];
        yield return ["AM-19", "MappedRejectionSummaryMessage", null, "The command was rejected. Correct the mapped errors before submitting again. {0} errors.", "La commande a été rejetée. Corrigez les erreurs associées avant un nouvel envoi. {0} erreurs.", UxAnnouncementChannel.FocusedSummary];
        yield return ["AM-20", "CommandBlockedMessage", null, "This command did not run. Another command is already in progress.", "Cette commande n'a pas été exécutée. Une autre commande est déjà en cours.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-21", "Am21UpdatedRow", null, "Updated row: {0}.", "Ligne mise à jour : {0}.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-22", "Am22DetailHidden", null, "The expanded row is hidden by the current filters.", "La ligne développée est masquée par les filtres actuels.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-23", "RouteNavigationFailedNamedText", null, "Could not open {0}. You remain on {1}.", "Impossible d’ouvrir {0}. Vous restez sur {1}.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-24", "Am24Exhausted", null, "Confirmation was not received. Review status later or continue working.", "Confirmation non reçue. Consultez l’état plus tard ou poursuivez votre travail.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-25", "ShellNoModulesText", null, "No modules are available.", "Aucun module n'est disponible.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-26", "ScopeBlockedHeading", "ScopeBlockedHeading", "Workspace unavailable", "Espace de travail indisponible", UxAnnouncementChannel.FocusedHeading];
        yield return ["AM-27", "Am27NoMatches", null, "No {0} match these filters.", "Aucun résultat pour les {0} avec ces filtres.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-28", "PaletteNoCommandsOrPagesText", null, "No commands or pages match.", "Aucune commande ni page ne correspond.", UxAnnouncementChannel.PoliteComboboxStatus];
        yield return ["AM-29", "StartupFailureText", null, "FrontComposer could not start. Review the configuration.", "FrontComposer n'a pas pu démarrer. Vérifiez la configuration.", UxAnnouncementChannel.FocusedHeading];
        yield return ["AM-30", "Am30QueryFailed", null, "Data could not be loaded.", "Les données n’ont pas pu être chargées.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-31", "ModuleTabUnavailableTemplate", null, "That page is unavailable. Showing {0}.", "Cette page est indisponible. Affichage de {0}.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-18", "ValidationSummaryMessageSingle", "single", "Correct the errors before submitting. One error.", "Corrigez les erreurs avant l'envoi. Une erreur.", UxAnnouncementChannel.FocusedSummary];
        yield return ["AM-19", "MappedRejectionSummaryMessageSingle", "single", "The command was rejected. Correct the mapped error before submitting again. One error.", "La commande a été rejetée. Corrigez l'erreur associée avant un nouvel envoi. Une erreur.", UxAnnouncementChannel.FocusedSummary];
        yield return ["AM-30", "Am30Offline", "offline", "You are offline. Data cannot be refreshed.", "Vous êtes hors ligne. Les données ne peuvent pas être actualisées.", UxAnnouncementChannel.PoliteStatus];
        yield return ["AM-26", "RouteForbiddenHeading", "RouteForbiddenHeading", "Forbidden", "Accès interdit", UxAnnouncementChannel.FocusedHeading];
        yield return ["AM-26", "UnauthorizedCommandWarningTitle", "UnauthorizedCommandWarningTitle", "Permission required", "Autorisation requise", UxAnnouncementChannel.FocusedHeading];
        yield return ["AM-26", "UnauthenticatedCommandWarningTitle", "UnauthenticatedCommandWarningTitle", "Sign-in required", "Connexion requise", UxAnnouncementChannel.FocusedHeading];
        yield return ["AM-26", "AuthorizationActionUnavailableTitle", "AuthorizationActionUnavailableTitle", "Action temporarily unavailable", "Action momentanément indisponible", UxAnnouncementChannel.FocusedHeading];
    }

    [Fact]
    public async Task UxClock_SuppressedFlowAndAsyncDisposal_IsolateAndDrainCallbacks()
    {
        TestTimeProvider clock = new();
        AsyncLocal<string?> ambient = new() { Value = "must not flow" };
        string? observed = "not invoked";
        int callbackThread = 0;
        ITimer suppressed;
        using (ExecutionContext.SuppressFlow())
        {
            suppressed = clock.CreateTimer(_ => { observed = ambient.Value; callbackThread = Environment.CurrentManagedThreadId; },
                null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
        using (suppressed)
        {
            ambient.Value = "advancing thread";
            int advancingThread = Environment.CurrentManagedThreadId;
            clock.Advance(TimeSpan.Zero);
            observed.ShouldBeNull();
            callbackThread.ShouldBe(advancingThread);
            ambient.Value.ShouldBe("advancing thread");
        }
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int invocations = 0;
        using ITimer active = clock.CreateTimer(_ =>
        {
            invocations++;
            entered.SetResult();
            release.Task.GetAwaiter().GetResult();
        }, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));
        Task advancement = Task.Run(() => clock.Advance(TimeSpan.FromMilliseconds(2)), Xunit.TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        ValueTask disposal = active.DisposeAsync();
        try
        {
            disposal.IsCompleted.ShouldBeFalse();
            active.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan).ShouldBeFalse();
        }
        finally { release.SetResult(); }
        await disposal.ConfigureAwait(true);
        await advancement.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        invocations.ShouldBe(1);
        await active.DisposeAsync().ConfigureAwait(true);
    }

    [Fact]
    public void UxEvidence_RequiredFactsAndFiniteValues_RejectIndividualOmissionsAndPreserveCompactPackets()
    {
        TestAccessibilityEvidenceRecorder recorder = new(new() { MaxDiagnosticPayloadCharacters = 8192 });
        (AccessibilityEvidenceRow Row, string[] Required)[] cases =
        [
            (AccessibilityEvidenceRow.AE01, ["ViewportWidth", "ContentLoss", "Clipping", "OperationsAvailable", "ReadingOrderLogical", "SelectedTabVisible", "HorizontalOverflow"]),
            (AccessibilityEvidenceRow.AE02, ["ViewportWidth", "ZoomPercent", "ContentLoss", "Clipping", "OperationsAvailable", "ReadingOrderLogical", "SelectedTabVisible", "HorizontalOverflow"]),
            (AccessibilityEvidenceRow.AE03, ["LineHeight", "ParagraphSpacing", "LetterSpacing", "WordSpacing", "ContentLoss", "Clipping", "Overlap", "ControlTruncation"]),
            (AccessibilityEvidenceRow.AE04, ["Targets"]),
            (AccessibilityEvidenceRow.AE05, ["FocusTarget", "FocusIndicator", "ViewportAndScrollports"]),
            (AccessibilityEvidenceRow.AE06, ["ContrastRatio"]),
            (AccessibilityEvidenceRow.AE07, ["ForcedColors", "ForcedColorCuesPreserved"]),
            (AccessibilityEvidenceRow.AE08, ["ReducedMotion", "NonEssentialAnimationsSuppressed", "StateCuesPreserved", "FocusChangesImmediate", "ExpirySilent"]),
            (AccessibilityEvidenceRow.AE09, ["Wcag22Aa", "FindingCount", "CriticalFindingCount", "AnnouncementCount", "KeyboardPassed", "AxePassed", "NoHoverOnlyAction"]),
        ];
        foreach ((AccessibilityEvidenceRow row, string[] required) in cases)
        {
            _ = recorder.Record(row, ContextFor(row), CompleteAccessibilityMeasurements(row));
            foreach (string name in required)
            {
                AccessibilityMeasurements incomplete = CompleteAccessibilityMeasurements(row);
                PropertyInfo property = typeof(AccessibilityMeasurements).GetProperty(name)!;
                object? missing = name == "Targets" ? Array.Empty<AccessibilityTargetEvidence>()
                    : name == "ViewportAndScrollports" ? Array.Empty<UxRect>() : null;
                property.SetValue(incomplete, missing);
                Should.Throw<ArgumentException>(() => recorder.Record(row, ContextFor(row), incomplete)).Message.ShouldContain(name);
            }
        }
        foreach (PropertyInfo property in typeof(AccessibilityMeasurements).GetProperties().Where(p => p.PropertyType == typeof(double?)))
        {
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                AccessibilityMeasurements nonfinite = CompleteAccessibilityMeasurements();
                property.SetValue(nonfinite, invalid);
                Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(1, 1, UxTheme.Light), nonfinite));
            }
        }
        UxRect invalidRect = new(0, double.NaN, 24, 24);
        foreach (string geometry in new[] { "FocusTarget", "FocusIndicator", "ViewportAndScrollports", "Occluders", "Targets" })
        {
            AccessibilityMeasurements nonfinite = CompleteAccessibilityMeasurements();
            object invalid = geometry is "Targets" ? new AccessibilityTargetEvidence[] { new(1, invalidRect, [UxKeyboardKey.Tab]) }
                : geometry is "ViewportAndScrollports" or "Occluders" ? new[] { invalidRect } : invalidRect;
            typeof(AccessibilityMeasurements).GetProperty(geometry)!.SetValue(nonfinite, invalid);
            Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(1, 1, UxTheme.Light), nonfinite));
        }
        foreach (AccessibilityTargetEvidence invalid in new[] {
            new AccessibilityTargetEvidence(1, new(0, 0, 24, 24), [UxKeyboardKey.Tab], Neighbors: [invalidRect]),
            new AccessibilityTargetEvidence(1, new(0, 0, 24, 24), [UxKeyboardKey.Tab], EquivalentControlBounds: invalidRect) })
        {
            AccessibilityMeasurements nonfinite = CompleteAccessibilityMeasurements();
            typeof(AccessibilityMeasurements).GetProperty("Targets")!.SetValue(nonfinite, new[] { invalid });
            Should.Throw<ArgumentException>(() => recorder.Record(AccessibilityEvidenceRow.AE06, new(1, 1, UxTheme.Light), nonfinite));
        }
        TestAccessibilityEvidenceRecorder compact = new(new());
        foreach ((AccessibilityEvidenceRow row, AccessibilityMeasurements measurements) in new[] {
            (AccessibilityEvidenceRow.AE06, new AccessibilityMeasurements { Passed = false, ContrastRatio = 0 }),
            (AccessibilityEvidenceRow.AE07, new AccessibilityMeasurements { Passed = false, ForcedColors = true, ForcedColorCuesPreserved = false }),
            (AccessibilityEvidenceRow.AE09, new AccessibilityMeasurements { Passed = false, Wcag22Aa = true, FindingCount = 0, CriticalFindingCount = 0,
                AnnouncementCount = 0, KeyboardPassed = false, AxePassed = false, NoHoverOnlyAction = false }) })
        {
            AccessibilityEvidence evidence = compact.Record(row, ContextFor(row), measurements);
            using JsonDocument packet = JsonDocument.Parse(evidence.RedactedMeasurements);
            packet.RootElement.GetProperty("Passed").GetBoolean().ShouldBeFalse();
            if (row == AccessibilityEvidenceRow.AE06) { packet.RootElement.GetProperty("ContrastRatio").GetDouble().ShouldBe(0); }
            if (row == AccessibilityEvidenceRow.AE07) { packet.RootElement.GetProperty("ForcedColorCuesPreserved").GetBoolean().ShouldBeFalse(); }
            if (row == AccessibilityEvidenceRow.AE09)
            {
                packet.RootElement.GetProperty("CriticalFindingCount").GetInt32().ShouldBe(0);
                packet.RootElement.GetProperty("FindingCount").GetInt32().ShouldBe(0);
                packet.RootElement.GetProperty("AnnouncementCount").GetInt32().ShouldBe(0);
                packet.RootElement.GetProperty("KeyboardPassed").GetBoolean().ShouldBeFalse();
            }
        }
        RedactedEvidenceFormatter.FormatAccessibility(CompleteAccessibilityMeasurements(), new()).ShouldEndWith("...<truncated>");
    }

    private static AccessibilityEvidenceContext ContextFor(AccessibilityEvidenceRow row)
        => new(1, 1, row == AccessibilityEvidenceRow.AE07 ? UxTheme.ForcedColors : UxTheme.Light);

    private static AccessibilityMeasurements CompleteAccessibilityMeasurements(AccessibilityEvidenceRow? row = null) => new()
    {
        Passed = true, ViewportWidth = 320, ZoomPercent = 400, HorizontalOverflow = 0,
        ContentLoss = false, Clipping = false, Overlap = false, ControlTruncation = false,
        OperationsAvailable = true, ReadingOrderLogical = true, SelectedTabVisible = true,
        LineHeight = 1.5, ParagraphSpacing = 2, LetterSpacing = .12, WordSpacing = .16, ContrastRatio = 4.5,
        ForcedColors = row == AccessibilityEvidenceRow.AE07, ForcedColorCuesPreserved = true, ReducedMotion = true,
        NonEssentialAnimationsSuppressed = true, StateCuesPreserved = true, FocusChangesImmediate = true, ExpirySilent = true,
        FindingCount = 0, CriticalFindingCount = 0, Wcag22Aa = true, AnnouncementCount = 1,
        KeyboardPassed = true, AxePassed = true, NoHoverOnlyAction = true,
        FocusTarget = new(20, 100, 24, 24), FocusIndicator = new(18, 98, 28, 28),
        ViewportAndScrollports = [new(0, 0, 320, 640)], Occluders = [new(0, 0, 320, 64)],
        Targets = [new(1, new(20, 100, 24, 24), [UxKeyboardKey.Tab, UxKeyboardKey.Enter])],
    };

    private sealed class TestCommand;

    private static void ConfigureOutcome(TestCommandService service, TestCommandOutcome outcome) {
        switch (outcome) {
            case TestCommandOutcome.Success:
                service.Succeed();
                break;
            case TestCommandOutcome.Rejected:
                service.Reject("configured rejection", "correct the command");
                break;
            case TestCommandOutcome.Timeout:
                service.Timeout();
                break;
            case TestCommandOutcome.StallAtSyncing:
                service.StallAtSyncing();
                break;
            default:
                throw new InvalidOperationException("Unsupported test outcome.");
        }
    }

    private static CyclicPayload CreateCyclicPayload() {
        CyclicPayload payload = new();
        payload.Self = payload;
        return payload;
    }

    private sealed class CyclicPayload {
        public CyclicPayload? Self { get; set; }
    }

    private sealed class UnsupportedPayload {
        public Action Callback { get; } = static () => { };
    }

    private sealed class ThrowingPayload {
        private readonly string _secret = "payload-secret";

        public string Value => throw new InvalidOperationException(_secret);
    }

    private sealed class ExceptionalPayload(Exception exception) {
        public string Value => throw exception;
    }

    private sealed class BuilderModel {
        public string? Name { get; set; }
        public string? Code { get; set; }
    }

    private sealed class DuringSetupTestBase : FrontComposerTestBase {
        public DuringSetupTestBase()
            : base(options => options.StoreInitialization = StoreInitializationMode.DuringHostSetup) {
        }
    }
}
