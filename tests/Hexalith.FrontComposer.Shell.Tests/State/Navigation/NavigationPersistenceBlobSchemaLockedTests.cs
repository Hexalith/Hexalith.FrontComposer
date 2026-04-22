using System.Collections.Generic;
using System.Text.Json;

using Hexalith.FrontComposer.Shell.Infrastructure.Storage;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-6 AC8 / ADR-048 — schema-lock for <see cref="NavigationPersistenceBlob"/>. Serializes
/// through the production <see cref="LocalStorageService.SchemaLockJsonOptions"/> so the test
/// pins the actual wire format (camelCase, null-default omitted) rather than the default
/// serializer shape.
/// </summary>
public sealed class NavigationPersistenceBlobSchemaLockedTests
{
    [Fact]
    public void LastActiveRouteFieldPresent_AndOmittedWhenNull()
    {
        NavigationPersistenceBlob blobWithRoute = new(
            SidebarCollapsed: true,
            CollapsedGroups: new Dictionary<string, bool> { ["Counter"] = true },
            LastActiveRoute: "domain/counter/counter-list");

        string json = JsonSerializer.Serialize(blobWithRoute, LocalStorageService.SchemaLockJsonOptions);

        json.ShouldContain("\"lastActiveRoute\":\"domain/counter/counter-list\"");
        json.ShouldContain("\"sidebarCollapsed\":true");
        json.ShouldContain("\"collapsedGroups\":{");

        NavigationPersistenceBlob blobNullRoute = new(
            SidebarCollapsed: false,
            CollapsedGroups: new Dictionary<string, bool>(),
            LastActiveRoute: null);

        string jsonNullRoute = JsonSerializer.Serialize(blobNullRoute, LocalStorageService.SchemaLockJsonOptions);

        // WhenWritingDefault + camelCase: null LastActiveRoute MUST NOT appear in the wire payload.
        jsonNullRoute.ShouldNotContain("lastActiveRoute", Case.Insensitive);
    }

    [Fact]
    public void BlobRoundTripsThroughProductionJsonOptions()
    {
        NavigationPersistenceBlob original = new(
            SidebarCollapsed: true,
            CollapsedGroups: new Dictionary<string, bool> { ["Counter"] = true, ["Orders"] = false },
            LastActiveRoute: "domain/orders/order-list?tab=1");

        string json = JsonSerializer.Serialize(original, LocalStorageService.SchemaLockJsonOptions);
        NavigationPersistenceBlob? roundTripped = JsonSerializer.Deserialize<NavigationPersistenceBlob>(
            json, LocalStorageService.SchemaLockJsonOptions);

        roundTripped.ShouldNotBeNull();
        roundTripped!.SidebarCollapsed.ShouldBeTrue();
        roundTripped.CollapsedGroups.ShouldNotBeNull();
        roundTripped.CollapsedGroups.Count.ShouldBe(2);
        roundTripped.CollapsedGroups["Counter"].ShouldBeTrue();
        roundTripped.CollapsedGroups["Orders"].ShouldBeFalse();
        roundTripped.LastActiveRoute.ShouldBe("domain/orders/order-list?tab=1");
    }

    [Fact]
    public void OlderBlobWithoutLastActiveRouteDeserialisesWithNullField()
    {
        // Pre-3-6 wire shape — the additive field must be null-tolerant on deserialise.
        const string preThreeSixPayload = "{\"sidebarCollapsed\":true,\"collapsedGroups\":{\"Counter\":true}}";

        NavigationPersistenceBlob? blob = JsonSerializer.Deserialize<NavigationPersistenceBlob>(
            preThreeSixPayload, LocalStorageService.SchemaLockJsonOptions);

        blob.ShouldNotBeNull();
        blob!.SidebarCollapsed.ShouldBeTrue();
        blob.LastActiveRoute.ShouldBeNull();
    }
}
