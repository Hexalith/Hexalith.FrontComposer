// Story 3-3 Task 10.4a (Winston review — ADR-039 reducer-purity invariant enforcement).
// Guards against a future contributor moving DensityPrecedence.Resolve inside a reducer, which
// would require DI in a Fluxor reducer and silently break the pure-function contract.

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Story 3-3 Task 10.4a — reducer-level lint enforcing ADR-039 purity invariant. Reads the
/// <c>DensityReducers.cs</c> source as text and asserts <c>DensityPrecedence.Resolve</c> does NOT
/// appear anywhere in the file. Action producers run the resolver and carry the pre-resolved
/// effective density in the action payload; reducers stay pure static methods.
/// </summary>
public sealed class DensityReducerPurityTest {
    [Fact]
    public void DensityReducersDoNotInvokeResolver() {
        DirectoryInfo? shellDir = LocateShellSourceDirectory();
        shellDir.ShouldNotBeNull("Could not locate src/Hexalith.FrontComposer.Shell from the test working directory.");

        string reducerPath = Path.Combine(shellDir!.FullName, "State", "Density", "DensityReducers.cs");
        File.Exists(reducerPath).ShouldBeTrue($"DensityReducers.cs not found at {reducerPath}.");

        string content = File.ReadAllText(reducerPath);
        content.ShouldNotContain(
            "DensityPrecedence.Resolve",
            Shouldly.Case.Sensitive,
            "ADR-039 purity violation — reducers must not invoke the resolver. Move the compute "
                + "to the action producer and carry the pre-resolved value in the action payload.");
    }

    private static DirectoryInfo? LocateShellSourceDirectory() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            string candidate = Path.Combine(dir.FullName, "src", "Hexalith.FrontComposer.Shell");
            if (Directory.Exists(candidate)) {
                return new DirectoryInfo(candidate);
            }

            dir = dir.Parent;
        }

        return null;
    }
}
