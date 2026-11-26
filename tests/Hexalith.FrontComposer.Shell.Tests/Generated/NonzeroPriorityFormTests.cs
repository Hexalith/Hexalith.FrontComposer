using Bunit;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Checks enum defaults without changing caller-owned values or touching the edit context.</summary>
public sealed class NonzeroPriorityFormTests : CommandRendererTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewModelSelectsTheFirstDeclaredMemberWhenZeroIsUndefined(bool useRenderer)
    {
        JSInterop.SetupVoid("Microsoft.FluentUI.Blazor.Components.Select.Initialize", _ => true).SetVoidResult();
        await InitializeStoreAsync();

        IRenderedComponent<NonzeroPriorityCommandForm> form = useRenderer
            ? Render<NonzeroPriorityCommandRenderer>().FindComponent<NonzeroPriorityCommandForm>()
            : Render<NonzeroPriorityCommandForm>();

        GetPriority(form, nameof(NonzeroPriorityCommand.Priority)).ToString().ShouldBe("default");
        form.Find("[name='Priority'] fluent-option[selected]").TextContent.Trim().ShouldBe("Default");
        GetPriority(form, nameof(NonzeroPriorityCommand.Preset)).ShouldBe(NonzeroPriority.Manual);
        form.FindComponent<FluentSelect<NonzeroPriority?, NonzeroPriority?>>().Instance.Value.ShouldBeNull();
        form.FindComponent<FluentSelect<FieldContractPriority, FieldContractPriority>>().Instance.Value.ShouldBe(FieldContractPriority.Low);
        form.Instance.IsDirty.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingModelPreservesCallerSuppliedValues(bool useRenderer)
    {
        JSInterop.SetupVoid("Microsoft.FluentUI.Blazor.Components.Select.Initialize", _ => true).SetVoidResult();
        await InitializeStoreAsync();

        NonzeroPriorityCommand supplied = new() { Priority = NonzeroPriority.Manual, Preset = default, Optional = NonzeroPriority.Manual };
        IRenderedComponent<NonzeroPriorityCommandForm> form = useRenderer
            ? Render<NonzeroPriorityCommandRenderer>(p => p.Add(renderer => renderer.InitialValue, supplied)).FindComponent<NonzeroPriorityCommandForm>()
            : Render<NonzeroPriorityCommandForm>(p => p.Add(component => component.InitialValue, supplied));

        GetPriority(form, nameof(NonzeroPriorityCommand.Priority)).ShouldBe(NonzeroPriority.Manual);
        supplied.Preset.ShouldBe(default(NonzeroPriority));
        form.FindComponent<FluentSelect<NonzeroPriority?, NonzeroPriority?>>().Instance.Value.ShouldBe(NonzeroPriority.Manual);
        form.Instance.IsDirty.ShouldBeFalse();
    }

    private static NonzeroPriority GetPriority(IRenderedComponent<NonzeroPriorityCommandForm> form, string field)
        => form.FindComponents<FluentSelect<NonzeroPriority, NonzeroPriority>>().Single(editor => editor.Instance.Name == field).Instance.Value;
}
