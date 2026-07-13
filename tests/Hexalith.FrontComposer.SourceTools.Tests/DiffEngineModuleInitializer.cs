using System.Runtime.CompilerServices;

namespace Hexalith.FrontComposer.SourceTools.Tests;

/// <summary>
/// Disables DiffEngine's diff-tool launcher for the entire test assembly.
/// </summary>
internal static class DiffEngineModuleInitializer
{
    /// <summary>
    /// Runs before any test in this assembly executes. REL-2 decision D1: the shared reusable
    /// Hexalith.Builds <c>domain-ci.yml</c> exposes no input to set the <c>DiffEngine_Disabled</c>
    /// environment variable, so a Verify snapshot mismatch would otherwise launch an interactive
    /// diff tool and hang the runner. Disabling DiffEngine in code makes that impossible under the
    /// reusable CI lane, the supplemental quality workflow, and local runs alike — independent of
    /// any per-step environment variable.
    /// </summary>
    [ModuleInitializer]
    public static void Initialize() => DiffEngine.DiffRunner.Disabled = true;
}
