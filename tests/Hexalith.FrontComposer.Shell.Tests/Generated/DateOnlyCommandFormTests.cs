using System.Globalization;

using Bunit;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Verifies the pinned Fluent pickers round-trip date-only command values.</summary>
public sealed class DateOnlyCommandFormTests : CommandRendererTestBase
{
    [Theory]
    [InlineData("en-US", "3/7/2026")]
    [InlineData("fr-FR", "07/03/2026")]
    public async Task TypedDateEditorsRoundTripLocalizedInputAndClearNullableDates(string cultureName, string input)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            JSInterop.SetupModule("./_content/Microsoft.FluentUI.AspNetCore.Components/Components/DateTime/FluentCalendar.razor.js").Mode = JSRuntimeMode.Loose;
            await InitializeStoreAsync();

            DateOnly initial = new(2026, 2, 12);
            DateOnlyCommand model = new() { StartsOn = initial, EndsOn = initial };
            IRenderedComponent<DateOnlyCommandForm> cut = Render<DateOnlyCommandForm>(p => p.Add(form => form.InitialValue, model));
            IRenderedComponent<FluentDatePicker<DateOnly>> startPicker = cut.FindComponent<FluentDatePicker<DateOnly>>();
            IRenderedComponent<FluentDatePicker<DateOnly?>> endPicker = cut.FindComponent<FluentDatePicker<DateOnly?>>();
            FluentTextInput startInput = startPicker.FindComponent<FluentTextInput>().Instance;
            FluentTextInput endInput = endPicker.FindComponent<FluentTextInput>().Instance;
            startInput.Value.ShouldBe(initial.ToString("d", CultureInfo.CurrentCulture));
            endInput.Value.ShouldBe(startInput.Value);

            await cut.InvokeAsync(() => startInput.ValueChanged.InvokeAsync(input));
            await cut.InvokeAsync(() => endInput.ValueChanged.InvokeAsync(input));

            DateOnly expected = new(2026, 3, 7);
            model.StartsOn.ShouldBe(expected);
            model.EndsOn.ShouldBe(expected);
            startPicker.Instance.Value.ShouldBe(expected);
            endPicker.Instance.Value.ShouldBe(expected);

            await cut.InvokeAsync(() => endInput.ValueChanged.InvokeAsync(string.Empty));

            model.EndsOn.ShouldBeNull();
            endPicker.Instance.Value.ShouldBeNull();
            model.StartsOn.ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
