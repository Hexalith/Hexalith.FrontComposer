using System.Reflection;

using Hexalith.FrontComposer.Contracts.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Diagnostics;

public sealed class FcDiagnosticIdsCompatibilityTests
{
    [Fact]
    public void Hfc2106_PreferredName_RetainsIdentifier()
    {
        FcDiagnosticIds.HFC2106_PreferenceHydrationFallback.ShouldBe("HFC2106");
    }

    [Fact]
    public void Hfc2106_LegacyName_RemainsPublicObsoleteAlias()
    {
        FieldInfo field = typeof(FcDiagnosticIds).GetField(
            "HFC2106_ThemeHydrationEmpty",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new ShouldAssertException("The public HFC2106 compatibility alias was removed.");

        field.IsLiteral.ShouldBeTrue();
        field.GetRawConstantValue().ShouldBe(FcDiagnosticIds.HFC2106_PreferenceHydrationFallback);

        ObsoleteAttribute obsolete = field.GetCustomAttribute<ObsoleteAttribute>()
            ?? throw new ShouldAssertException("The HFC2106 compatibility alias must be marked obsolete.");

        obsolete.IsError.ShouldBeFalse();
        obsolete.Message.ShouldNotBeNull();
        obsolete.Message.ShouldContain(nameof(FcDiagnosticIds.HFC2106_PreferenceHydrationFallback));
    }
}
