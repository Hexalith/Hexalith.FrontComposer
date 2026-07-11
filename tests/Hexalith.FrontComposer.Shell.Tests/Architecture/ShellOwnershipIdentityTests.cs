using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

public sealed class ShellOwnershipIdentityTests {
    [Fact]
    public void MovedTypes_RelocatedToShell_UseExactAssemblyQualifiedIdentities() {
        Type[] movedTypes = [
            typeof(FcShellOptions),
            typeof(FcShellDevModeOptions),
            typeof(CustomizationContractValidationMode),
            typeof(InlinePopoverRegistry),
            typeof(CaptureGridStateAction),
            typeof(RestoreGridStateAction),
            typeof(ClearGridStateAction),
            typeof(PruneExpiredAction),
            typeof(ColumnFilterChangedAction),
            typeof(StatusFilterToggledAction),
            typeof(GlobalSearchChangedAction),
            typeof(SortChangedAction),
            typeof(FiltersResetAction),
            typeof(LoadPageAction),
            typeof(LoadPageSucceededAction),
            typeof(LoadPageNotModifiedAction),
            typeof(LoadPageFailedAction),
            typeof(LoadPageCancelledAction),
            typeof(ClearPendingPagesAction),
            typeof(ColumnVisibilityChangedAction),
            typeof(ResetColumnVisibilityAction),
            typeof(ScrollCapturedAction),
            typeof(ExpandRowAction),
            typeof(CollapseRowAction),
        ];
        string[] expected = [
            "Hexalith.FrontComposer.Shell.Options.FcShellOptions, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.Options.FcShellDevModeOptions, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.Options.CustomizationContractValidationMode, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.Services.InlinePopoverRegistry, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.CaptureGridStateAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.RestoreGridStateAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ClearGridStateAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.PruneExpiredAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ColumnFilterChangedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.StatusFilterToggledAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.GlobalSearchChangedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.SortChangedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.FiltersResetAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageSucceededAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageNotModifiedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageFailedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadPageCancelledAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ClearPendingPagesAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ColumnVisibilityChangedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ResetColumnVisibilityAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.DataGridNavigation.ScrollCapturedAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandRowAction, Hexalith.FrontComposer.Shell",
            "Hexalith.FrontComposer.Shell.State.ExpandedRow.CollapseRowAction, Hexalith.FrontComposer.Shell",
        ];

        movedTypes.Select(type => $"{type.FullName}, {type.Assembly.GetName().Name}")
            .ShouldBe(expected);
    }
}
