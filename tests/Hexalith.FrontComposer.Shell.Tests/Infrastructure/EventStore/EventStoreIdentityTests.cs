using System.Globalization;

using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class EventStoreIdentityTests
{
    [Fact]
    public void GetAggregateId_UsesFreshValuesWithEstablishedPropertyPrecedence()
    {
        EventStoreIdentity.GetAggregateId(new { AggregateId = "first", Id = "", Name = "unused" }).ShouldBe("first");
        EventStoreIdentity.GetAggregateId(new { AggregateId = " ", Id = "second", Name = "unused" }).ShouldBe("second");
        EventStoreIdentity.GetAggregateId(new { AggregateId = "", Id = " ", Name = "third" }).ShouldBe("third");
        EventStoreIdentity.GetAggregateId(new { Name = "fallback" }).ShouldBe("fallback");
    }

    [Fact]
    public void GetAggregateId_ConvertsWithInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            EventStoreIdentity.GetAggregateId(new { Id = 1.25m }).ShouldBe("1.25");
        }
        finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void GetAggregateId_RejectsTheFirstNonemptyInvalidProperty()
    {
        Should.Throw<ArgumentException>(() =>
            EventStoreIdentity.GetAggregateId(new { AggregateId = "invalid:segment", Id = "valid" }))
            .Message.ShouldContain("AggregateId");
        Should.Throw<ArgumentException>(() => EventStoreIdentity.GetAggregateId(new { Unrelated = "value" }));
    }
}
