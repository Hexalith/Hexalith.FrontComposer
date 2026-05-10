using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.ETagCache;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using PactNet;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Pact;

#pragma warning disable CA2007 // xUnit v3 test continuations intentionally resume on the test context.

public sealed class EventStorePactContractTests {
    private const string ConsumerName = "Hexalith.FrontComposer.Shell";
    private const string ProviderName = "Hexalith.EventStore";
    private const string SyntheticBearerToken = "FC_CONTRACT_TOKEN";
    private const string SyntheticTenant = "tenant-contract-a";
    private const string SyntheticUser = "user-contract-a";
    private const string SyntheticMessageId = "01HXCNTRCT0000000000000000";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    [Trait("Category", "Contract")]
    public async Task ConsumerPactArtifacts_AreGeneratedFromEventStoreAdapters() {
        string pactDirectory = PactDirectory();

        List<ContractInteraction> commandInteractions = [
            await BuildCommandAcceptedInteraction(),
            await BuildCommandFailureInteraction(
                "command validation failure is classified",
                "command-validation-failure",
                HttpStatusCode.BadRequest,
                "CommandValidationException"),
            await BuildCommandFailureInteraction(
                "command auth failure requires redirect",
                "command-unauthorized",
                HttpStatusCode.Unauthorized,
                "AuthRedirectRequiredException",
                includeProblemDetails: false),
            await BuildCommandFailureInteraction(
                "command forbidden warning is classified",
                "command-forbidden",
                HttpStatusCode.Forbidden,
                "CommandWarningException"),
            await BuildCommandFailureInteraction(
                "command missing aggregate warning is classified",
                "command-not-found",
                HttpStatusCode.NotFound,
                "CommandWarningException"),
            await BuildCommandFailureInteraction(
                "command conflict rejection is classified",
                "command-conflict",
                HttpStatusCode.Conflict,
                "CommandRejectedException"),
            await BuildCommandFailureInteraction(
                "command rate limit warning is classified",
                "command-rate-limited",
                (HttpStatusCode)429,
                "CommandWarningException"),
            await BuildCommandFailureInteraction(
                "command unexpected server failure is classified",
                "command-unexpected-5xx",
                HttpStatusCode.InternalServerError,
                "HttpRequestException",
                includeProblemDetails: false),
        ];

        List<ContractInteraction> queryInteractions = [
            await BuildQueryOkInteraction("query fresh projection data is classified", "query-fresh-data", payload: """{"payload":[{"id":"order-1","status":"Pending"}],"totalCount":1}"""),
            await BuildQueryOkInteraction("query empty projection data is classified", "query-empty-result", payload: """{"payload":[],"totalCount":0}"""),
            await BuildQueryFailureInteraction("query malformed payload failure is classified", "query-malformed-payload", HttpStatusCode.BadRequest, "HttpRequestException"),
            await BuildQueryFailureInteraction("query forbidden failure is classified", "query-forbidden", HttpStatusCode.Forbidden, "QueryFailureException"),
            await BuildQueryFailureInteraction("query missing projection failure is classified", "query-not-found", HttpStatusCode.NotFound, "QueryFailureException"),
            await BuildQueryFailureInteraction("query rate limit failure is classified", "query-rate-limited", (HttpStatusCode)429, "QueryFailureException"),
        ];

        List<ContractInteraction> cacheInteractions = [
            await BuildQueryCachedNotModifiedInteraction(),
            await BuildQueryCallerOwnedNotModifiedInteraction(),
            await BuildQueryMultipleValidatorsInteraction(),
        ];

        List<ContractInteraction> authTenantInteractions = [
            await BuildCommandAuthTenantPropagationInteraction(),
            await BuildQueryAuthTenantPropagationInteraction(),
        ];

        WritePact(pactDirectory, "frontcomposer-eventstore-command-dispatch.json", commandInteractions);
        WritePact(pactDirectory, "frontcomposer-eventstore-query-execution.json", queryInteractions);
        WritePact(pactDirectory, "frontcomposer-eventstore-cache-validation.json", cacheInteractions);
        WritePact(pactDirectory, "frontcomposer-eventstore-auth-tenant-propagation.json", authTenantInteractions);

        List<ContractInteraction> allInteractions = [
            .. commandInteractions,
            .. queryInteractions,
            .. cacheInteractions,
            .. authTenantInteractions,
        ];
        WriteManifest(pactDirectory, allInteractions);
        WriteProviderStateCatalog(pactDirectory);
        WriteProviderVerificationHandoff(pactDirectory, allInteractions.Count);

        allInteractions.Count.ShouldBeGreaterThan(0);
        allInteractions.Select(i => i.Description).Distinct(StringComparer.Ordinal).Count().ShouldBe(allInteractions.Count);
        ScanDirectoryForRedactionLeaks(pactDirectory);
    }

    [Fact]
    [Trait("Category", "Contract")]
    public async Task ETagContractGuards_RejectUnsafeValidatorsBeforeSend() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        IQueryService sut = NewQueryClient(handler, new NoCache());

