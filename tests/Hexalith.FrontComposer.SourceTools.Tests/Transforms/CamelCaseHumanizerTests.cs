
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public class CamelCaseHumanizerTests {
    [Theory]
    [InlineData("OrderDate", "Order Date")]
    [InlineData("XMLParser", "XML Parser")]
    [InlineData("OrderID", "Order ID")]
    [InlineData("Id", "Id")]
    [InlineData("firstName", "First Name")]
    [InlineData("", "")]
    [InlineData("ORDER", "ORDER")]
    [InlineData("Order2Name", "Order2 Name")]
    [InlineData("OrderIDs", "Order IDs")]
    [InlineData("HTMLToJSON", "HTML To JSON")]
    [InlineData("a", "A")]
    [InlineData("AB", "AB")]
    [InlineData("getHTTPResponse", "Get HTTP Response")]
    [InlineData("VeryLongEnumMemberNameThatExceedsThirtyCharacterLimit", "Very Long Enum Member Name That Exceeds Thirty Character Limit")]
    [InlineData("Name", "Name")]
    public void Humanize_KnownInputs_ReturnsExpected(string input, string expected) => CamelCaseHumanizer.Humanize(input).ShouldBe(expected);

    [Fact]
    public void Humanize_Null_ReturnsNull() => CamelCaseHumanizer.Humanize(null).ShouldBeNull();
}
