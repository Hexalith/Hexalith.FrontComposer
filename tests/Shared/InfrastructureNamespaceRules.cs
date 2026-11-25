namespace Hexalith.FrontComposer.Tests.Governance;

/// <summary>Provider namespaces excluded from framework source and generated code.</summary>
internal static class InfrastructureNamespaceRules
{
    /// <summary>Forbidden C# namespace roots and their diagnostic display names.</summary>
    public static IReadOnlyList<(string Reference, string ProviderFamily)> All { get; } = [
        ("Dapr", "Dapr SDK"),
        ("StackExchange.Redis", "Redis"),
        ("Microsoft.AspNetCore.SignalR.StackExchangeRedis", "Redis"),
        ("Microsoft.AspNetCore.SignalR.Client", "SignalR Client (Shell-only)"),
        ("Confluent.Kafka", "Kafka"),
        ("Npgsql", "PostgreSQL"),
        ("Microsoft.Azure.Cosmos", "Cosmos DB"),
        ("Azure.Storage", "Azure Storage"),
        ("Azure.Messaging.ServiceBus", "Azure Service Bus"),
        ("Amazon.S3", "AWS provider SDK"),
        ("Google.Cloud", "GCP provider SDK"),
    ];
}
