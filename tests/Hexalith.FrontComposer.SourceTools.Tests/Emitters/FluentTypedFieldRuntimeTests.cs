using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Exercises actual generated typed input parameters and edit callbacks against Fluent V5.</summary>
public sealed class FluentTypedFieldRuntimeTests
{
    /// <summary>Invalid identifier edits preserve the model and validation state until repair or permitted clearing.</summary>
    /// <param name="nullable">Whether the generated property allows null.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "MutationErrorRegression")]
    public async Task InvalidGuidRetainsModelUntilRepairAndNullableClear(bool nullable)
    {
        using TypedFieldFormFixture fixture = TypedFieldFormFixture.Create(nullable);
        PropertyInfo property = fixture.Model.GetType().GetProperty("Identifier").ShouldNotBeNull();
        Guid original = Guid.Parse("cf10400d-aefa-4082-acfc-0e87a9fcd8b9");
        property.SetValue(fixture.Model, original);
        TypedFieldInputParameters input = fixture.GetInput("Identifier");
        input.Type.ShouldBe(typeof(FluentTextInput));
        input.Values["Value"].ShouldBe(original.ToString());
        input.Values["Class"].ShouldBe("fc-command-input fc-monospace");
        FieldIdentifier.Create((Expression<Func<string?>>)input.Values["ValueExpression"]!).FieldName.ShouldBe("_IdentifierString");
        LambdaExpression validation = (LambdaExpression)input.Values["ValidationFieldFor"]!;
        MemberExpression member = (MemberExpression)validation.Body;
        member.Member.Name.ShouldBe("Identifier");
        validation.ReturnType.ShouldBe(nullable ? typeof(Guid?) : typeof(Guid));
        EventCallback<string?> changed = input.Values["ValueChanged"].ShouldBeOfType<EventCallback<string?>>();
        FieldIdentifier field = new(fixture.Model, "Identifier");
        int notifications = 0;
        fixture.EditContext.OnFieldChanged += (_, e) =>
        {
            if (e.FieldIdentifier.Equals(field))
            {
                notifications++;
            }
        };
        await fixture.Renderer.Dispatcher.InvokeAsync(() => changed.InvokeAsync("not-a-guid"));
        property.GetValue(fixture.Model).ShouldBe(original);
        fixture.EditContext.GetValidationMessages(field).ShouldBe(["Invalid identifier format."]);
        fixture.HasParseErrors().ShouldBeTrue();
        fixture.GetInput("Identifier").Values["Value"].ShouldBe("not-a-guid");
        fixture.GetInput("Identifier").Values["data-fc-invalid"].ShouldBe("true");
        Guid repaired = Guid.Parse("1daf3ca1-37b2-4eac-bcd5-a8699dc2f36c");
        await fixture.Renderer.Dispatcher.InvokeAsync(() => changed.InvokeAsync(repaired.ToString("D")));
        property.GetValue(fixture.Model).ShouldBe(repaired);
        fixture.EditContext.GetValidationMessages(field).ShouldBeEmpty();
        fixture.HasParseErrors().ShouldBeFalse();
        await fixture.Renderer.Dispatcher.InvokeAsync(() => changed.InvokeAsync(string.Empty));
        if (nullable)
        {
            property.GetValue(fixture.Model).ShouldBeNull();
            fixture.EditContext.GetValidationMessages(field).ShouldBeEmpty();
            fixture.HasParseErrors().ShouldBeFalse();
        }
        else
        {
            property.GetValue(fixture.Model).ShouldBe(repaired);
            fixture.EditContext.GetValidationMessages(field).ShouldBe(["An identifier is required."]);
            fixture.HasParseErrors().ShouldBeTrue();
        }

        notifications.ShouldBe(3);
    }

    /// <summary>Time editors retain typed callbacks, full-day values and nullable clearing.</summary>
    /// <param name="nullable">Whether the generated property allows null.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimeEditorPreservesTypedBindingAndFullDayRange(bool nullable)
    {
        using TypedFieldFormFixture fixture = TypedFieldFormFixture.Create(nullable);
        PropertyInfo property = fixture.Model.GetType().GetProperty("At").ShouldNotBeNull();
        TimeOnly original = new(23, 59, 42);
        property.SetValue(fixture.Model, original);
        TypedFieldInputParameters input = fixture.GetInput("At");
        input.Type.ShouldBe(nullable ? typeof(FluentTimePicker<TimeOnly?>) : typeof(FluentTimePicker<TimeOnly>));
        input.Values["Value"].ShouldBe(original);
        input.Values["RenderStyle"].ShouldBe(DatePickerRenderStyle.Native);
        input.Values["StartHour"].ShouldBe(0);
        input.Values["EndHour"].ShouldBe(24);
        input.Values["Increment"].ShouldBe(0);
        input.Values["Placeholder"].ShouldBe("HH:mm");
        LambdaExpression expression = (LambdaExpression)input.Values["ValueExpression"]!;
        expression.ReturnType.ShouldBe(nullable ? typeof(TimeOnly?) : typeof(TimeOnly));
        ((MemberExpression)expression.Body).Member.Name.ShouldBe("At");
        FieldIdentifier field = new(fixture.Model, "At");
        int notifications = 0;
        fixture.EditContext.OnFieldChanged += (_, e) =>
        {
            if (e.FieldIdentifier.Equals(field))
            {
                notifications++;
            }
        };
        TimeOnly repaired = new(0, 0);
        if (nullable)
        {
            EventCallback<TimeOnly?> changed = input.Values["ValueChanged"].ShouldBeOfType<EventCallback<TimeOnly?>>();
            await fixture.Renderer.Dispatcher.InvokeAsync(() => changed.InvokeAsync(repaired));
            property.GetValue(fixture.Model).ShouldBe(repaired);
            await fixture.Renderer.Dispatcher.InvokeAsync(() => changed.InvokeAsync(null));
            property.GetValue(fixture.Model).ShouldBeNull();
            notifications.ShouldBe(2);
        }
        else
        {
            EventCallback<TimeOnly> changed = input.Values["ValueChanged"].ShouldBeOfType<EventCallback<TimeOnly>>();
            await fixture.Renderer.Dispatcher.InvokeAsync(() => changed.InvokeAsync(repaired));
            property.GetValue(fixture.Model).ShouldBe(repaired);
            notifications.ShouldBe(1);
        }
    }
}
