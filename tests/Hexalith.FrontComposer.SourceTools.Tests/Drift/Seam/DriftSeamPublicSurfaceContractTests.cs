using System.Reflection;
using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Seam;

/// <summary>
/// AC17 / T1 + T3 + T4 — comparison seam stays internal.
/// </summary>
public sealed class DriftSeamPublicSurfaceContractTests {
    private static readonly string[] ForbiddenPublicTypeSubstrings = [
        "Drift",
        "BaselineCli",
        "BaselineMigration",
        "BaselineUpdate",
        // CL-2 — extend the advisory-leakage list.
        "TrimAotAdvisor",
        "ReflectionCatalogAdvisor",
    ];

    [Fact]
    public void NoPublicTypeWithDriftKeyword_ShipsInSourceToolsAssembly() => AssertNoForbiddenPublicTypes(typeof(DomainModel).Assembly);

    [Fact]
    public void NoPublicTypeWithDriftKeyword_ShipsInContractsAssembly() =>
        // CM-17 — extend the seam check to the Contracts assembly.
        AssertNoForbiddenPublicTypes(typeof(FcDiagnosticIds).Assembly);

    [Fact]
    public void NoPublicCommandLineEntryPoint_ShipsInSourceToolsAssembly() {
        // CH-4 — tighten detection: only Main(string[]) entry points and System.CommandLine
        // base types count. Random `Command` substrings on attribute names are fine.
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type[] cliTypes = [.. sourceTools.GetTypes()
            .Where(t => t.IsPublic && (
                       t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Any(m => m.Name == "Main"
                               && m.GetParameters().Length == 1
                               && m.GetParameters()[0].ParameterType == typeof(string[]))
                    || HasSystemCommandLineBase(t)))];

        cliTypes.ShouldBeEmpty(
            $"AC17 — Story 9-2 owns the CLI; 9-1 must not ship public command entry points: {string.Join(", ", cliTypes.Select(t => t.FullName))}.");
    }

    [Fact]
    public void NoPublicCodeFixOrRefactoringProvider_ShipsInSourceToolsAssembly() {
        // CH-19 — AC17 forbids "code fixes" and "source rewrites".
        // Detect by base-type FullName to avoid pulling Microsoft.CodeAnalysis.Workspaces into the
        // test project just for `typeof(CodeFixProvider)`.
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type[] forbidden = [.. sourceTools.GetTypes()
            .Where(t => t.IsPublic && InheritsFrom(t,
                "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider",
                "Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider"))];

        forbidden.ShouldBeEmpty(
            $"AC17 — Story 9-1 must not ship public CodeFixProvider/CodeRefactoringProvider types: {string.Join(", ", forbidden.Select(t => t.FullName))}.");
    }

    private static bool InheritsFrom(Type t, params string[] baseFullNames) {
        Type? cur = t.BaseType;
        while (cur is not null) {
            if (cur.FullName is string name && Array.IndexOf(baseFullNames, name) >= 0) {
                return true;
            }

            cur = cur.BaseType;
        }

        return false;
    }

    [Fact]
    public void DriftComparisonService_IsAccessibleOnlyViaInternalsVisibleTo() {
        // CH-5 — once the type is internal-but-IVT-accessible, reference it via Type.GetType
        // by qualified name so a rename surfaces as a clean type-not-found failure rather than
        // a brittle Name-string match.
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type? service = sourceTools.GetType("Hexalith.FrontComposer.SourceTools.Drift.DriftComparisonService", throwOnError: false);
        _ = service.ShouldNotBeNull("AC17 — DriftComparisonService must exist as an internal type once T3 lands.");
        service!.IsPublic.ShouldBeFalse("AC17 — the seam must remain internal.");
        service.IsSealed.ShouldBeTrue(
            "AC17 — DriftComparisonService should be sealed so InternalsVisibleTo grantees cannot subclass and bypass.");

        // CH-18 — exact assembly-name match for the test grant; avoids over-permissive prefix.
        InternalsVisibleToAttribute[] ivt = [.. sourceTools.GetCustomAttributes<InternalsVisibleToAttribute>()];
        ivt.Any(a => a.AssemblyName == "Hexalith.FrontComposer.SourceTools.Tests")
            .ShouldBeTrue("AC17 — InternalsVisibleTo must grant the test assembly by exact name.");
    }

