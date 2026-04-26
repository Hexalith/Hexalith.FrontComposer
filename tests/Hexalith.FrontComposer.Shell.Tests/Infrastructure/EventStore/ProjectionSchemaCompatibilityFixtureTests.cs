using System.Text.Json;

using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class ProjectionSchemaCompatibilityFixtureTests {
    [Theory]
    [InlineData("current-order-projection.json", "order-current")]
    [InlineData("prior-minor-order-projection.json", "order-prior")]
    [InlineData("forward-order-projection.json", "order-forward")]
    public void SupportedProjectionFixtures_DeserializeWithWebDefaults(string fileName, string expectedId) {
        string json = ReadFixture(fileName);

        FixtureOrderProjection? projection = JsonSerializer.Deserialize<FixtureOrderProjection>(
            json,
            EventStoreRequestContent.JsonOptions);

        projection.ShouldNotBeNull();
        projection!.Id.ShouldBe(expectedId);
    }

    [Fact]
    public void IncompatibleProjectionFixture_MapsToSchemaMismatchPath() {
        string json = ReadFixture("incompatible-order-projection.json");

        JsonException ex = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<FixtureOrderProjection>(
            json,
            EventStoreRequestContent.JsonOptions));

        ProjectionSchemaMismatchException mismatch = new("FixtureOrderProjection", ex);
        mismatch.ProjectionType.ShouldBe("FixtureOrderProjection");
        mismatch.InnerException.ShouldBe(ex);
    }

    private static string ReadFixture(string fileName) {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "TestData",
            "SchemaCompatibility",
            fileName));
        return File.ReadAllText(path);
    }

    private sealed record FixtureOrderProjection(string Id, string? Status = null);
}
