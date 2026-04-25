using Hexalith.FrontComposer.Contracts.Communication;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Communication;

public sealed class EventStoreContractTests {
    [Fact]
    public void CommandResult_PreservesTwoArgumentConstructor_WithAppendOnlyMetadata() {
        CommandResult legacy = new("01HX", "Accepted");
        CommandResult enriched = legacy with {
            CorrelationId = "corr-1",
            Location = new Uri("https://example.test/api/v1/commands/status/corr-1"),
            RetryAfter = TimeSpan.FromSeconds(1),
        };

        legacy.MessageId.ShouldBe("01HX");
        legacy.Status.ShouldBe("Accepted");
        legacy.CorrelationId.ShouldBeNull();
        enriched.CorrelationId.ShouldBe("corr-1");
    }

    [Fact]
    public void QueryResult_NotModified_IsExplicitNoChangeState() {
        QueryResult<string> result = QueryResult<string>.NotModified("\"etag-1\"");

        result.IsNotModified.ShouldBeTrue();
        result.Items.ShouldBeEmpty();
        result.ETag.ShouldBe("\"etag-1\"");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("orders:tenant")]
    public void EventStoreValidation_RejectsMissingOrColonRoutingValues(string value) {
        _ = Should.Throw<ArgumentException>(() => EventStoreValidation.RequireNonColonSegment(value, "value"));
    }

    [Fact]
    public void EventStoreValidation_RejectsMoreThanTenEtags() {
        string[] etags = Enumerable.Range(0, 11).Select(i => $"\"etag-{i}\"").ToArray();

        _ = Should.Throw<ArgumentException>(() => EventStoreValidation.ValidateETagCount(etags));
    }

    [Fact]
    public void ContractsAssembly_DoesNotReferenceInfrastructurePackages() {
        string[] names = typeof(ICommandService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        names.ShouldNotContain(n => n.Contains("Dapr", StringComparison.OrdinalIgnoreCase));
        names.ShouldNotContain(n => n.Contains("SignalR", StringComparison.OrdinalIgnoreCase));
        // The net10 Contracts target already references Fluent UI/AspNetCore component types for
        // rendering constants outside this communication seam. Story 5-1 must not add transport
        // infrastructure references to Contracts.
        names.ShouldNotContain(n => n.Contains("Hosting", StringComparison.OrdinalIgnoreCase));
        names.ShouldNotContain(n => n.Contains("EventStore", StringComparison.OrdinalIgnoreCase));
    }
}
