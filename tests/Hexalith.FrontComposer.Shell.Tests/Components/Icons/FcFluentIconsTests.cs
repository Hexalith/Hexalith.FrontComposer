using Hexalith.FrontComposer.Shell.Components.Icons;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Icons;

public sealed class FcFluentIconsTests {
    [Fact]
    public void FactoryMethods_ReturnBlazorFluentUiV5TypedIcons() {
        (string Name, Icon Icon)[] icons = [
            ($"{nameof(FcFluentIcons.Apps20)} regular", FcFluentIcons.Apps20()),
            ($"{nameof(FcFluentIcons.Apps20)} filled", FcFluentIcons.Apps20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.ChevronRight20)} regular", FcFluentIcons.ChevronRight20()),
            ($"{nameof(FcFluentIcons.ChevronRight20)} filled", FcFluentIcons.ChevronRight20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.DocumentSearch32)} regular", FcFluentIcons.DocumentSearch32()),
            ($"{nameof(FcFluentIcons.DocumentSearch32)} filled", FcFluentIcons.DocumentSearch32(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.DocumentSearch48)} regular", FcFluentIcons.DocumentSearch48()),
            ($"{nameof(FcFluentIcons.DocumentSearch48)} filled", FcFluentIcons.DocumentSearch48(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.DevMode20)} regular", FcFluentIcons.DevMode20()),
            ($"{nameof(FcFluentIcons.DevMode20)} filled", FcFluentIcons.DevMode20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.Play16)} regular", FcFluentIcons.Play16()),
            ($"{nameof(FcFluentIcons.Play16)} filled", FcFluentIcons.Play16(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.Search20)} regular", FcFluentIcons.Search20()),
            ($"{nameof(FcFluentIcons.Search20)} filled", FcFluentIcons.Search20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.Settings20)} regular", FcFluentIcons.Settings20()),
            ($"{nameof(FcFluentIcons.Settings20)} filled", FcFluentIcons.Settings20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.Navigation20)} regular", FcFluentIcons.Navigation20()),
            ($"{nameof(FcFluentIcons.Navigation20)} filled", FcFluentIcons.Navigation20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.BuildingPeople20)} regular", FcFluentIcons.BuildingPeople20()),
            ($"{nameof(FcFluentIcons.BuildingPeople20)} filled", FcFluentIcons.BuildingPeople20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.People20)} regular", FcFluentIcons.People20()),
            ($"{nameof(FcFluentIcons.People20)} filled", FcFluentIcons.People20(IconVariant.Filled)),
            ($"{nameof(FcFluentIcons.PersonBoard20)} regular", FcFluentIcons.PersonBoard20()),
            ($"{nameof(FcFluentIcons.PersonBoard20)} filled", FcFluentIcons.PersonBoard20(IconVariant.Filled)),
            (nameof(FcFluentIcons.Checkmark16), FcFluentIcons.Checkmark16()),
            (nameof(FcFluentIcons.CheckmarkCircle16), FcFluentIcons.CheckmarkCircle16()),
            (nameof(FcFluentIcons.DismissCircle16), FcFluentIcons.DismissCircle16()),
            (nameof(FcFluentIcons.InfoCircle16), FcFluentIcons.InfoCircle16()),
            (nameof(FcFluentIcons.SubtractCircle16), FcFluentIcons.SubtractCircle16()),
            (nameof(FcFluentIcons.QuestionCircle16), FcFluentIcons.QuestionCircle16()),
            (nameof(FcFluentIcons.Warning16), FcFluentIcons.Warning16()),
            (nameof(FcFluentIcons.ArrowSync16), FcFluentIcons.ArrowSync16()),
            (nameof(FcFluentIcons.Star16), FcFluentIcons.Star16()),
            (nameof(FcFluentIcons.Edit16), FcFluentIcons.Edit16()),
            (nameof(FcFluentIcons.Eye16), FcFluentIcons.Eye16()),
            (nameof(FcFluentIcons.Key16), FcFluentIcons.Key16()),
            (nameof(FcFluentIcons.Copy16), FcFluentIcons.Copy16()),
        ];

        foreach ((string name, Icon icon) in icons) {
            string? typeName = icon.GetType().FullName;

            typeName.ShouldNotBeNull();
            typeName!.StartsWith(
                "Microsoft.FluentUI.AspNetCore.Components.Icons.",
                StringComparison.Ordinal)
                .ShouldBeTrue($"{name} must use a typed Blazor Fluent UI v5 icon.");
        }
    }
}
