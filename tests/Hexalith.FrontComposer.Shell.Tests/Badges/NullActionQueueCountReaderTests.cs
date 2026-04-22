using Hexalith.FrontComposer.Shell.Badges;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Badges;

public sealed class NullActionQueueCountReaderTests {
    [Fact]
    public async Task GetCountAsync_ReturnsZeroForAnyType() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        NullActionQueueCountReader sut = new();

        int countA = await sut.GetCountAsync(typeof(string), ct);
        int countB = await sut.GetCountAsync(typeof(int), ct);

        countA.ShouldBe(0);
        countB.ShouldBe(0);
    }

    [Fact]
    public async Task GetCountAsync_ThrowsArgumentNullException_OnNullType() {
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        NullActionQueueCountReader sut = new();

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.GetCountAsync(null!, ct).ConfigureAwait(false));
    }
}
