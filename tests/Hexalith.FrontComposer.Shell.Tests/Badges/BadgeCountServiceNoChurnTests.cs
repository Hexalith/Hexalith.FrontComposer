using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Shell.Badges;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Badges;

/// <summary>
/// Story 5-2 AC7 — when the reader returns the same count twice (304 reuses cached value, or
/// 429 preserves the prior visible value), <c>BadgeCountService</c> MUST NOT emit a
/// <c>CountChanged</c> notification. Without that suppression every cached refresh would
/// animate the badge.
/// </summary>
public class BadgeCountServiceNoChurnTests {
    [Fact]
    public async Task UpdateCount_DuplicateValue_DoesNotEmit() {
        FixedReader reader = new();
        StaticCatalog catalog = new(typeof(SampleProjection));
        ServiceCollection services = new();
        using ServiceProvider provider = services.BuildServiceProvider();
        using BadgeCountService sut = new(catalog, reader, provider, NullLogger<BadgeCountService>.Instance, TimeProvider.System);

        List<BadgeCountChangedArgs> emissions = new();
        using System.IDisposable subscription = sut.CountChanged.Subscribe(args => emissions.Add(args));

        // Initial fan-out — emits once with count=3.
        await sut.InitializeAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Re-initialize with the same value (simulating a 304 / 429 path returning the same
        // count): no second emission.
        await sut.InitializeAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        emissions.Count.ShouldBe(1);
        emissions[0].NewCount.ShouldBe(3);
        sut.Counts[typeof(SampleProjection)].ShouldBe(3);
    }

    [Fact]
    public async Task UpdateCount_ChangedValue_EmitsAgain() {
        DynamicReader reader = new(initialCount: 3);
        StaticCatalog catalog = new(typeof(SampleProjection));
        ServiceCollection services = new();
        using ServiceProvider provider = services.BuildServiceProvider();
        using BadgeCountService sut = new(catalog, reader, provider, NullLogger<BadgeCountService>.Instance, TimeProvider.System);

        List<BadgeCountChangedArgs> emissions = new();
        using System.IDisposable subscription = sut.CountChanged.Subscribe(args => emissions.Add(args));

        await sut.InitializeAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        reader.Count = 7;
        await sut.InitializeAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        emissions.Count.ShouldBe(2);
        emissions[0].NewCount.ShouldBe(3);
        emissions[1].NewCount.ShouldBe(7);
    }

    private sealed class SampleProjection { }

    private sealed class StaticCatalog(System.Type type) : IActionQueueProjectionCatalog {
        public IReadOnlyList<System.Type> ActionQueueTypes { get; } = new[] { type };
    }

    private sealed class FixedReader : IActionQueueCountReader {
        public ValueTask<int> GetCountAsync(System.Type projectionType, CancellationToken cancellationToken)
            => new(3);
    }

    private sealed class DynamicReader(int initialCount) : IActionQueueCountReader {
        public int Count { get; set; } = initialCount;

        public ValueTask<int> GetCountAsync(System.Type projectionType, CancellationToken cancellationToken)
            => new(Count);
    }
}
