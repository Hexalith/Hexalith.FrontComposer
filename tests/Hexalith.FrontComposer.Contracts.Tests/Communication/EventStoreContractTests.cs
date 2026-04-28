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
        // F02 — replace the brittle substring deny-list (Contains("Hosting") / Contains("EventStore"))
        // with exact-match family rules. Story 5-6 D2 explicitly forbids substring matches like
        // "Hosting" or "EventStore" because they false-positive on benign assembly names. The
        // authoritative governance suite lives in Hexalith.FrontComposer.Shell.Tests/Governance
        // (InfrastructureGovernanceTests.FrameworkAssemblies_DoNotReferenceProviderAssemblies).
        // This Contracts-side test stays as a fast-feedback safety net using the same exact-rule
        // discipline.
        string[] names = typeof(ICommandService).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        (string Reference, string Family)[] forbiddenExact = [
            ("Dapr", "Dapr SDK"),
            ("StackExchange.Redis", "Redis"),
            ("Microsoft.AspNetCore.SignalR.Client", "SignalR Client (Shell-only)"),
            ("Microsoft.AspNetCore.SignalR.StackExchangeRedis", "Redis"),
            ("Confluent.Kafka", "Kafka"),
            ("Npgsql", "PostgreSQL"),
            ("Microsoft.Azure.Cosmos", "Cosmos DB"),
            ("Azure.Storage", "Azure Storage"),
            ("Azure.Messaging.ServiceBus", "Azure Service Bus"),
            ("Amazon.S3", "AWS provider SDK"),
            ("Google.Cloud", "GCP provider SDK"),
        ];

        foreach ((string forbidden, string family) in forbiddenExact) {
            names.ShouldNotContain(
                n => string.Equals(n, forbidden, StringComparison.OrdinalIgnoreCase)
                    || n.StartsWith(forbidden + ".", StringComparison.OrdinalIgnoreCase),
                $"Contracts must not reference provider family '{family}' (matched on '{forbidden}'). Route through EventStore contract/client or deployment/AppHost component configuration.");
        }
    }
}
