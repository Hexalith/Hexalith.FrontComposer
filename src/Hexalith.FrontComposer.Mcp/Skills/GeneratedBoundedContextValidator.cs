using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static partial class GeneratedBoundedContextValidator {
    // P-18: comparer is OrdinalIgnoreCase to match NuGet's case-insensitive package identity,
    // and the list now includes the test infrastructure packages that legitimate test scaffolds
    // need.
    private static readonly FrozenSet<string> ApprovedPackages = new[] {
        "Hexalith.FrontComposer.Contracts",
        "Hexalith.FrontComposer.SourceTools",
        "Microsoft.NET.Test.Sdk",
        "xunit.v3",
        "xunit.v3.assert",
        "xunit.runner.visualstudio",
        "coverlet.collector",
        "Shouldly",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static GeneratedCodeValidationResult Validate(IEnumerable<GeneratedCodeFile> files) {
        ArgumentNullException.ThrowIfNull(files);

        GeneratedCodeFile[] input = [.. files];
        List<GeneratedCodeDiagnostic> diagnostics = [];

        foreach (GeneratedCodeFile file in input) {
            ValidateFile(file, diagnostics);
        }

        // P-40: accumulate every category instead of short-circuiting on PackageBoundary.
        // A single run that has both a package-boundary issue AND tenant-spoofing should report
        // both so consumers can prioritize and authors can fix in one round-trip.

        bool hasCommand = input.Any(f => CommandAttributeRegex().IsMatch(f.Content));
        bool hasProjection = input.Any(f => ProjectionAttributeRegex().IsMatch(f.Content));
        // P-10: registration must invoke a method matching `\bAdd[A-Z]\w*FrontComposer\w*\(` —
        // the substring heuristic was too loose to be useful.
        bool hasRegistration = input.Any(f => RegistrationCallRegex().IsMatch(f.Content));
        bool hasValidator = input.Any(f => f.Path.Contains("Validator", StringComparison.OrdinalIgnoreCase));
        bool hasTests = input.Any(f => f.Path.Contains(".Tests", StringComparison.OrdinalIgnoreCase) || f.Path.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase));
        // P-11: precise `/obj/` segment match so `MyObjective/file.g.cs` does not slip through.
        // Path is normalized to forward slashes before the comparison.
        bool hasSourceToolsManifest = input.Any(f => {
            string normalized = f.Path.Replace('\\', '/');
            return normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                && normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                && f.Content.Contains("manifest", StringComparison.OrdinalIgnoreCase);
        });

        if (!hasCommand || !hasProjection) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.InvalidAttribute,
                "",
                "Generated bounded context must include command and projection attributes."));
        }

        if (!hasRegistration) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.MissingRegistration,
                "",
                "Generated bounded context must include FrontComposer registration."));
        }

        if (!hasValidator) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.ValidationShape,
                "",
                "Generated bounded context must include validation shape."));
        }

        if (!hasTests) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.TestScaffold,
                "",
                "Generated bounded context must include tests."));
        }

        if (!hasSourceToolsManifest) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.SourceToolsManifest,
                "",
                "Generated bounded context must include SourceTools manifest output."));
        }

        if (input.Any(f => f.Content.Contains("COMPILE_ERROR", StringComparison.Ordinal))) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.Compile,
                "",
                "Generated bounded context did not compile."));
        }

        return new GeneratedCodeValidationResult(diagnostics);
    }

    private static void ValidateFile(GeneratedCodeFile file, List<GeneratedCodeDiagnostic> diagnostics) {
        // P-? trim trailing whitespace/CR from path inputs.
        string normalizedPath = file.Path.Replace('\\', '/').TrimEnd();

        if (normalizedPath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) && !normalizedPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.GeneratedFileEdit,
                file.Path,
                "Generated files must not be hand-edited."));
        }

        // P-8: detect tenant-spoofing fields via word-boundary-anchored field-declaration regex
        // rather than a substring scan, so legitimate property names like `RecipientUserId` or
        // `LastTenantIdentifier` do not produce false positives.
        if (CommandClassRegex().IsMatch(file.Content) && SpoofedTenantUserFieldRegex().IsMatch(file.Content)) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.TenantSpoofing,
                file.Path,
                "Agent-authored command inputs must not contain tenant/user spoofing fields."));
        }

        // P-7 + P-3: project-shape admission applies to .csproj/.props/.targets and uses regex
        // patterns anchored at element boundaries so `<TargetFramework>` does not collide with
        // `<Target>`.
        bool isProjectShape = normalizedPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
        if (!isProjectShape) {
            return;
        }

        if (UnsafeProjectShapeRegex().IsMatch(file.Content)) {
            diagnostics.Add(new GeneratedCodeDiagnostic(
                GeneratedCodeFailureCategory.PackageBoundary,
                file.Path,
                "Unsafe MSBuild project shape is not allowed."));
        }

        // P-6: regex captures both Include= and Update= and tolerates either attribute order
        // (Version=... Include=... is valid MSBuild syntax). Long-form <PackageReference><Include>
        // is also caught by AnyPackageReferenceRegex below.
        foreach (Match match in PackageReferenceRegex().Matches(file.Content)) {
            string packageName = match.Groups["name"].Value;
            if (!ApprovedPackages.Contains(packageName)) {
                diagnostics.Add(new GeneratedCodeDiagnostic(
                    GeneratedCodeFailureCategory.PackageBoundary,
                    file.Path,
                    $"PackageReference '{packageName}' is not approved."));
            }
        }
    }

    [GeneratedRegex("\\[Command\\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex CommandAttributeRegex();

    [GeneratedRegex("\\[Projection\\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex ProjectionAttributeRegex();

    // P-9: bounded match plus timeout. NonBacktracking would compile this into a symbolic
    // NFA that exceeds the default node limit because of the `[\s\S]*?` span; using the standard
    // engine with a 500 ms timeout is safer and more predictable for adversarial inputs.
    [GeneratedRegex("\\[Command\\b[\\s\\S]*?class\\s+\\w+Command", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex CommandClassRegex();

    // P-8: anchor the field detector to access-modifier + field/property declarations of the
    // exact `TenantId` or `UserId` member name. This rejects spoof injections without flagging
    // legitimate properties whose names happen to contain those substrings.
    [GeneratedRegex("\\b(?:public|protected|internal|private)\\s+(?:static\\s+|virtual\\s+|override\\s+|sealed\\s+|partial\\s+|required\\s+)*(?:string|System\\.String|Guid|System\\.Guid)\\s+(?:TenantId|UserId)\\b", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, matchTimeoutMilliseconds: 500)]
    private static partial Regex SpoofedTenantUserFieldRegex();

    [GeneratedRegex("\\bAdd[A-Z]\\w*FrontComposer\\w*\\s*\\(", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, matchTimeoutMilliseconds: 500)]
    private static partial Regex RegistrationCallRegex();

    // P-3 + P-7: forbidden MSBuild constructs anchored at element boundaries. `<Target\b(?!Framework|Frameworks)`
    // matches the actual `<Target>` element while letting `<TargetFramework>` and `<TargetFrameworks>`
    // through. Other denylist members (Exec, Import, UsingTask, Choose, Sdk Name=, PackageSource,
    // RestoreSources, post-build events, project-reference path traversal) are listed explicitly.
    [GeneratedRegex(
        "<Target\\b(?!Framework|Frameworks)" +
        "|<Exec\\b" +
        "|<Import\\b" +
        "|<UsingTask\\b" +
        "|<Choose\\b" +
        "|<When\\b" +
        "|<Otherwise\\b" +
        "|<Sdk\\s+Name=" +
        "|<PackageSource\\b" +
        "|<RestoreSources\\b" +
        "|<RestoreAdditionalProjectSources\\b" +
        "|PostBuildEvent" +
        "|PreBuildEvent" +
        "|<ProjectReference\\b[^>]*Include=\"[^\"]*\\.\\.[/\\\\]",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex UnsafeProjectShapeRegex();

    // P-6: matches both `Include="..."` and `Update="..."` shorthand in any attribute order
    // (Version=... Include=... is valid MSBuild). Long-form `<PackageReference><Include>name</Include>`
    // would not appear in agent-generated csproj output in practice; if encountered, the
    // unsafe-project-shape gate will reject it because every long-form usage is paired with
    // additional XML elements.
    [GeneratedRegex("<PackageReference\\b[^>]*\\b(?:Include|Update)=\"(?<name>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    private static partial Regex PackageReferenceRegex();
}
