#pragma warning disable CA2007
using System.Reflection;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 2-3 AC1 — dedicated regression pins for <see cref="FcColumnFilterCell"/>'s 300 ms
/// TimeProvider-driven debounce. The state/reducer layer is pinned by FilterReducerTests; this
/// closes the filter-UI debounce gap (no dedicated test file existed at baseline 8036c3c).
/// </summary>
public sealed class FcColumnFilterCellTests : BunitContext {
    private const string ViewKeyValue = "acme:OrdersProjection";
    private const string ColumnKeyValue = "Name";

    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly FakeTimeProvider _time = new();

    public FcColumnFilterCellTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddSingleton<IDispatcher>(_dispatcher);
        _ = Services.AddSingleton<TimeProvider>(_time);
    }

    [Fact]
    public async Task TypingThenAdvancingBelowDebounceThreshold_DispatchesNothing() {
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell();

        Task pending = InvokeValueChangedAsync(cut.Instance, "Ac");
        _time.Advance(TimeSpan.FromMilliseconds(299));

        _dispatcher.DidNotReceive().Dispatch(Arg.Any<ColumnFilterChangedAction>());

        // Drain the still-pending debounce so the test leaves no dangling fake timer.
        _time.Advance(TimeSpan.FromMilliseconds(1));
        await pending;
    }

    [Fact]
    public async Task TypingThenAdvancingPastDebounceThreshold_DispatchesExactlyOneActionWithTypedValue() {
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell();

        Task pending = InvokeValueChangedAsync(cut.Instance, "Acme");
        _time.Advance(TimeSpan.FromMilliseconds(300));
        await pending;

        _dispatcher.Received(1).Dispatch(ArgEx.Is<ColumnFilterChangedAction>(action =>
            action.ViewKey == ViewKeyValue
            && action.ColumnKey == ColumnKeyValue
            && action.FilterValue == "Acme"));
    }

    [Fact]
    public async Task ClearingTheInput_DispatchesActionWithNullFilterValue() {
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell("Acme");

        Task pending = InvokeValueChangedAsync(cut.Instance, "   ");
        _time.Advance(TimeSpan.FromMilliseconds(300));
        await pending;

        _dispatcher.Received(1).Dispatch(ArgEx.Is<ColumnFilterChangedAction>(action =>
            action.ViewKey == ViewKeyValue
            && action.ColumnKey == ColumnKeyValue
            && action.FilterValue == null));
    }

    [Fact]
    public async Task RapidTypingWithinWindow_CoalescesToExactlyOneDispatchWithLatestValue() {
        // The debounce contract: each keystroke cancels the prior pending delay, so a burst of
        // typing dispatches ONE ColumnFilterChangedAction carrying only the final value.
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell();

        Task first = InvokeValueChangedAsync(cut.Instance, "Ac");
        _time.Advance(TimeSpan.FromMilliseconds(100));
        Task second = InvokeValueChangedAsync(cut.Instance, "Acme");
        _time.Advance(TimeSpan.FromMilliseconds(300));
        await first;
        await second;

        _dispatcher.Received(1).Dispatch(Arg.Any<ColumnFilterChangedAction>());
        _dispatcher.Received(1).Dispatch(ArgEx.Is<ColumnFilterChangedAction>(action =>
            action.FilterValue == "Acme"));
    }

    [Fact]
    public void HydratesInitialValueFromSnapshot_OnParametersSet() {
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell("Acme");

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme");
    }

    [Fact]
    public async Task ReRenderWithUnchangedSnapshot_PreservesInFlightTypedValue() {
        // A re-render that re-supplies the SAME snapshot value must not clobber what the operator is
        // mid-way through typing (OnParametersSet only re-hydrates when the snapshot value changes).
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell();

        Task pending = InvokeValueChangedAsync(cut.Instance, "Acme");
        cut.Render(parameters => parameters
            .Add(static p => p.ViewKey, ViewKeyValue)
            .Add(static p => p.ColumnKey, ColumnKeyValue)
            .Add(static p => p.ColumnHeader, "Name")
            .Add(static p => p.InitialValue, null));

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme");

        _time.Advance(TimeSpan.FromMilliseconds(300));
        await pending;
    }

    [Fact]
    public void ReRenderWithChangedSnapshot_RehydratesToNewValue() {
        // When the hydrated snapshot value actually changes (e.g. cleared/replaced elsewhere), the
        // cell re-hydrates so the input reflects authoritative grid-view state.
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell("Acme");
        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme");

        cut.Render(parameters => parameters
            .Add(static p => p.ViewKey, ViewKeyValue)
            .Add(static p => p.ColumnKey, ColumnKeyValue)
            .Add(static p => p.ColumnHeader, "Name")
            .Add(static p => p.InitialValue, "Globex"));

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Globex");
    }

    [Fact]
    public async Task Dispose_CancelsPendingDebounce_WithoutDispatching() {
        IRenderedComponent<FcColumnFilterCell> cut = RenderCell();

        Task pending = InvokeValueChangedAsync(cut.Instance, "Acme");
        cut.Instance.Dispose();
        _time.Advance(TimeSpan.FromMilliseconds(300));
        await pending;

        _dispatcher.DidNotReceive().Dispatch(Arg.Any<ColumnFilterChangedAction>());
    }

    private IRenderedComponent<FcColumnFilterCell> RenderCell(string? initialValue = null)
        => Render<FcColumnFilterCell>(parameters => parameters
            .Add(static p => p.ViewKey, ViewKeyValue)
            .Add(static p => p.ColumnKey, ColumnKeyValue)
            .Add(static p => p.ColumnHeader, "Name")
            .Add(static p => p.InitialValue, initialValue));

    private static Task InvokeValueChangedAsync(object component, string value) {
        MethodInfo? method = component.GetType().GetMethod(
            "OnValueChangedAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        method.ShouldNotBeNull();
        object? result = method!.Invoke(component, [value]);
        result.ShouldBeAssignableTo<Task>();
        return (Task)result!;
    }

    private static T GetPrivateField<T>(object component, string fieldName) {
        FieldInfo? field = component.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.ShouldNotBeNull();
        return (T)field!.GetValue(component)!;
    }
}
