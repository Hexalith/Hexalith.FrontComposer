using System.Globalization;
using System.Reflection;

using FsCheck.Xunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>Exercises the numeric converters emitted for generated command forms.</summary>
public sealed class GeneratedNumericConverterTests
{
    [Theory]
    [InlineData("", 17, "A number is required.")]
    [InlineData("  \t", 17, "A number is required.")]
    [InlineData("123", 123, null)]
    [InlineData("2147483648", 17, "Invalid number format.")]
    [InlineData("-42", -42, null)]
    [InlineData("0009", 9, null)]
    [InlineData("abc", 17, "Invalid number format.")]
    [InlineData(null, 17, "A number is required.")]
    public async Task IntegerInputPreservesTheValueUntilItCanParse(string? input, int expected, string? error)
    {
        using TypedFieldFormFixture fixture = TypedFieldFormFixture.CreateNumeric();
        PropertyInfo property = fixture.Model.GetType().GetProperty("Quantity").ShouldNotBeNull();
        property.SetValue(fixture.Model, 17);
        TypedFieldInputParameters editor = fixture.GetInput(property.Name);
        editor.Type.ShouldBe(typeof(FluentTextInput));
        editor.Values["InputMode"].ShouldBe(TextInputMode.Numeric);

        await ChangeAsync(fixture, editor, input);

        property.GetValue(fixture.Model).ShouldBe(expected);
        string[] messages = [.. fixture.EditContext.GetValidationMessages(new FieldIdentifier(fixture.Model, property.Name))];
        messages.ShouldBe(error is null ? [] : [error]);
        fixture.HasParseErrors().ShouldBe(error is not null);
    }

    [Theory]
    [InlineData("", "12.75")]
    [InlineData("fr-FR", "12,75")]
    public async Task DecimalInputUsesTheActiveCulture(string cultureName, string input)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            using TypedFieldFormFixture fixture = TypedFieldFormFixture.CreateNumeric();
            PropertyInfo property = fixture.Model.GetType().GetProperty("Price").ShouldNotBeNull();

            await ChangeAsync(fixture, fixture.GetInput(property.Name), input);

            property.GetValue(fixture.Model).ShouldBe(12.75m);
            fixture.HasParseErrors().ShouldBeFalse();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task BlankNullableIntegerClearsItsValue()
    {
        using TypedFieldFormFixture fixture = TypedFieldFormFixture.CreateNumeric();
        PropertyInfo property = fixture.Model.GetType().GetProperty("OptionalQuantity").ShouldNotBeNull();
        property.SetValue(fixture.Model, 17);

        await ChangeAsync(fixture, fixture.GetInput(property.Name), null);

        property.GetValue(fixture.Model).ShouldBeNull();
        fixture.HasParseErrors().ShouldBeFalse();
    }

    [Property]
    public bool IntegerRoundTripsThroughTheGeneratedConverter(int value)
    {
        using TypedFieldFormFixture fixture = TypedFieldFormFixture.CreateNumeric();
        PropertyInfo property = fixture.Model.GetType().GetProperty("Quantity").ShouldNotBeNull();
        property.SetValue(fixture.Model, ~value);
        string input = value.ToString(CultureInfo.CurrentCulture);
        ChangeAsync(fixture, fixture.GetInput(property.Name), input).GetAwaiter().GetResult();
        return Equals(property.GetValue(fixture.Model), value) && !fixture.HasParseErrors();
    }

    private static Task ChangeAsync(TypedFieldFormFixture fixture, TypedFieldInputParameters editor, string? input)
    {
        EventCallback<string?> change = editor.Values["ValueChanged"].ShouldBeOfType<EventCallback<string?>>();
        return fixture.Renderer.Dispatcher.InvokeAsync(() => change.InvokeAsync(input));
    }
}
