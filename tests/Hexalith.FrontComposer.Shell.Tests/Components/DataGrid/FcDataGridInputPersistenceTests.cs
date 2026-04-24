#pragma warning disable CA2007
using System.Reflection;
using System.Collections.Generic;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

public sealed class FcDataGridInputPersistenceTests : BunitContext {
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly FakeTimeProvider _time = new();

    public FcDataGridInputPersistenceTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLogging();
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddSingleton<IDispatcher>(_dispatcher);
        _ = Services.AddSingleton<TimeProvider>(_time);
    }

    [Fact]
    public async Task ColumnFilterCell_KeepsPendingTypedValue_OnUnchangedParameterRerender() {
        IRenderedComponent<FcColumnFilterCell> cut = Render<FcColumnFilterCell>(parameters => parameters
            .Add(static p => p.ViewKey, "acme:OrdersProjection")
            .Add(static p => p.ColumnKey, "Name")
            .Add(static p => p.ColumnHeader, "Name")
            .Add(static p => p.InitialValue, "Acme"));

        Task pendingChange = InvokeChangeAsync(cut.Instance, "Acme Corp");

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme Corp");

        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(FcColumnFilterCell.ViewKey)] = "acme:OrdersProjection",
            [nameof(FcColumnFilterCell.ColumnKey)] = "Name",
            [nameof(FcColumnFilterCell.ColumnHeader)] = "Name",
            [nameof(FcColumnFilterCell.InitialValue)] = "Acme",
        })));

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme Corp");

        _time.Advance(TimeSpan.FromMilliseconds(300));
        await pendingChange;

        _dispatcher.Received(1).Dispatch(Arg.Is<ColumnFilterChangedAction>(action =>
            action.ViewKey == "acme:OrdersProjection"
            && action.ColumnKey == "Name"
            && action.FilterValue == "Acme Corp"));
    }

    [Fact]
    public async Task ProjectionGlobalSearch_KeepsPendingTypedValue_OnUnchangedParameterRerender() {
        IRenderedComponent<FcProjectionGlobalSearch> cut = Render<FcProjectionGlobalSearch>(parameters => parameters
            .Add(static p => p.ViewKey, "acme:OrdersProjection")
            .Add(static p => p.InitialValue, "Acme"));

        Task pendingChange = InvokeChangeAsync(cut.Instance, "Acme Corp");

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme Corp");

        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> {
            [nameof(FcProjectionGlobalSearch.ViewKey)] = "acme:OrdersProjection",
            [nameof(FcProjectionGlobalSearch.InitialValue)] = "Acme",
        })));

        GetPrivateField<string>(cut.Instance, "_value").ShouldBe("Acme Corp");

        _time.Advance(TimeSpan.FromMilliseconds(300));
        await pendingChange;

        _dispatcher.Received(1).Dispatch(Arg.Is<GlobalSearchChangedAction>(action =>
            action.ViewKey == "acme:OrdersProjection"
            && action.Query == "Acme Corp"));
    }

    private static Task InvokeChangeAsync(object component, string value) {
        MethodInfo? method = component.GetType().GetMethod(
            "OnValueChangedAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        method.ShouldNotBeNull();
        object? result = method.Invoke(component, [value]);
        result.ShouldBeAssignableTo<Task>();
        return (Task)result!;
    }

    private static T GetPrivateField<T>(object component, string fieldName) {
        FieldInfo? field = component.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        field.ShouldNotBeNull();
        return (T)field.GetValue(component)!;
    }
}
