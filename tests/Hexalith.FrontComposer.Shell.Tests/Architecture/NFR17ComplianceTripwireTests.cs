using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

/// <summary>
/// Story 3-6 AC6 / NFR17 tripwire. Scans every <c>IStorageService.SetAsync(...)</c> call site
/// under <c>src/Hexalith.FrontComposer.Shell/State/</c> and asserts the persisted argument
/// expression matches one of the NFR17-compliance-whitelisted patterns (no PII / no business
/// data at the framework layer). Regression catcher, not a security guarantee — see story 3-6 G8
/// for the trust boundary.
/// </summary>
/// <remarks>
/// When a new SetAsync call is introduced, update BOTH the NFR17 compliance matrix in
/// story 3-6 Dev Notes AND the <see cref="AllowedArgumentPatterns"/> set below. The set is
/// intentionally small so additions require an explicit review.
/// </remarks>
public sealed class NFR17ComplianceTripwireTests
{
    /// <summary>
    /// Whitelisted argument-expressions passed to <c>SetAsync(key, arg)</c>. Each entry maps to a
    /// known NFR17-compliant persisted type documented in the story's NFR17 compliance matrix.
    /// </summary>
    private static readonly HashSet<string> AllowedArgumentPatterns = new(StringComparer.Ordinal)
    {
        // NavigationPersistenceBlob (Navigation nav blob)
        "blob",
        // GridViewPersistenceBlob (DataGrid per-view blob) — same variable name used in DataGrid effects
        // ImmutableHashSet<string> (CapabilitySeenSet)
        "snapshot",
        // ThemeValue (Theme enum)
        "action.NewTheme",
        // DensityLevel (Density enum, direct write)
        "value",
        // DensityLevel? (Density migration path — migrated legacy value cast to nullable enum).
        // Regex captures up to the inner close-paren of the cast, leaving the '(DensityLevel?' prefix.
        "(DensityLevel?",
    };

    private static readonly Regex SetAsyncCall = new(
        @"(?:storage|_storage)\.SetAsync\s*\(\s*(?<key>[^,]+),\s*(?<arg>[^\)]+?)(?=\s*(?:,\s*[a-zA-Z_]|\)))",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void BlobDoesNotCarryEntityData()
    {
        string stateFolder = LocateStateFolder();
        List<(string file, string argExpr)> violations = [];

        foreach (string csFile in Directory.EnumerateFiles(stateFolder, "*.cs", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(csFile);
            foreach (Match match in SetAsyncCall.Matches(content))
            {
                string arg = match.Groups["arg"].Value.Trim();
                if (!AllowedArgumentPatterns.Contains(arg))
                {
                    violations.Add((csFile, arg));
                }
            }
        }

        violations.ShouldBeEmpty(
            "NFR17 tripwire — every IStorageService.SetAsync call under State/ must pass an allow-listed "
            + "argument expression. If a new persisted type was intentionally added, update the NFR17 "
            + "compliance matrix in story 3-6 AND the AllowedArgumentPatterns set in this test. "
            + "Violations: "
            + string.Join(", ", violations.Select(v => $"{Path.GetFileName(v.file)} → '{v.argExpr}'")));
    }

    [Fact]
    public void SetAsyncCallSiteCount_MatchesExpected()
    {
        // A second tripwire: the number of SetAsync call sites should not grow silently. If a PR
        // adds a new call site, this count must be bumped AND the argument pattern whitelisted.
        const int expectedCallSites = 6;
        string stateFolder = LocateStateFolder();
        int actualCallSites = Directory.EnumerateFiles(stateFolder, "*.cs", SearchOption.AllDirectories)
            .Sum(f => SetAsyncCall.Matches(File.ReadAllText(f)).Count);

        actualCallSites.ShouldBe(expectedCallSites,
            $"SetAsync call site count changed from {expectedCallSites} to {actualCallSites}. "
            + "Bump the expected count AND update the NFR17 compliance matrix + AllowedArgumentPatterns.");
    }

    private static string LocateStateFolder()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "src", "Hexalith.FrontComposer.Shell", "State");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException(
            "Could not locate src/Hexalith.FrontComposer.Shell/State/ from test base directory.");
    }
}