        QueryRequest tooMany = QueryRequest(ETags: Enumerable.Range(0, 11).Select(i => $"\"etag-{i}\"").ToArray());
        _ = await Should.ThrowAsync<ArgumentException>(() => sut.QueryAsync<OrderProjection>(tooMany, TestContext.Current.CancellationToken));

        QueryRequest injected = QueryRequest(ETags: ["\"etag-1\"\r\nInjected: value"]);
        _ = await Should.ThrowAsync<ArgumentException>(() => sut.QueryAsync<OrderProjection>(injected, TestContext.Current.CancellationToken));

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void RedactionScanner_RejectsAdversarialContractLeaks() {
        string[] rejected = [
            """{"headers":{"Authorization":"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.sig"}}""",
            """{"cookie":"sessionid=abc123"}""",
            """{"connection":"Server=tcp:prod.database.windows.net;Password=s3cret!"}""",
            """{"path":"C:\\Users\\Jerome\\AppData\\Local\\Temp\\pact.log"}""",
            """{"url":"/api/v1/queries?access_token=abc123"}""",
            """{"detail":"BEGIN_ENV AZURE_CLIENT_SECRET=abc END_ENV"}""",
            """{"payload":"ZXlKaGJHY2lPaUpJVXpJMU5pSjkuZXlKMGVYQWlPaUpLVjFRaUxDSmhiR2NpT2lKSVV6STFOaUo5"}""",
        ];

        foreach (string candidate in rejected) {
            RedactionScanner.FindLeaks(candidate).ShouldNotBeEmpty(candidate);
        }

        string allowlisted = $$"""{"headers":{"Authorization":"Bearer {{SyntheticBearerToken}}"},"tenant":"{{SyntheticTenant}}","userId":"{{SyntheticUser}}"}""";
        RedactionScanner.FindLeaks(allowlisted).ShouldBeEmpty();
    }

