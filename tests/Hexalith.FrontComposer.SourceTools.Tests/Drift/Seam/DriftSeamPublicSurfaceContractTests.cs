using System.Reflection;
using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Seam;

/// <summary>
/// AC17 / T1 + T3 — comparison seam stays internal. Story 9-1 must NOT introduce any public
/// CLI verb, command UX, code fix, source rewrite, or public package API. Story 9-2 owns the
/// public CLI; in 9-1 the seam is exposed only via <c>InternalsVisibleTo</c>. Reflective
/// contract test: every newly-introduced drift type that ships in
/// <c>Hexalith.FrontComposer.SourceTools</c> must be <see cref="Type.IsNotPublic"/>.
/// </summary>
public sealed class DriftSeamPublicSurfaceContractTests {
    private const string SkipReason = "RED-PHASE: T1 + T3 + T4 — drift surface introduction pending.";

    private static readonly string[] ForbiddenPublicTypeSubstrings = [
        "Drift",
        "BaselineCli",
        "BaselineMigration",
        "BaselineUpdate",
    ];

    [Fact(Skip = SkipReason)]
    public void NoPublicTypeWithDriftKeyword_ShipsInSourceToolsAssembly() {
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type[] publicDriftTypes = [.. sourceTools.GetTypes()
            .Where(t => t.IsPublic
                     && ForbiddenPublicTypeSubstrings.Any(token => t.Name.Contains(token, StringComparison.Ordinal)))];

        publicDriftTypes.ShouldBeEmpty(
            $"AC17 — no drift-related type may be public in 9-1: found {string.Join(", ", publicDriftTypes.Select(t => t.FullName))}.");
    }

    [Fact(Skip = SkipReason)]
    public void NoPublicCommandLineEntryPoint_ShipsInSourceToolsAssembly() {
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type[] cliTypes = [.. sourceTools.GetTypes()
            .Where(t => t.IsPublic
                     && (t.GetMethods().Any(m => m.Name == "Main" && m.GetParameters().Length == 1
                                              && m.GetParameters()[0].ParameterType == typeof(string[]))
                      || t.GetCustomAttributes().Any(a => a.GetType().Name.Contains("Command", StringComparison.Ordinal))))];

        cliTypes.ShouldBeEmpty(
            $"AC17 — Story 9-2 owns the CLI; 9-1 must not ship public command entry points: {string.Join(", ", cliTypes.Select(t => t.FullName))}.");
    }

    [Fact(Skip = SkipReason)]
    public void DriftComparisonService_IsAccessibleOnlyViaInternalsVisibleTo() {
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type? service = sourceTools.GetTypes().FirstOrDefault(t => t.Name == "DriftComparisonService");
        service.ShouldNotBeNull("AC17 — the seam must exist as an internal type once T3 lands.");
        service!.IsPublic.ShouldBeFalse("AC17 — the seam must remain internal.");

        // The test-assembly access is granted via [InternalsVisibleTo("Hexalith.FrontComposer.SourceTools.Tests")].
        IEnumerable<InternalsVisibleToAttribute> ivt = sourceTools.GetCustomAttributes<InternalsVisibleToAttribute>();
        ivt.Any(a => a.AssemblyName.StartsWith("Hexalith.FrontComposer.SourceTools.Tests", StringComparison.Ordinal))
            .ShouldBeTrue("AC17 — InternalsVisibleTo must be wired so the test project can reach the seam.");
    }
}
