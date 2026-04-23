using System.Reflection;

using Hexalith.FrontComposer.Shell.Components.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T4.5 / H2 — <see cref="FcProjectionRoutes.StatusFilter"/> indirection tests.
/// Includes null-guard assertions (FMA round 3) and the Enum-signature lock (round 4).
/// </summary>
public sealed class FcProjectionRoutesTests {
    private enum TestStatus { Pending, Submitted, Approved }

    [Fact]
    public void StatusFilterBuildsCorrectUrl() {
        string url = FcProjectionRoutes.StatusFilter("/orders", TestStatus.Pending);
        url.ShouldBe("/orders?filter=status:Pending");
    }

    [Fact]
    public void StatusFilterThrowsOnNullBcRoute() {
        _ = Should.Throw<ArgumentNullException>(
            () => FcProjectionRoutes.StatusFilter(null!, TestStatus.Pending));
    }

    [Fact]
    public void StatusFilterThrowsOnNullStatusValue() {
        _ = Should.Throw<ArgumentNullException>(
            () => FcProjectionRoutes.StatusFilter("/orders", null!));
    }

    [Flags]
    private enum FlagsStatus {
        None = 0,
        Pending = 1,
        Submitted = 2,
    }

    [Fact]
    public void StatusFilterEscapesSpacesAndCommasInCombinedFlagsValue() {
        FlagsStatus combined = FlagsStatus.Pending | FlagsStatus.Submitted;
        string url = FcProjectionRoutes.StatusFilter("/orders", combined);

        // Blame: ToString of a [Flags] value produces "Pending, Submitted" with a comma-space.
        // Uri.EscapeDataString escapes the comma AND the space so the URL parser is safe.
        url.ShouldNotContain("Pending, Submitted");
        url.ShouldContain("%2C");
    }

    [Fact]
    public void StatusFilterRequiresEnumParameter_CompileTimeSignatureLock() {
        MethodInfo? method = typeof(FcProjectionRoutes).GetMethod(
            nameof(FcProjectionRoutes.StatusFilter));
        method.ShouldNotBeNull();
        ParameterInfo[] parameters = method!.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].ParameterType.ShouldBe(typeof(Enum));
    }
}
