using System.Globalization;

using Bunit;

using Counter.Domain;
using Counter.Web.Components.Replacements;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Testing.Tests;

public sealed class FrontComposerTestHostTests {
    [Fact]
    public async Task FrontComposerTestBase_DefaultSetup_RegistersDeterministicServices() {
        using TestHost host = new();

        await host.InitializeAsync();

        _ = host.Services.GetRequiredService<IStorageService>().ShouldBeOfType<InMemoryStorageService>();
        FrontComposerTestUserContextAccessor user = host.Services.GetRequiredService<FrontComposerTestUserContextAccessor>();
        user.TenantId.ShouldBe("tenant-a");
        user.UserId.ShouldBe("user-a");
        host.Services.GetRequiredService<ICommandService>().ShouldBeSameAs(host.ExposedCommandService);
    }

    [Fact]
    public void AddFrontComposerTestHost_DefaultSetup_RegistersShellServicesAndHonorsJsInteropMode() {
        using BunitContext context = new();
        FixedTimeProvider timeProvider = new(DateTimeOffset.Parse("2026-06-05T10:15:00Z", CultureInfo.InvariantCulture));
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options => {
                options.JSInteropMode = JSRuntimeMode.Strict;
                options.TimeProvider = timeProvider;
            });

        context.JSInterop.Mode.ShouldBe(JSRuntimeMode.Strict);
        _ = context.Services.GetRequiredService<IStorageService>().ShouldBeOfType<InMemoryStorageService>();
        context.Services.GetRequiredService<ICommandService>().ShouldBeSameAs(host.CommandService);
        context.Services.GetRequiredService<ICommandServiceWithLifecycle>().ShouldBeSameAs(host.CommandService);
        context.Services.GetRequiredService<IQueryService>().ShouldBeSameAs(host.QueryService);
        context.Services.GetRequiredService<Hexalith.FrontComposer.Shell.State.DataGridNavigation.IProjectionPageLoader>()
            .ShouldBeSameAs(host.PageLoader);
        context.Services.GetRequiredService<TestFaultInjectionProvider>().ShouldBeSameAs(host.FaultProvider);
        context.Services.GetRequiredService<TimeProvider>().ShouldBeSameAs(timeProvider);
    }

    [Fact]
    public void AddDomainAssembly_SameMarkerTwice_RegistersOneAssembly() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);

        _ = host.AddDomainAssembly<CounterDomain>().AddDomainAssembly<CounterDomain>();

        host.Options.DomainAssemblies.ShouldBe([typeof(CounterDomain).Assembly]);
    }

    [Fact]
    public void AddFrontComposerTestHost_DuringHostSetup_CompositionInitializesStore() {
        using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options => options.StoreInitialization = StoreInitializationMode.DuringHostSetup);

        IDispatcher dispatcher = context.Services.GetRequiredService<IDispatcher>();

        Should.NotThrow(() => dispatcher.Dispatch(new CounterProjectionLoadRequestedAction("corr-during-setup")));
    }

    [Fact]
    public async Task AddFrontComposerTestHost_CustomReplacementBeforeStoreInitialization_IsHonored() {
        await using BunitContext context = new();
        FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options => {
                options.TestTenantId = "tenant-b";
                options.TestUserId = "user-b";
            });

        FrontComposerTestUserContextAccessor replacement = new() { TenantId = "tenant-c", UserId = "user-c" };
        _ = context.Services.Replace(ServiceDescriptor.Scoped(_ => replacement));
        _ = context.Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => replacement));

        IStore store = context.Services.GetRequiredService<IStore>();
        await store.InitializeAsync();

        context.Services.GetRequiredService<IUserContextAccessor>().TenantId.ShouldBe("tenant-c");
        host.CommandService.Evidence.ShouldBeEmpty();
    }

    [Fact]
    public async Task TestCommandService_Dispatch_CapturesRedactedEvidenceAndLifecycle() {
        using TestHost host = new();
        ICommandServiceWithLifecycle commandService = host.Services.GetRequiredService<ICommandServiceWithLifecycle>();
        SensitiveCommand command = new() {
            Tenant = "tenant-a",
            User = "user-a",
            Token = "secret-token",
            Amount = 42,
        };

        CommandResult result = await commandService
            .DispatchAsync(command, (_, _) => { }, Xunit.TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        result.Status.ShouldBe("Accepted");
        CommandDispatchEvidence evidence = host.ExposedCommandService.Evidence.Single();
        evidence.TenantId.ShouldBe("tenant-a");
        evidence.UserId.ShouldBe("user-a");
        CommandEvidenceAssertions.AssertLifecycleContains(evidence, Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Confirmed);
        CommandEvidenceAssertions.AssertRedacted(evidence, "tenant-a", "user-a");
        evidence.RedactedPayload.ShouldNotContain("secret-token");
    }

    [Fact]
    public async Task TestCommandService_Dispatch_HonorsCancellationBeforeCapturingEvidence() {
        using TestHost host = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);
        ICommandService commandService = host.Services.GetRequiredService<ICommandService>();

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            commandService.DispatchAsync(new SensitiveCommand(), cts.Token)).ConfigureAwait(true);

        host.ExposedCommandService.Evidence.ShouldBeEmpty();
    }

    [Fact]
    public async Task TestCommandService_Dispatch_CapturesDeterministicCommandContext() {
        using TestHost host = new();
        ICommandServiceWithLifecycle commandService = host.Services.GetRequiredService<ICommandServiceWithLifecycle>();

        CommandResult result = await commandService
            .DispatchAsync(new SensitiveCommand { Amount = 7 }, (_, _) => { }, Xunit.TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        CommandDispatchEvidence evidence = host.ExposedCommandService.Evidence.Single();
        result.MessageId.ShouldBe("test-message-0001");
        result.CorrelationId.ShouldBe("test-correlation-0001");
        evidence.BoundedContext.ShouldBe("Test");
        evidence.CommandName.ShouldBe("Test Command");
        evidence.Status.ShouldBe("Accepted");
        evidence.LifecycleStates.ShouldBe([
            Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Acknowledged,
            Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Syncing,
            Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Confirmed,
        ]);
    }

    [Fact]
    public async Task TestProjectionPageLoader_ConfiguredPage_ReturnsEvidenceWithoutNetwork() {
        using TestHost host = new();
        TestProjectionPageLoader loader = host.Services.GetRequiredService<TestProjectionPageLoader>();
        loader.SucceedWith(typeof(CounterProjection).FullName!, [new CounterProjection { Id = "counter-1", Count = 7 }]);

        Hexalith.FrontComposer.Shell.State.DataGridNavigation.ProjectionPageResult result =
            await loader.LoadPageAsync(
                typeof(CounterProjection).FullName!,
                0,
                20,
                System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null,
                false,
                null,
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.TotalCount.ShouldBe(1);
        loader.Evidence.Single().ProjectionTypeFqn.ShouldBe(typeof(CounterProjection).FullName);
        loader.Evidence.Single().Skip.ShouldBe(0);
        loader.Evidence.Single().Take.ShouldBe(20);
        loader.Evidence.Single().Mode.ShouldBe("configured");
    }

    [Fact]
    public async Task TestProjectionPageLoader_NotModifiedPage_CapturesModeAndReusesCachedItems() {
        using TestHost host = new();
        TestProjectionPageLoader loader = host.Services.GetRequiredService<TestProjectionPageLoader>();
        CounterProjection cached = new() { Id = "counter-cache", Count = 9 };
        loader.NotModified(typeof(CounterProjection).FullName!, [cached], etag: "\"etag-page\"");

        Hexalith.FrontComposer.Shell.State.DataGridNavigation.ProjectionPageResult result =
            await loader.LoadPageAsync(
                typeof(CounterProjection).FullName!,
                5,
                10,
                System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null,
                false,
                null,
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.IsNotModified.ShouldBeTrue();
        result.Items.Single().ShouldBeSameAs(cached);
        result.ETag.ShouldBe("\"etag-page\"");
        loader.Evidence.Single().ProjectionTypeFqn.ShouldBe(typeof(CounterProjection).FullName);
        loader.Evidence.Single().Skip.ShouldBe(5);
        loader.Evidence.Single().Take.ShouldBe(10);
        loader.Evidence.Single().Mode.ShouldBe("not-modified");
    }

    [Fact]
    public async Task QueryAndPageLoaderEvidence_MaxEvidenceRecords_IsBounded() {
        await using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options => options.MaxEvidenceRecords = 2);

        for (int i = 0; i < 5; i++) {
            _ = await host.QueryService.QueryAsync<string>(
                new QueryRequest("String", null),
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
            _ = await host.PageLoader.LoadPageAsync(
                typeof(CounterProjection).FullName!,
                i,
                20,
                System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null,
                false,
                null,
                Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        host.QueryService.Evidence.Count.ShouldBe(2);
        host.PageLoader.Evidence.Count.ShouldBe(2);
        host.PageLoader.Evidence.Select(e => e.Skip).ShouldBe([3, 4]);
    }

    [Fact]
    public async Task TestQueryService_NotModifiedAndEmptyPaths_CaptureRequestEvidence() {
        using TestHost host = new();
        TestQueryService query = host.Services.GetRequiredService<TestQueryService>();
        query.NotModifiedWith([new CounterProjection { Id = "counter-cache", Count = 4 }], "\"etag-cache\"");

        QueryResult<CounterProjection> notModified = await query.QueryAsync<CounterProjection>(
            new QueryRequest(
                typeof(CounterProjection).FullName!,
                "tenant-query",
                Skip: 10,
                Take: 5),
            Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        QueryResult<string> empty = await query.QueryAsync<string>(
            new QueryRequest("StringProjection", null, Skip: 2, Take: 3),
            Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        notModified.IsNotModified.ShouldBeTrue();
        notModified.Items.Single().Id.ShouldBe("counter-cache");
        empty.Items.ShouldBeEmpty();
        query.Evidence[0].ProjectionTypeFqn.ShouldBe(typeof(CounterProjection).FullName);
        query.Evidence[0].TenantId.ShouldBe("tenant-query");
        query.Evidence[0].Skip.ShouldBe(10);
        query.Evidence[0].Take.ShouldBe(5);
        query.Evidence[0].Mode.ShouldBe("not-modified");
        query.Evidence[1].ProjectionTypeFqn.ShouldBe("StringProjection");
        query.Evidence[1].Mode.ShouldBe("empty");
    }

    [Fact]
    public async Task QueryAndPageLoader_HonorCancellationBeforeCapturingEvidence() {
        using TestHost host = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            host.ExposedQueryService.QueryAsync<string>(new QueryRequest("StringProjection", null), cts.Token)).ConfigureAwait(true);
        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            host.ExposedPageLoader.LoadPageAsync(
                "Projection",
                0,
                10,
                System.Collections.Immutable.ImmutableDictionary<string, string>.Empty,
                null,
                false,
                null,
                cts.Token)).ConfigureAwait(true);

        host.ExposedQueryService.Evidence.ShouldBeEmpty();
        host.ExposedPageLoader.Evidence.ShouldBeEmpty();
    }

    [Fact]
    public void TestFaultInjectionProvider_AllModes_AreDeterministicTimestampedAndBounded() {
        using BunitContext context = new();
        FixedTimeProvider timeProvider = new(DateTimeOffset.Parse("2026-06-05T12:00:00Z", CultureInfo.InvariantCulture));
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(
            context,
            options => {
                options.TestTenantId = "tenant-fault";
                options.TestUserId = "user-fault";
                options.TimeProvider = timeProvider;
                options.MaxEvidenceRecords = 3;
            });

        _ = host.FaultProvider.Drop("corr-1");
        _ = host.FaultProvider.Delay("corr-2");
        _ = host.FaultProvider.PartialDelivery("corr-3");
        _ = host.FaultProvider.Reorder("corr-4");
        FaultInjectionEvidence nudge = host.FaultProvider.ReconnectNudge("corr-5");

        nudge.Mode.ShouldBe("reconnect-nudge");
        host.FaultProvider.Evidence.Select(e => e.Mode).ShouldBe(["partial-delivery", "reorder", "reconnect-nudge"]);
        host.FaultProvider.Evidence.ShouldAllBe(e => e.TenantId == "tenant-fault" && e.UserId == "user-fault");
        host.FaultProvider.Evidence.ShouldAllBe(e => e.CapturedAtUtc == timeProvider.Timestamp);
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_RedactsSensitiveValuesAndTruncatesPayload() {
        FrontComposerTestOptions options = new() {
            TestTenantId = "tenant-secret",
            TestUserId = "user-secret",
            MaxDiagnosticPayloadCharacters = 80,
        };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                TenantId = "tenant-secret",
                UserId = "user-secret",
                ApiTOKEN = "visible-token",
                NestedSecret = "visible-secret",
                AdminPassword = "visible-password",
                Extra = new string('x', 200),
            },
            options);

        payload.ShouldNotContain("tenant-secret");
        payload.ShouldNotContain("user-secret");
        payload.ShouldNotContain("visible-token");
        payload.ShouldNotContain("visible-secret");
        payload.ShouldNotContain("visible-password");
        payload.ShouldContain("...<truncated>");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_RedactsSecretValuesContainingCommas() {
        FrontComposerTestOptions options = new() { MaxDiagnosticPayloadCharacters = 256 };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                Password = "alpha,bravo,charlie",
                Token = "one,two,three",
                Amount = 7,
            },
            options);

        payload.ShouldNotContain("alpha");
        payload.ShouldNotContain("bravo");
        payload.ShouldNotContain("charlie");
        payload.ShouldNotContain("one");
        payload.ShouldNotContain("two");
        payload.ShouldNotContain("three");
        payload.ShouldContain("7");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_RedactsConfiguredTenantAndUserInsideNestedPayloads() {
        FrontComposerTestOptions options = new() {
            TestTenantId = "tenant-private",
            TestUserId = "user-private",
            MaxDiagnosticPayloadCharacters = 1024,
        };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                Context = new {
                    TenantId = "tenant-private",
                    UserId = "user-private",
                    Items = new[] {
                        new { Owner = "user-private", Tenant = "tenant-private" },
                    },
                },
                Label = "safe-assertion-context",
            },
            options);

        payload.ShouldNotContain("tenant-private");
        payload.ShouldNotContain("user-private");
        payload.ShouldContain("<tenant>");
        payload.ShouldContain("<user>");
        payload.ShouldContain("safe-assertion-context");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_RedactsConfiguredTenantAndUserInPropertyNames() {
        FrontComposerTestOptions options = new() {
            TestTenantId = "tenant-keyed",
            TestUserId = "user-keyed",
            MaxDiagnosticPayloadCharacters = 1024,
        };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                ByTenant = new Dictionary<string, string> {
                    ["tenant-keyed"] = "tenant-scoped-value",
                },
                ByUser = new Dictionary<string, int> {
                    ["user-keyed"] = 5,
                },
                Label = "keyed-assertion-context",
            },
            options);

        payload.ShouldNotContain("tenant-keyed");
        payload.ShouldNotContain("user-keyed");
        payload.ShouldContain("<tenant>");
        payload.ShouldContain("<user>");
        payload.ShouldContain("keyed-assertion-context");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_RedactsSecretKeysCaseInsensitivelyAcrossValueShapes() {
        FrontComposerTestOptions options = new() { MaxDiagnosticPayloadCharacters = 2048 };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                ApiTOKEN = "string-token-value",
                nestedSecret = 12345,
                AdminPASSWORD = true,
                NullPassword = (string?)null,
                TokenObject = new {
                    First = "object-secret-a",
                    Second = "object-secret-b",
                },
                SecretArray = new object?[] { "array-secret-a", 99, false, null },
                VisibleAmount = 77,
            },
            options);

        payload.ShouldNotContain("string-token-value");
        payload.ShouldNotContain("12345");
        payload.ShouldNotContain("true");
        payload.ShouldNotContain("object-secret-a");
        payload.ShouldNotContain("object-secret-b");
        payload.ShouldNotContain("array-secret-a");
        payload.ShouldContain("77");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_RedactsPunctuationHeavySecretStringsCompletely() {
        FrontComposerTestOptions options = new() { MaxDiagnosticPayloadCharacters = 2048 };
        string secret = "alpha,beta{gamma}[delta]\"quoted\\path\":value=semi;url=https://example.test/path?token=a:b&secret=c";

        string payload = RedactedEvidenceFormatter.Format(
            new {
                Token = secret,
                ApiSecret = "prefix," + secret,
                Password = "before:" + secret + ";after",
                Safe = "punctuation-safe-value",
            },
            options);

        payload.ShouldNotContain(secret);
        payload.ShouldNotContain("alpha");
        payload.ShouldNotContain("quoted");
        payload.ShouldNotContain("example.test");
        payload.ShouldContain("punctuation-safe-value");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_TruncatesOversizedPayloadAfterRedaction() {
        FrontComposerTestOptions options = new() {
            TestTenantId = "tenant-oversized",
            TestUserId = "user-oversized",
            MaxDiagnosticPayloadCharacters = 96,
        };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                TenantId = "tenant-oversized",
                UserId = "user-oversized",
                Token = "oversized-token",
                Secret = "oversized-secret",
                Password = "oversized-password",
                Description = new string('x', 400),
            },
            options);

        payload.ShouldContain("...<truncated>");
        payload.ShouldNotContain("tenant-oversized");
        payload.ShouldNotContain("user-oversized");
        payload.ShouldNotContain("oversized-token");
        payload.ShouldNotContain("oversized-secret");
        payload.ShouldNotContain("oversized-password");
    }

    [Fact]
    public void RedactedEvidenceFormatter_Format_PreservesBenignAssertionValues() {
        FrontComposerTestOptions options = new() { MaxDiagnosticPayloadCharacters = 512 };

        string payload = RedactedEvidenceFormatter.Format(
            new {
                ProjectionType = "Counter",
                Count = 12,
                Status = "Accepted",
                IsFresh = true,
            },
            options);

        payload.ShouldContain("Counter");
        payload.ShouldContain("12");
        payload.ShouldContain("Accepted");
        payload.ShouldContain("true");
    }

    [Fact]
    public async Task TestCommandService_Dispatch_RedactsCommandPayloadEvidenceThroughFormatter() {
        using TestHost host = new("tenant-command", "user-command");
        ICommandService commandService = host.Services.GetRequiredService<ICommandService>();
        RedactionMatrixCommand command = new() {
            Tenant = "tenant-command",
            User = "user-command",
            Token = "command-token",
            SecretDetails = new RedactionSecretDetails("nested-secret-a", "nested-secret-b"),
            PasswordHistory = ["password-one", "password-two"],
            Name = "safe-command-name",
        };

        _ = await commandService.DispatchAsync(command, Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        CommandDispatchEvidence evidence = host.ExposedCommandService.Evidence.Single();
        evidence.RedactedPayload.ShouldNotContain("tenant-command");
        evidence.RedactedPayload.ShouldNotContain("user-command");
        evidence.RedactedPayload.ShouldNotContain("command-token");
        evidence.RedactedPayload.ShouldNotContain("nested-secret-a");
        evidence.RedactedPayload.ShouldNotContain("nested-secret-b");
        evidence.RedactedPayload.ShouldNotContain("password-one");
        evidence.RedactedPayload.ShouldNotContain("password-two");
        evidence.RedactedPayload.ShouldContain("safe-command-name");
    }

    [Fact]
    public void FrontComposerTestHost_Dispose_RestoresAppliedCulture() {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUICulture = CultureInfo.CurrentUICulture;
        try {
            using BunitContext context = new();
            using (context.Services.AddFrontComposerTestHost(
                context,
                options => options.Culture = CultureInfo.GetCultureInfo("ja-JP"))) {
                CultureInfo.CurrentCulture.Name.ShouldBe("ja-JP");
                CultureInfo.CurrentUICulture.Name.ShouldBe("ja-JP");
            }

            CultureInfo.CurrentCulture.ShouldBe(originalCulture);
            CultureInfo.CurrentUICulture.ShouldBe(originalUICulture);
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Fact]
    public async Task ParallelContexts_DifferentTenants_DoNotShareFakeEvidence() {
        static async Task<CommandDispatchEvidence> DispatchAsync(string tenant, string user, CancellationToken cancellationToken) {
            using TestHost host = new(tenant, user);
            ICommandService service = host.Services.GetRequiredService<ICommandService>();
            _ = await service.DispatchAsync(new SensitiveCommand { Tenant = tenant, User = user }, cancellationToken)
                .ConfigureAwait(true);
            return host.ExposedCommandService.Evidence.Single();
        }

        CommandDispatchEvidence[] evidence = await Task.WhenAll(
            DispatchAsync("tenant-1", "user-1", Xunit.TestContext.Current.CancellationToken),
            DispatchAsync("tenant-2", "user-2", Xunit.TestContext.Current.CancellationToken)).ConfigureAwait(true);

        evidence[0].TenantId.ShouldBe("tenant-1");
        evidence[1].TenantId.ShouldBe("tenant-2");
        evidence[0].MessageId.ShouldBe(evidence[1].MessageId);
    }

    [Fact]
    public async Task CounterProjectionView_WithCompositionHost_RendersDataGridAndViewOverride() {
        await using BunitContext context = new();
        FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        _ = host.AddDomainAssembly<CounterDomain>();
        _ = context.Services.AddViewOverride<CounterProjection, CounterFullViewReplacement>();

        IStore store = context.Services.GetRequiredService<IStore>();
        await store.InitializeAsync();
        IDispatcher dispatcher = context.Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [new CounterProjection { Id = "counter-1", Count = 12, LastUpdated = DateTimeOffset.Parse("2026-04-14T00:00:00Z") }]));

        IRenderedComponent<CounterProjectionView> cut = context.Render<CounterProjectionView>();

        cut.WaitForAssertion(() => {
            GeneratedProjectionAssertions.AssertDataGridEnvelope(cut, "Counter");
            GeneratedProjectionAssertions.AssertCellValues(cut, "counter-full-view-heading", "12");
        });
    }

    [Fact]
    public async Task CounterIncrementCommandRenderer_WithCompositionHost_DispatchesThroughFakeService() {
        await using BunitContext context = new();
        using FrontComposerTestHostBuilder host = context.Services.AddFrontComposerTestHost(context);
        _ = host.AddDomainAssembly<CounterDomain>();
        IStore store = context.Services.GetRequiredService<IStore>();
        await store.InitializeAsync().ConfigureAwait(true);

        IRenderedComponent<IncrementCommandRenderer> cut = context.Render<IncrementCommandRenderer>();
        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => _ = cut.Find("form"));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => {
            CommandDispatchEvidence evidence = host.CommandService.Evidence.Single();
            evidence.CommandType.ShouldBe(typeof(IncrementCommand).FullName);
            evidence.Status.ShouldBe("Accepted");
        });
    }

    private sealed class TestHost : FrontComposerTestBase {
        public TestHost()
            : this("tenant-a", "user-a") {
        }

        public TestHost(string tenant, string user)
            : base(options => {
                options.TestTenantId = tenant;
                options.TestUserId = user;
            }) {
        }

        public TestCommandService ExposedCommandService => CommandService;

        public TestQueryService ExposedQueryService => QueryService;

        public TestProjectionPageLoader ExposedPageLoader => PageLoader;

        public Task InitializeAsync() => InitializeStoreAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider {
        public DateTimeOffset Timestamp { get; } = timestamp;

        public override DateTimeOffset GetUtcNow() => Timestamp;
    }

    private sealed class SensitiveCommand {
        public string? Tenant { get; init; }

        public string? User { get; init; }

        public string? Token { get; init; }

        public int Amount { get; init; }
    }

    private sealed class RedactionMatrixCommand {
        public string? Tenant { get; init; }

        public string? User { get; init; }

        public string? Token { get; init; }

        public RedactionSecretDetails? SecretDetails { get; init; }

        public IReadOnlyList<string> PasswordHistory { get; init; } = [];

        public string? Name { get; init; }
    }

    private sealed record RedactionSecretDetails(string Primary, string Secondary);
}