    [Fact]
    public void DriftComparisonService_ExposesExpectedCompareSignature() {
        // CM-19 — pin Story 9-2's seam contract: Compare(DriftCurrentSnapshot, DriftBaselineSet)
        // returning DriftComparisonResult. Renames or signature drift surface here.
        Assembly sourceTools = typeof(DomainModel).Assembly;
        Type? service = sourceTools.GetType("Hexalith.FrontComposer.SourceTools.Drift.DriftComparisonService", throwOnError: false);
        _ = service.ShouldNotBeNull();
        Type? snapshot = sourceTools.GetType("Hexalith.FrontComposer.SourceTools.Drift.DriftCurrentSnapshot", throwOnError: false);
        Type? baseline = sourceTools.GetType("Hexalith.FrontComposer.SourceTools.Drift.DriftBaselineSet", throwOnError: false);
        Type? result = sourceTools.GetType("Hexalith.FrontComposer.SourceTools.Drift.DriftComparisonResult", throwOnError: false);
        _ = snapshot.ShouldNotBeNull();
        _ = baseline.ShouldNotBeNull();
        _ = result.ShouldNotBeNull();

        MethodInfo? compare = service!
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == "Compare"
                              && m.GetParameters().Length == 2
                              && m.GetParameters()[0].ParameterType == snapshot
                              && m.GetParameters()[1].ParameterType == baseline);
        _ = compare.ShouldNotBeNull("AC17 — Compare(DriftCurrentSnapshot, DriftBaselineSet) overload is the seam contract for Story 9-2.");
        compare!.ReturnType.ShouldBe(result, "AC17 — Compare must return DriftComparisonResult.");
    }

    [Fact]
    public void NoPublicMethodSignature_LeaksInternalDriftType_FromSourceToolsAssembly() {
        // CH-16 + CH-17 — flag any public method whose parameter or return type is internally
        // declared in the SourceTools assembly. Catches generic-parameter leakage and overload
        // resolution surface even after substring renames.
        Assembly sourceTools = typeof(DomainModel).Assembly;
        IEnumerable<Type> publicTypes = sourceTools.GetTypes().Where(t => t.IsPublic);

        List<string> leakages = [];
        foreach (Type t in publicTypes) {
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                IEnumerable<Type> signatureTypes = m.GetParameters().Select(p => p.ParameterType).Concat([m.ReturnType]);
                foreach (Type sigType in signatureTypes) {
                    foreach (Type referenced in EnumerateContainedTypes(sigType)) {
                        if (referenced.Assembly == sourceTools && !referenced.IsPublic && !referenced.IsNestedPublic) {
                            leakages.Add($"{t.FullName}.{m.Name} surfaces internal type {referenced.FullName}");
                        }
                    }
                }
            }
        }

        leakages.ShouldBeEmpty("AC17 — public method signatures must not surface internally declared SourceTools types.");
    }

    private static void AssertNoForbiddenPublicTypes(Assembly assembly) {
        Type[] publicForbiddenTypes = [.. assembly.GetTypes()
            .Where(t => t.IsPublic
                     && ForbiddenPublicTypeSubstrings.Any(token => t.Name.Contains(token, StringComparison.Ordinal)))];

        publicForbiddenTypes.ShouldBeEmpty(
            $"AC17 — no drift-related type may be public in 9-1: found {string.Join(", ", publicForbiddenTypes.Select(t => t.FullName))}.");
    }

    private static bool HasSystemCommandLineBase(Type t) {
        Type? cur = t;
        while (cur is not null) {
            if (cur.FullName?.StartsWith("System.CommandLine.", StringComparison.Ordinal) == true) {
                return true;
            }

            cur = cur.BaseType;
        }

        return false;
    }

    private static IEnumerable<Type> EnumerateContainedTypes(Type t) {
        yield return t;
        if (t.IsGenericType) {
            foreach (Type arg in t.GetGenericArguments()) {
                foreach (Type inner in EnumerateContainedTypes(arg)) {
                    yield return inner;
                }
            }
        }

        if (t.IsArray) {
            Type? elem = t.GetElementType();
            if (elem is not null) {
                foreach (Type inner in EnumerateContainedTypes(elem)) {
                    yield return inner;
                }
            }
        }
    }
}
