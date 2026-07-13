using System.Text.Json;

using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Storage;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>
/// Pins the canonical Shell JSON profiles and their retained production aliases.
/// </summary>
public sealed class FcJsonTests {
    [Fact]
    public void Profiles_AreReadOnlyAndProductionAliasesReferenceCanonicalInstances() {
        FcJson.PlainWeb.IsReadOnly.ShouldBeTrue();
        FcJson.StorageCompact.IsReadOnly.ShouldBeTrue();

        ReferenceEquals(EventStoreRequestContent.JsonOptions, FcJson.PlainWeb).ShouldBeTrue();
        ReferenceEquals(LocalStorageService.SchemaLockJsonOptions, FcJson.StorageCompact).ShouldBeTrue();
    }

    [Fact]
    public void PlainWeb_PreservesCamelCaseNullAndDefaultMembersExactly() {
        JsonProfilePayload payload = new(DisplayName: null, Enabled: false, Count: 0);

        string json = JsonSerializer.Serialize(payload, FcJson.PlainWeb);

        json.ShouldBe("{\"displayName\":null,\"enabled\":false,\"count\":0}");
    }

    [Fact]
    public void StorageCompact_OmitsNullAndDefaultMembersAndRoundTrips() {
        JsonProfilePayload payload = new(DisplayName: "FrontComposer", Enabled: false, Count: 0);

        string json = JsonSerializer.Serialize(payload, FcJson.StorageCompact);
        JsonProfilePayload? roundTripped = JsonSerializer.Deserialize<JsonProfilePayload>(
            json,
            FcJson.StorageCompact);

        json.ShouldBe("{\"displayName\":\"FrontComposer\"}");
        roundTripped.ShouldBe(payload);
    }

    private sealed record JsonProfilePayload(string? DisplayName, bool Enabled, int Count);
}
