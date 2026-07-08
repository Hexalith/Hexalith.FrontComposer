using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Communication;

public sealed class Story114WireFormatTests {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ProjectionChangedDetail_JsonShape_UsesPinnedCamelCaseMembers() {
        ProjectionChangedDetail detail = new(
            "counter",
            "tenant-a",
            "conversation-1",
            new SortedDictionary<string, string>(StringComparer.Ordinal) {
                ["aggregateId"] = "order-1",
                ["etag"] = "\"v1\"",
            });

        string json = JsonSerializer.Serialize(detail, JsonOptions);

        json.ShouldBe("""{"projectionType":"counter","tenantId":"tenant-a","groupScope":"conversation-1","metadata":{"aggregateId":"order-1","etag":"\u0022v1\u0022"}}""");
        ProjectionChangedDetail roundTrip = JsonSerializer.Deserialize<ProjectionChangedDetail>(json, JsonOptions)!;
        roundTrip.ProjectionType.ShouldBe(detail.ProjectionType);
        roundTrip.TenantId.ShouldBe(detail.TenantId);
        roundTrip.GroupScope.ShouldBe(detail.GroupScope);
        roundTrip.Metadata.ShouldBe(detail.Metadata);
    }

    [Fact]
    public void ProjectionChangedDetail_JsonShape_AllowsMissingMetadataAsEmpty() {
        const string missingJson = """{"projectionType":"counter","tenantId":"tenant-a"}""";

        ProjectionChangedDetail missing = JsonSerializer.Deserialize<ProjectionChangedDetail>(missingJson, JsonOptions)!;

        missing.GroupScope.ShouldBeNull();
        missing.Metadata.ShouldNotBeNull();
        missing.Metadata.ShouldBeEmpty();

        const string nullJson = """{"projectionType":"counter","tenantId":"tenant-a","groupScope":null,"metadata":null}""";

        ProjectionChangedDetail explicitNull = JsonSerializer.Deserialize<ProjectionChangedDetail>(nullJson, JsonOptions)!;

        explicitNull.Metadata.ShouldNotBeNull();
        explicitNull.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void CommandResult_JsonShape_UsesPinnedMembersAndStatusConstants() {
        CommandResult result = new(
            "01HX0000000000000000000000",
            CommandResultStatus.Accepted,
            "corr-1",
            new Uri("https://example.test/commands/status/corr-1"),
            TimeSpan.FromSeconds(3));

        string json = JsonSerializer.Serialize(result, JsonOptions);

        CommandResultStatus.Accepted.ShouldBe("Accepted");
        CommandResultStatus.Rejected.ShouldBe("Rejected");
        json.ShouldBe("""{"messageId":"01HX0000000000000000000000","status":"Accepted","correlationId":"corr-1","location":"https://example.test/commands/status/corr-1","retryAfter":"00:00:03"}""");
        CommandResult roundTrip = JsonSerializer.Deserialize<CommandResult>(json, JsonOptions)!;
        roundTrip.ShouldBe(result);
    }

    [Fact]
    public void CommandResult_JsonShape_AllowsMissingOptionalMembers() {
        const string json = """{"messageId":"01HX0000000000000000000001","status":"Rejected","unexpected":"ignored"}""";

        CommandResult result = JsonSerializer.Deserialize<CommandResult>(json, JsonOptions)!;

        result.MessageId.ShouldBe("01HX0000000000000000000001");
        result.Status.ShouldBe(CommandResultStatus.Rejected);
        result.CorrelationId.ShouldBeNull();
        result.Location.ShouldBeNull();
        result.RetryAfter.ShouldBeNull();
    }

    [Fact]
    public void ProblemDetailsPayload_JsonShape_UsesPinnedCamelCaseMembers() {
        ProblemDetailsPayload payload = new(
            "Invalid command",
            "Quantity must be positive.",
            400,
            "Order",
            new SortedDictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) {
                ["Quantity"] = ["must be greater than zero"],
            },
            ["Correct the highlighted fields."]);

        string json = JsonSerializer.Serialize(payload, JsonOptions);

        json.ShouldBe("""{"title":"Invalid command","detail":"Quantity must be positive.","status":400,"entityLabel":"Order","validationErrors":{"Quantity":["must be greater than zero"]},"globalErrors":["Correct the highlighted fields."],"rejectionDetails":null}""");
        ProblemDetailsPayload roundTrip = JsonSerializer.Deserialize<ProblemDetailsPayload>(json, JsonOptions)!;
        roundTrip.Title.ShouldBe(payload.Title);
        roundTrip.Detail.ShouldBe(payload.Detail);
        roundTrip.Status.ShouldBe(payload.Status);
        roundTrip.EntityLabel.ShouldBe(payload.EntityLabel);
        roundTrip.ValidationErrors["Quantity"].ShouldBe(payload.ValidationErrors["Quantity"]);
        roundTrip.GlobalErrors.ShouldBe(payload.GlobalErrors);
        roundTrip.RejectionDetails.ShouldBeNull();
    }

    [Fact]
    public void ProblemDetailsPayload_JsonShape_AllowsMissingErrorCollectionsAsEmpty() {
        const string json = """{"title":"Invalid command","detail":"Quantity must be positive.","status":400,"entityLabel":"Order"}""";

        ProblemDetailsPayload roundTrip = JsonSerializer.Deserialize<ProblemDetailsPayload>(json, JsonOptions)!;

        roundTrip.ValidationErrors.ShouldBeEmpty();
        roundTrip.GlobalErrors.ShouldBeEmpty();
    }

    [Fact]
    public void ProblemDetailsPayload_JsonShape_PinsRejectionDetails() {
        ProblemDetailsPayload payload = new(
            "Order locked",
            "Retry later.",
            409,
            "Order",
            new SortedDictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
            []) {
            RejectionDetails = new CommandRejectionDetails(
                "ORDER_LOCKED",
                "Concurrency",
                "Reload the order.",
                "FC-CMD-409"),
        };

        string json = JsonSerializer.Serialize(payload, JsonOptions);

        json.ShouldBe("""{"title":"Order locked","detail":"Retry later.","status":409,"entityLabel":"Order","validationErrors":{},"globalErrors":[],"rejectionDetails":{"errorCode":"ORDER_LOCKED","reasonCategory":"Concurrency","suggestedAction":"Reload the order.","docsCode":"FC-CMD-409"}}""");
        ProblemDetailsPayload roundTrip = JsonSerializer.Deserialize<ProblemDetailsPayload>(json, JsonOptions)!;
        roundTrip.RejectionDetails.ShouldNotBeNull();
        roundTrip.RejectionDetails.ErrorCode.ShouldBe("ORDER_LOCKED");
        roundTrip.RejectionDetails.ReasonCategory.ShouldBe("Concurrency");
        roundTrip.RejectionDetails.SuggestedAction.ShouldBe("Reload the order.");
        roundTrip.RejectionDetails.DocsCode.ShouldBe("FC-CMD-409");
    }
}
