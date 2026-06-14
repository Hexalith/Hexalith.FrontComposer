namespace Hexalith.FrontComposer.Contracts.Conformance;

/// <summary>
/// Authoritative cross-story contract for FrontComposer source-generator output paths.
/// Story 9-2 owns the generator's emit layout; Story 9-3 (IDE parity), the matrix, evidence
/// manifests, CLI inspection, and adopter docs all consume this single constant rather than
/// re-declaring the template string.
/// </summary>
/// <remarks>
/// Adopters who script around generated output (CI gates, doc generators, IDE tooling) should
/// reference <see cref="Template"/> and <see cref="BuildProjectRelativePath"/> here instead of
/// hardcoding the obj-tree path. <see cref="Version"/> bumps whenever the layout changes; any
/// bump invalidates IDE-parity evidence manifests and triggers Story 9-3 revalidation.
/// </remarks>
public static class GeneratedOutputPathContract {
    /// <summary>The semantic version of the generated-output layout. Bumping invalidates evidence manifests.</summary>
    public const string Version = "v1";

    /// <summary>The project-relative template; placeholders are <c>{Config}</c>, <c>{TFM}</c>, and <c>{TypeName}</c>.</summary>
    public const string Template = "obj/{Config}/{TFM}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs";

    /// <summary>The directory portion of <see cref="Template"/> that adopter docs reference.</summary>
    public const string Directory = "obj/{Config}/{TFM}/generated/HexalithFrontComposer";

    private static readonly char[] _forbiddenFilenameChars =
    {
        '\0', '/', '\\', ':', '*', '?', '"', '<', '>', '|',
    };

    private static readonly string[] _windowsReservedDeviceNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// Builds the project-relative generated-output path for a given configuration, target framework, and file name.
    /// Always returns forward-slash form so the result is stable across operating systems and safe to compare in
    /// reports, manifests, and CLI output.
    /// </summary>
    /// <param name="configuration">MSBuild Configuration (e.g. <c>Debug</c>, <c>Release</c>). Required, no separators.</param>
    /// <param name="targetFramework">Target framework moniker (e.g. <c>net10.0</c>). Required, no separators.</param>
    /// <param name="generatedFileName">
    /// Generated file name including suffix (e.g. <c>Acme.Foo.g.razor.cs</c>). Must not include path separators,
    /// path-traversal segments, NUL/control characters, NTFS alternate-data-stream colons, or Windows reserved
    /// device names. Caller validates structure; this method validates safety.
    /// </param>
    public static string BuildProjectRelativePath(string configuration, string targetFramework, string generatedFileName) {
        ValidateSegment(configuration, nameof(configuration));
        ValidateSegment(targetFramework, nameof(targetFramework));
        ValidateFilename(generatedFileName, nameof(generatedFileName));

        return string.Join(
            "/",
            "obj",
            configuration,
            targetFramework,
            "generated",
            "HexalithFrontComposer",
            generatedFileName);
    }

    private static void ValidateSegment(string value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        if (value.IndexOfAny(_forbiddenFilenameChars) >= 0
            || value.Equals(".", StringComparison.Ordinal)
            || value.Equals("..", StringComparison.Ordinal)) {
            throw new ArgumentException($"{parameterName} must not contain path separators, control characters, or traversal segments.", parameterName);
        }
    }

    private static void ValidateFilename(string value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        if (value.IndexOfAny(_forbiddenFilenameChars) >= 0) {
            throw new ArgumentException($"{parameterName} must not contain path separators, NUL, NTFS ADS colons, or wildcard characters.", parameterName);
        }

        if (value.Contains("..")) {
            throw new ArgumentException($"{parameterName} must not contain traversal segments.", parameterName);
        }

        for (int i = 0; i < value.Length; i++) {
            if (char.IsControl(value, i)) {
                throw new ArgumentException($"{parameterName} must not contain control characters.", parameterName);
            }
        }

        string baseName = value;
        int dot = value.IndexOf('.');

        if (dot > 0) {
            baseName = value.Substring(0, dot);
        }

        for (int i = 0; i < _windowsReservedDeviceNames.Length; i++) {
            if (baseName.Equals(_windowsReservedDeviceNames[i], StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException($"{parameterName} must not begin with a Windows reserved device name ('{baseName}').", parameterName);
            }
        }
    }
}