    private static async Task<ContractInteraction> BuildCommandAcceptedInteraction() {
        RecordingHandler handler = new(_ => Response(
            HttpStatusCode.Accepted,
            """{"correlationId":"corr-command-accepted"}""",
            headers => {
                headers.Location = new Uri("https://eventstore.test/api/v1/commands/status/corr-command-accepted");
                headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
                headers.Add("X-Correlation-ID", "corr-command-accepted");
            }));
        ICommandService sut = NewCommandClient(handler);

        CommandResult result = await sut.DispatchAsync(Command(), TestContext.Current.CancellationToken);

        result.MessageId.ShouldBe(SyntheticMessageId);
        result.Status.ShouldBe("Accepted");
        result.CorrelationId.ShouldBe("corr-command-accepted");
        result.Location!.AbsoluteUri.ShouldBe("https://eventstore.test/api/v1/commands/status/corr-command-accepted");
        result.RetryAfter.ShouldBe(TimeSpan.FromSeconds(2));

        ContractInteraction interaction = Interaction(
            "command dispatch accepted preserves generated message identity",
            "command-accepted",
            handler.Requests.Single(),
            new ContractHttpResponse(
                202,
                Headers([
                    ("Location", "https://eventstore.test/api/v1/commands/status/corr-command-accepted"),
                    ("Retry-After", "2"),
                    ("X-Correlation-ID", "corr-command-accepted"),
                    ("Content-Type", "application/json"),
                ]),
                Json("""{"correlationId":"corr-command-accepted"}""")),
            "CommandResult.Status=Accepted; CommandResult.MessageId preserved");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                ICommandService mockSut = NewCommandClient(mockServerUri);
                CommandResult mockResult = await mockSut.DispatchAsync(Command(), TestContext.Current.CancellationToken);
                mockResult.MessageId.ShouldBe(SyntheticMessageId);
                mockResult.Status.ShouldBe("Accepted");
                mockResult.CorrelationId.ShouldBe("corr-command-accepted");
                mockResult.Location!.AbsoluteUri.ShouldBe("https://eventstore.test/api/v1/commands/status/corr-command-accepted");
                mockResult.RetryAfter.ShouldBe(TimeSpan.FromSeconds(2));
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildCommandFailureInteraction(
        string description,
        string providerState,
        HttpStatusCode status,
        string classifierExpectation,
        bool includeProblemDetails = true) {
        string? body = includeProblemDetails ? ProblemDetails((int)status, providerState) : null;
        RecordingHandler handler = new(_ => Response(
            status,
            body,
            headers => {
                if ((int)status == 429) {
                    headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
                }
            }));
        ICommandService sut = NewCommandClient(handler);

        Exception ex = await Should.ThrowAsync<Exception>(() => sut.DispatchAsync(Command(), TestContext.Current.CancellationToken));
        ex.GetType().Name.ShouldBe(classifierExpectation);

        ContractInteraction interaction = Interaction(
            description,
            providerState,
            handler.Requests.Single(),
            new ContractHttpResponse(
                (int)status,
                ResponseHeaders(status, includeProblemDetails),
                body is null ? null : Json(body)),
            classifierExpectation);

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                ICommandService mockSut = NewCommandClient(mockServerUri);
                Exception mockEx = await Should.ThrowAsync<Exception>(() => mockSut.DispatchAsync(Command(), TestContext.Current.CancellationToken));
                mockEx.GetType().Name.ShouldBe(classifierExpectation);
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildQueryOkInteraction(string description, string providerState, string payload) {
        RecordingHandler handler = new(_ => Response(
            HttpStatusCode.OK,
            payload,
            headers => headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag-query-1\"")));
        IQueryService sut = NewQueryClient(handler, new NoCache());

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(QueryRequest(), TestContext.Current.CancellationToken);

        result.ETag.ShouldBe("\"etag-query-1\"");

        ContractInteraction interaction = Interaction(
            description,
            providerState,
            handler.Requests.Single(),
            new ContractHttpResponse(
                200,
                Headers([("ETag", "\"etag-query-1\""), ("Content-Type", "application/json")]),
                Json(payload)),
            "QueryResult<T>.IsNotModified=false; QueryResult<T>.TotalCount preserved");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                IQueryService mockSut = NewQueryClient(mockServerUri, new NoCache());
                QueryResult<OrderProjection> mockResult = await mockSut.QueryAsync<OrderProjection>(QueryRequest(), TestContext.Current.CancellationToken);
                mockResult.ETag.ShouldBe("\"etag-query-1\"");
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildQueryFailureInteraction(
        string description,
        string providerState,
        HttpStatusCode status,
        string classifierExpectation) {
        string body = ProblemDetails((int)status, providerState);
        RecordingHandler handler = new(_ => Response(status, body));
        IQueryService sut = NewQueryClient(handler, new NoCache());

        Exception ex = await Should.ThrowAsync<Exception>(() => sut.QueryAsync<OrderProjection>(QueryRequest(), TestContext.Current.CancellationToken));
        ex.GetType().Name.ShouldBe(classifierExpectation);

        ContractInteraction interaction = Interaction(
            description,
            providerState,
            handler.Requests.Single(),
            new ContractHttpResponse((int)status, ResponseHeaders(status, includeProblemDetails: true), Json(body)),
            classifierExpectation);

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                IQueryService mockSut = NewQueryClient(mockServerUri, new NoCache());
                Exception mockEx = await Should.ThrowAsync<Exception>(() => mockSut.QueryAsync<OrderProjection>(QueryRequest(), TestContext.Current.CancellationToken));
                mockEx.GetType().Name.ShouldBe(classifierExpectation);
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildQueryCachedNotModifiedInteraction() {
        string cachedPayload = """{"payload":[{"id":"order-1","status":"Cached"}],"totalCount":1}""";
        RecordingHandler handler = new(_ => Response(
            HttpStatusCode.NotModified,
            body: null,
            headers => headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag-cache-1\"")));
        IQueryService sut = NewQueryClient(
            handler,
            new SeededCache("\"etag-cache-1\"", cachedPayload));

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            QueryRequest(CacheDiscriminator: "orders-grid"),
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeTrue();
        result.Items.Single().Status.ShouldBe("Cached");
        handler.Requests.Single().Headers["If-None-Match"].ShouldBe("\"etag-cache-1\"");

        ContractInteraction interaction = Interaction(
            "query cache validation reuses framework cache on 304",
            "query-etag-match",
            handler.Requests.Single(),
            new ContractHttpResponse(304, Headers([("ETag", "\"etag-cache-1\"")]), null),
            "QueryResult<T>.NotModifiedFromCache");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                IQueryService mockSut = NewQueryClient(
                    mockServerUri,
                    new SeededCache("\"etag-cache-1\"", cachedPayload));
                QueryResult<OrderProjection> mockResult = await mockSut.QueryAsync<OrderProjection>(
                    QueryRequest(CacheDiscriminator: "orders-grid"),
                    TestContext.Current.CancellationToken);
                mockResult.IsNotModified.ShouldBeTrue();
                mockResult.Items.Single().Status.ShouldBe("Cached");
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildQueryCallerOwnedNotModifiedInteraction() {
        RecordingHandler handler = new(_ => Response(
            HttpStatusCode.NotModified,
            body: null,
            headers => headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag-caller-1\"")));
        IQueryService sut = NewQueryClient(handler, new NoCache());

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            QueryRequest(ETag: "\"etag-caller-1\""),
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeTrue();
        result.Items.ShouldBeEmpty();
        handler.Requests.Single().Headers["If-None-Match"].ShouldBe("\"etag-caller-1\"");

        ContractInteraction interaction = Interaction(
            "query caller-owned etag returns explicit no-change signal on 304",
            "query-etag-no-cache",
            handler.Requests.Single(),
            new ContractHttpResponse(304, Headers([("ETag", "\"etag-caller-1\"")]), null),
            "QueryResult<T>.NotModified");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                IQueryService mockSut = NewQueryClient(mockServerUri, new NoCache());
                QueryResult<OrderProjection> mockResult = await mockSut.QueryAsync<OrderProjection>(
                    QueryRequest(ETag: "\"etag-caller-1\""),
                    TestContext.Current.CancellationToken);
                mockResult.IsNotModified.ShouldBeTrue();
                mockResult.Items.ShouldBeEmpty();
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildQueryMultipleValidatorsInteraction() {
        RecordingHandler handler = new(_ => Response(HttpStatusCode.OK, """{"payload":[{"id":"order-1","status":"Pending"}],"totalCount":1}"""));
        IQueryService sut = NewQueryClient(handler, new NoCache());

        _ = await sut.QueryAsync<OrderProjection>(
            QueryRequest(ETags: ["\"etag-1\"", "\"etag-2\"", "\"etag-3\""]),
            TestContext.Current.CancellationToken);

        handler.Requests.Single().Headers["If-None-Match"].ShouldBe("\"etag-1\", \"etag-2\", \"etag-3\"");

        ContractInteraction interaction = Interaction(
            "query emits bounded multiple etag validators",
            "query-large-valid-metadata",
            handler.Requests.Single(),
            new ContractHttpResponse(200, Headers([("Content-Type", "application/json")]), Json("""{"payload":[{"id":"order-1","status":"Pending"}],"totalCount":1}""")),
            "If-None-Match validator count <= EventStoreOptions.MaxETagCount");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                IQueryService mockSut = NewQueryClient(mockServerUri, new NoCache());
                _ = await mockSut.QueryAsync<OrderProjection>(
                    QueryRequest(ETags: ["\"etag-1\"", "\"etag-2\"", "\"etag-3\""]),
                    TestContext.Current.CancellationToken);
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildCommandAuthTenantPropagationInteraction() {
        RecordingHandler handler = new(_ => Response(HttpStatusCode.Accepted, """{"correlationId":"corr-auth-tenant"}"""));
        ICommandService sut = NewCommandClient(handler, tenant: "Tenant_Contract_Case");

        _ = await sut.DispatchAsync(Command(tenantId: "Tenant_Contract_Case"), TestContext.Current.CancellationToken);

        CapturedRequest request = handler.Requests.Single();
        request.Headers["Authorization"].ShouldBe($"Bearer {SyntheticBearerToken}");
        JsonElement body = request.Body!.Value;
        body.GetProperty("tenant").GetString().ShouldBe("Tenant_Contract_Case");

        ContractInteraction interaction = Interaction(
            "command propagates authenticated tenant and bearer requirement",
            "tenant-mismatch",
            request,
            new ContractHttpResponse(202, Headers([("Content-Type", "application/json")]), Json("""{"correlationId":"corr-auth-tenant"}""")),
            "Authorization required; tenant comes from authenticated context");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                ICommandService mockSut = NewCommandClient(mockServerUri, tenant: "Tenant_Contract_Case");
                _ = await mockSut.DispatchAsync(Command(tenantId: "Tenant_Contract_Case"), TestContext.Current.CancellationToken);
            });

        return interaction;
    }

    private static async Task<ContractInteraction> BuildQueryAuthTenantPropagationInteraction() {
        RecordingHandler handler = new(_ => Response(HttpStatusCode.OK, """{"payload":[{"id":"order-1","status":"Pending"}],"totalCount":1}"""));
        IQueryService sut = NewQueryClient(handler, new NoCache(), tenant: "Tenant_Contract_Case");

        _ = await sut.QueryAsync<OrderProjection>(
            QueryRequest(TenantId: "Tenant_Contract_Case"),
            TestContext.Current.CancellationToken);

        CapturedRequest request = handler.Requests.Single();
        request.Headers["Authorization"].ShouldBe($"Bearer {SyntheticBearerToken}");
        request.Body!.Value.GetProperty("tenant").GetString().ShouldBe("Tenant_Contract_Case");

        ContractInteraction interaction = Interaction(
            "query propagates authenticated tenant and bearer requirement",
            "query-auth-tenant",
            request,
            new ContractHttpResponse(200, Headers([("Content-Type", "application/json")]), Json("""{"payload":[{"id":"order-1","status":"Pending"}],"totalCount":1}""")),
            "Authorization required; tenant comes from authenticated context");

        await VerifyPactNetInteractionAsync(
            interaction,
            async mockServerUri => {
                IQueryService mockSut = NewQueryClient(mockServerUri, new NoCache(), tenant: "Tenant_Contract_Case");
                _ = await mockSut.QueryAsync<OrderProjection>(
                    QueryRequest(TenantId: "Tenant_Contract_Case"),
                    TestContext.Current.CancellationToken);
            });

        return interaction;
    }

    private static async Task VerifyPactNetInteractionAsync(ContractInteraction interaction, Func<Uri, Task> exerciseAsync) {
        string scratchDir = Path.Combine(Path.GetTempPath(), "frontcomposer-pactnet", Guid.NewGuid().ToString("N"));
        try {
            IPactBuilderV4 builder = global::PactNet.Pact
                .V4(ConsumerName, ProviderName, new PactConfig {
                    PactDir = scratchDir,
                    DefaultJsonSettings = JsonOptions,
                })
                .WithHttpInteractions();

            IRequestBuilderV4 request = builder
                .UponReceiving(interaction.Description)
                .Given(interaction.ProviderStates.Single().Name)
                .WithRequest(new HttpMethod(interaction.Request.Method), interaction.Request.Path);

            foreach (KeyValuePair<string, string> header in interaction.Request.Headers) {
                request = request.WithHeader(header.Key, header.Value);
            }

            if (interaction.Request.Body is { } requestBody) {
                request = request.WithJsonBody(requestBody, JsonOptions);
            }

            IResponseBuilderV4 response = request.WillRespond().WithStatus((ushort)interaction.Response.Status);
            foreach (KeyValuePair<string, string> header in interaction.Response.Headers) {
                response = response.WithHeader(header.Key, header.Value);
            }

            if (interaction.Response.Body is { } responseBody) {
                response = response.WithJsonBody(responseBody, JsonOptions);
            }

            await builder.VerifyAsync(ctx => exerciseAsync(ctx.MockServerUri));
        }
        finally {
            TryDeleteDirectory(scratchDir);
        }
    }

    private static void TryDeleteDirectory(string path) {
        try {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException) {
        }
        catch (UnauthorizedAccessException) {
        }
    }

    private static ContractInteraction Interaction(
        string description,
        string providerState,
        CapturedRequest request,
        ContractHttpResponse response,
        string classifierExpectation)
        => new(
            Type: "Synchronous/HTTP",
            Description: description,
            ProviderStates: [new ContractProviderState(providerState)],
            Request: new ContractHttpRequest(
                Method: request.Method,
                Path: request.Path,
                Headers: request.Headers,
                Body: request.Body),
            Response: response,
            Metadata: new ContractInteractionMetadata(
                GeneratedSource: request.GeneratedSource,
                AdapterPath: request.AdapterPath,
                OwningAcceptanceCriteria: request.OwningAcceptanceCriteria,
                ClassifierExpectation: classifierExpectation));

    private static void WritePact(string pactDirectory, string fileName, IReadOnlyList<ContractInteraction> interactions) {
        PactDocument pact = new(
            Consumer: new ContractParty(ConsumerName),
            Provider: new ContractParty(ProviderName),
            Interactions: interactions,
            Metadata: new ContractMetadata(
                PactSpecification: new ContractSpecification("4.0"),
                PactNet: new ContractTool("5.0.1"),
                FrontComposerStory: "10-3-consumer-driven-contract-tests-pact",
                GeneratedBy: "EventStorePactContractTests"));

        WriteJson(Path.Combine(pactDirectory, fileName), pact);
    }

    private static void WriteManifest(string pactDirectory, IReadOnlyList<ContractInteraction> interactions) {
        var manifest = new {
            story = "10-3-consumer-driven-contract-tests-pact",
            consumer = ConsumerName,
            provider = ProviderName,
            pactFiles = new[] {
                "frontcomposer-eventstore-command-dispatch.json",
                "frontcomposer-eventstore-query-execution.json",
                "frontcomposer-eventstore-cache-validation.json",
                "frontcomposer-eventstore-auth-tenant-propagation.json",
            },
            interactionCount = interactions.Count,
            interactions = interactions
                .OrderBy(i => i.Description, StringComparer.Ordinal)
                .Select(i => new {
                    i.Description,
                    providerState = i.ProviderStates.Single().Name,
                    method = i.Request.Method,
                    path = i.Request.Path,
                    i.Metadata.GeneratedSource,
                    i.Metadata.AdapterPath,
                    i.Metadata.OwningAcceptanceCriteria,
                    i.Metadata.ClassifierExpectation,
                })
                .ToArray(),
        };

        WriteJson(Path.Combine(pactDirectory, "interaction-manifest.json"), manifest);
    }

    private static void WriteProviderStateCatalog(string pactDirectory) {
        var states = new[] {
            State("command-accepted", "Seed tenant-contract-a, user-contract-a, order-1, and accept ShipOrderCommand.", "Clear seeded command inbox and status resource for the run.", "202 Accepted; CommandResult.Status Accepted", isolated: true),
            State("command-validation-failure", "Reject ShipOrderCommand with bounded validation ProblemDetails.", "No persisted state; reset validation fixture.", "400 validation ProblemDetails", isolated: true),
            State("command-unauthorized", "Run without an accepted bearer context.", "No persisted state.", "401 auth redirect classification", isolated: true),
            State("command-forbidden", "Seed tenant-contract-a but deny the command policy/resource.", "Clear authorization fixture.", "403 command warning", isolated: true),
            State("command-not-found", "Seed tenant-contract-a with missing aggregate order-missing.", "No persisted aggregate state.", "404 command warning", isolated: true),
            State("command-conflict", "Seed order-1 in a conflicting version/state.", "Clear seeded aggregate version.", "409 command rejection", isolated: true),
            State("command-rate-limited", "Seed per-tenant throttle bucket above limit.", "Clear throttle bucket.", "429 command warning with Retry-After", isolated: true),
            State("command-unexpected-5xx", "Force provider test seam to return bounded synthetic server failure.", "Reset failure injection flag.", "5xx HttpRequestException", isolated: true),
            State("tenant-mismatch", "Seed authenticated tenant Tenant_Contract_Case and reject cross-tenant access.", "Clear tenant fixture.", "tenant value remains authenticated context", isolated: true),
            State("query-fresh-data", "Seed projection row order-1 for tenant-contract-a.", "Clear projection row and ETag cache.", "200 query result with ETag", isolated: true),
            State("query-empty-result", "Seed tenant-contract-a with no projection rows.", "Clear projection rows.", "200 empty result", isolated: true),
            State("query-malformed-payload", "Reject malformed query request with bounded ProblemDetails.", "No persisted state.", "400 query failure", isolated: true),
            State("query-forbidden", "Seed tenant-contract-a but deny projection read.", "Clear authorization fixture.", "403 query failure", isolated: true),
            State("query-not-found", "Seed tenant-contract-a without requested projection.", "No persisted projection state.", "404 query failure", isolated: true),
            State("query-rate-limited", "Seed per-tenant query throttle bucket above limit.", "Clear throttle bucket.", "429 query failure with Retry-After", isolated: true),
            State("query-etag-match", "Seed order-1 projection and matching ETag cache validator.", "Clear projection row and cache validator.", "304 Not Modified", isolated: true),
            State("query-etag-no-cache", "Seed matching provider ETag but require caller-owned cache handling.", "Clear validator fixture.", "304 explicit no-change", isolated: true),
            State("query-large-valid-metadata", "Seed valid multi-validator metadata below configured max.", "Clear metadata fixture.", "200 OK; validators accepted", isolated: true),
            State("query-auth-tenant", "Seed authenticated tenant Tenant_Contract_Case for query adapter.", "Clear tenant fixture.", "Authorization required; tenant preserved", isolated: true),
        };

        WriteJson(Path.Combine(pactDirectory, "provider-state-catalog.json"), new {
            provider = ProviderName,
            defaultIsolation = "state reset per interaction; tenant/user/aggregate/cache data scoped by verification run id",
            forbiddenDependencies = new[] { "DAPR", "Aspire", "Keycloak", "external network", "persisted shared state" },
            startupGuards = new[] { "unique loopback port", "health probe", "bounded startup timeout", "stale process detection", "process cleanup on failure" },
            states,
        });
    }

    private static object State(string name, string setup, string teardown, string expectedResult, bool isolated)
        => new {
            name,
            setup,
            teardown,
            seededTenant = SyntheticTenant,
            seededUser = SyntheticUser,
            seededAggregateId = "order-1",
            expectedResult,
            isolatedPerInteraction = isolated,
            owningRepository = "Hexalith.EventStore",
            testOnlySeam = "Provider-state HTTP endpoint or fixture command in the EventStore submodule",
        };

    private static void WriteProviderVerificationHandoff(string pactDirectory, int interactionCount) {
        string text = $"""
        # EventStore Provider Verification Handoff

        Story: 10-3-consumer-driven-contract-tests-pact
        Consumer: {ConsumerName}
        Provider: {ProviderName}
        Interaction count: {interactionCount}
        Release status: blocked until provider verification runs against the pinned EventStore provider version.

        Provider verification must run in `Hexalith.EventStore` against a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback, because the native verifier calls an HTTP endpoint.

        Required command shape:

        ```powershell
        dotnet test Hexalith.EventStore.sln --configuration Release --filter "Category=ContractProvider" -- `
          --pact-source "{Path.Combine("..", "tests", "Hexalith.FrontComposer.Shell.Tests", "Pact")}" `
          --provider-state-catalog "{Path.Combine("..", "tests", "Hexalith.FrontComposer.Shell.Tests", "Pact", "provider-state-catalog.json")}" `
          --report-output "artifacts/contracts/provider-verification.json"
        ```

        Required pact path: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json`
        Required manifest: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
        Required provider-state catalog: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`

        Blocking reason in this repository: the current FrontComposer repo can generate consumer pacts and validate artifacts, but deterministic provider states must be owned by the EventStore HTTP pipeline/test host so setup, teardown, health probing, port allocation, and stale-process detection are verified beside the provider.
        """;

        File.WriteAllText(Path.Combine(pactDirectory, "provider-verification-handoff.md"), text, Encoding.UTF8);
    }

    private static ICommandService NewCommandClient(RecordingHandler handler, string tenant = SyntheticTenant)
        => new EventStoreCommandClient(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor(tenant, SyntheticUser),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

    private static ICommandService NewCommandClient(Uri baseAddress, string tenant = SyntheticTenant)
        => new EventStoreCommandClient(
            new BaseAddressClientFactory(baseAddress),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor(tenant, SyntheticUser),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

    private static IQueryService NewQueryClient(RecordingHandler handler, IETagCache cache, string tenant = SyntheticTenant)
        => new EventStoreQueryClient(
            new SingleClientFactory(handler),
            Options(),
            new TestUserContextAccessor(tenant, SyntheticUser),
            EventStoreTestSupport.CreateClassifier(),
            cache,
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

    private static IQueryService NewQueryClient(Uri baseAddress, IETagCache cache, string tenant = SyntheticTenant)
        => new EventStoreQueryClient(
            new BaseAddressClientFactory(baseAddress),
            Options(),
            new TestUserContextAccessor(tenant, SyntheticUser),
            EventStoreTestSupport.CreateClassifier(),
            cache,
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

    private static IOptions<EventStoreOptions> Options()
        => Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => ValueTask.FromResult<string?>(SyntheticBearerToken),
            RequireAccessToken = true,
        });

    private static ShipOrderCommand Command(string tenantId = SyntheticTenant)
        => new() {
            TenantId = tenantId,
            AggregateId = "order-1",
            Quantity = 3,
        };

    private static QueryRequest QueryRequest(
        string? TenantId = SyntheticTenant,
        string? ETag = null,
        IReadOnlyList<string>? ETags = null,
        string? CacheDiscriminator = null)
        => new(
            ProjectionType: "orders",
            TenantId,
            Domain: "orders",
            AggregateId: "order-1",
            QueryType: "GetOrders",
            ETag: ETag,
            ETags: ETags,
            CacheDiscriminator: CacheDiscriminator);

    private static HttpResponseMessage Response(
        HttpStatusCode status,
        string? body = null,
        Action<System.Net.Http.Headers.HttpResponseHeaders>? headers = null) {
        HttpResponseMessage response = new(status);
        if (body is not null) {
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        headers?.Invoke(response.Headers);
        return response;
    }

    private static SortedDictionary<string, string> ResponseHeaders(HttpStatusCode status, bool includeProblemDetails) {
        SortedDictionary<string, string> headers = Headers(includeProblemDetails ? [("Content-Type", "application/problem+json")] : []);
        if ((int)status == 429) {
            headers["Retry-After"] = "5";
        }

        return headers;
    }

    private static SortedDictionary<string, string> Headers(IEnumerable<(string Name, string Value)> values) {
        SortedDictionary<string, string> headers = new(StringComparer.Ordinal);
        foreach ((string name, string value) in values) {
            headers[name] = value;
        }

        return headers;
    }

    private static JsonElement Json(string json)
        => JsonDocument.Parse(json).RootElement.Clone();

    private static string ProblemDetails(int status, string providerState)
        => $$"""{"title":"{{providerState}}","detail":"Synthetic bounded contract fixture.","status":{{status}},"errors":{"payload":["Synthetic validation failure."]},"globalErrors":["Synthetic global failure."],"entityLabel":"order-1"}""";

    private static void WriteJson(string path, object value) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string json = JsonSerializer.Serialize(value, JsonOptions) + Environment.NewLine;
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private static string PactDirectory()
        => Path.Combine(RepositoryRoot(), "tests", "Hexalith.FrontComposer.Shell.Tests", "Pact");

    private static string RepositoryRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null) {
            if (File.Exists(Path.Combine(current.FullName, "Directory.Packages.props"))) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }

    private static void ScanDirectoryForRedactionLeaks(string pactDirectory) {
        foreach (string file in Directory.EnumerateFiles(pactDirectory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetExtension(path) is ".json" or ".md")) {
            string text = File.ReadAllText(file, Encoding.UTF8);
            RedactionScanner.FindLeaks(text).ShouldBeEmpty($"Contract artifact {Path.GetFileName(file)} contains a disallowed secret pattern.");
        }
    }

    [BoundedContext("Orders")]
    private sealed class ShipOrderCommand {
        public string TenantId { get; init; } = SyntheticTenant;
        public string AggregateId { get; init; } = "order-1";
        public int Quantity { get; init; }
    }

    private sealed record OrderProjection(string Id, string? Status);

    private sealed class FixedUlidFactory : IUlidFactory {
        public string NewUlid() => SyntheticMessageId;
    }

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class BaseAddressClientFactory(Uri baseAddress) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new() { BaseAddress = baseAddress };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            string? body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            SortedDictionary<string, string> headers = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers) {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            if (request.Content?.Headers.ContentType is not null) {
                headers["Content-Type"] = request.Content.Headers.ContentType.MediaType ?? "application/json";
            }

            JsonElement? jsonBody = string.IsNullOrWhiteSpace(body) ? null : Json(body);
            Requests.Add(new CapturedRequest(
                request.Method.Method,
                request.RequestUri?.PathAndQuery ?? string.Empty,
                headers,
                jsonBody,
                request.RequestUri?.PathAndQuery == "/api/v1/commands"
                    ? "samples/Counter/Counter.Domain/ShipOrderCommand metadata"
                    : "samples/Counter/Counter.Web generated orders projection metadata",
                request.RequestUri?.PathAndQuery == "/api/v1/commands"
                    ? "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs"
                    : "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs",
                request.RequestUri?.PathAndQuery == "/api/v1/commands"
                    ? "AC2, AC3, AC4, AC14, AC17, AC22, AC23"
                    : "AC5, AC6, AC7, AC8, AC14, AC17, AC22, AC23"));

            return responseFactory(request);
        }
    }

    private sealed class NoCache : IETagCache {
        public bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key) {
            key = string.Empty;
            return false;
        }

        public Task<ETagCacheEntry?> TryGetAsync(string key, int expectedPayloadVersion, CancellationToken cancellationToken = default)
            => Task.FromResult<ETagCacheEntry?>(null);

        public Task SetAsync(string key, ETagCacheEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByProjectionTypeAsync(string tenantId, string userId, string projectionType, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SeededCache(string eTag, string payload) : IETagCache {
        public bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key) {
            key = $"{tenantId}:{userId}:{discriminator}";
            return true;
        }

        public Task<ETagCacheEntry?> TryGetAsync(string key, int expectedPayloadVersion, CancellationToken cancellationToken = default)
            => Task.FromResult<ETagCacheEntry?>(new ETagCacheEntry(
                eTag,
                payload,
                CachedAtUtcTicks: 1,
                LastAccessedUtcTicks: 1,
                FormatVersion: ETagCacheEntry.CurrentFormatVersion,
                PayloadVersion: expectedPayloadVersion,
                Discriminator: "orders-grid"));

        public Task SetAsync(string key, ETagCacheEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByProjectionTypeAsync(string tenantId, string userId, string projectionType, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private static class RedactionScanner {
        private static readonly string[] ForbiddenFragments = [
            "access_token=",
            "api_key=",
            "authorization_payload",
            "connectionstring",
            "cookie",
            "password=",
            "set-cookie",
        ];

        public static IReadOnlyList<string> FindLeaks(string text) {
            List<string> leaks = [];
            string normalized = text.Replace(SyntheticBearerToken, "ALLOWLISTED_SYNTHETIC_TOKEN", StringComparison.Ordinal);
            string lower = normalized.ToLowerInvariant();

            foreach (string fragment in ForbiddenFragments) {
                if (lower.Contains(fragment, StringComparison.Ordinal)) {
                    leaks.Add(fragment);
                }
            }

            if ((System.Text.RegularExpressions.Regex.IsMatch(normalized, "\"authorization\"\\s*:", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    || System.Text.RegularExpressions.Regex.IsMatch(normalized, "\\bauthorization\\s*:", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                && !text.Contains($"Bearer {SyntheticBearerToken}", StringComparison.Ordinal)) {
                leaks.Add("raw Authorization header");
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "Bearer\\s+[A-Za-z0-9_\\-]+\\.[A-Za-z0-9_\\-]+\\.[A-Za-z0-9_\\-]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                leaks.Add("jwt bearer token");
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "[A-Za-z]:(?:\\\\)+Users(?:\\\\)+[^\\\\]+(?:\\\\)+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                leaks.Add("local user path");
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "[A-Z0-9_]{8,}=.{6,}", System.Text.RegularExpressions.RegexOptions.None)) {
                leaks.Add("environment-shaped secret");
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(normalized, "[A-Za-z0-9+/]{64,}={0,2}", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                leaks.Add("encoded token-like payload");
            }

            return leaks;
        }
    }

    private sealed record CapturedRequest(
        string Method,
        string Path,
        SortedDictionary<string, string> Headers,
        JsonElement? Body,
        string GeneratedSource,
        string AdapterPath,
        string OwningAcceptanceCriteria);

    private sealed record ContractParty(string Name);

    private sealed record ContractProviderState(string Name);

    private sealed record ContractHttpRequest(
        string Method,
        string Path,
        SortedDictionary<string, string> Headers,
        JsonElement? Body);

    private sealed record ContractHttpResponse(
        int Status,
        SortedDictionary<string, string> Headers,
        JsonElement? Body);

    private sealed record ContractInteractionMetadata(
        string GeneratedSource,
        string AdapterPath,
        string OwningAcceptanceCriteria,
        string ClassifierExpectation);

    private sealed record ContractInteraction(
        string Type,
        string Description,
        IReadOnlyList<ContractProviderState> ProviderStates,
        ContractHttpRequest Request,
        ContractHttpResponse Response,
        ContractInteractionMetadata Metadata);

    private sealed record ContractSpecification(string Version);

    private sealed record ContractTool(string Version);

    private sealed record ContractMetadata(
        ContractSpecification PactSpecification,
        ContractTool PactNet,
        string FrontComposerStory,
        string GeneratedBy);

    private sealed record PactDocument(
        ContractParty Consumer,
        ContractParty Provider,
        IReadOnlyList<ContractInteraction> Interactions,
        ContractMetadata Metadata);
}

#pragma warning restore CA2007
