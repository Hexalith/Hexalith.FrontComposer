using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

public sealed class GridViewSnapshotTests {
    [Fact]
    public void FiltersUseOrdinalEqualityAcrossInputComparers() {
        ImmutableDictionary<string, string> ordinal = ImmutableDictionary<string, string>.Empty
            .Add("Name", "Active");
        ImmutableDictionary<string, string> caseInsensitive = ImmutableDictionary
            .Create<string, string>(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase)
            .Add("Name", "Active");
        GridViewSnapshot original = new(0, ordinal, null, false, null, null, DateTimeOffset.UnixEpoch);
        GridViewSnapshot equivalent = original with { Filters = caseInsensitive };
        GridViewSnapshot differentKey = original with {
            Filters = caseInsensitive.Clear().Add("name", "Active"),
        };
        GridViewSnapshot differentValue = original with {
            Filters = caseInsensitive.Clear().Add("Name", "active"),
        };

        original.Equals(equivalent).ShouldBeTrue();
        equivalent.Equals(original).ShouldBeTrue();
        original.GetHashCode().ShouldBe(equivalent.GetHashCode());
        original.Equals(differentKey).ShouldBeFalse();
        differentKey.Equals(original).ShouldBeFalse();
        original.Equals(differentValue).ShouldBeFalse();
        differentValue.Equals(original).ShouldBeFalse();
        equivalent.Filters.ShouldBeSameAs(caseInsensitive);
    }

    [Fact]
    public void FiltersPreserveDistinctEntriesWithOrdinallyIdenticalKeys() {
        ImmutableDictionary<string, string> repeatedKey = ImmutableDictionary
            .Create<string, string>(ReferenceEqualityComparer.Instance)
            .Add(new string('N', 1), "Active")
            .Add(new string('N', 1), "Inactive");
        GridViewSnapshot original = new(0, repeatedKey, null, false, null, null, DateTimeOffset.UnixEpoch);
        GridViewSnapshot equivalent = original with {
            Filters = repeatedKey.Clear()
                .Add(new string('N', 1), "Inactive")
                .Add(new string('N', 1), "Active"),
        };
        GridViewSnapshot different = original with {
            Filters = ImmutableDictionary<string, string>.Empty.Add("N", "Active").Add("Other", "Inactive"),
        };

        original.Filters.Count.ShouldBe(2);
        original.Equals(equivalent).ShouldBeTrue();
        equivalent.Equals(original).ShouldBeTrue();
        original.GetHashCode().ShouldBe(equivalent.GetHashCode());
        original.Equals(different).ShouldBeFalse();
        different.Equals(original).ShouldBeFalse();
    }
}
