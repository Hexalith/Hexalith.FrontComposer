using System.Collections.ObjectModel;
using System.Diagnostics;

using Hexalith.FrontComposer.Contracts.DevMode;

namespace Hexalith.FrontComposer.Contracts.Diagnostics;

/// <summary>
/// Metadata-only diagnostic shared by SourceTools, runtime customization hosts, and dev-mode surfaces.
/// </summary>
/// <remarks>
/// The contract intentionally carries strings, enum values, and sanitized structured properties only.
/// It must not capture exception objects, render fragments, scoped services, tenant/user identifiers,
/// item payloads, field values, or localized runtime strings.
/// </remarks>
public sealed record CustomizationDiagnostic {
    private static readonly HashSet<string> ForbiddenPropertyNames = new(StringComparer.OrdinalIgnoreCase) {
        "tenant",
        "tenantId",
        "user",
        "userId",
        "accessToken",
        "token",
        "item",
        "itemPayload",
        "payload",
        "fieldValue",
        "renderFragment",
        "localizedString",
        "exception",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomizationDiagnostic"/> class.
    /// </summary>
    public CustomizationDiagnostic(
        string Id,
        CustomizationDiagnosticSeverity Severity,
        CustomizationDiagnosticPhase Phase,
        CustomizationLevel Level,
        string? ProjectionTypeName,
        string? ComponentTypeName,
        string? Role,
        string? FieldName,
        string What,
        string Expected,
        string Got,
        string Fix,
        string? Fallback,
        string DocsLink,
        IReadOnlyDictionary<string, string>? Properties = null) {
        RequireSection(Id, nameof(Id));
        RequireSection(What, nameof(What));
        RequireSection(Expected, nameof(Expected));
        RequireSection(Got, nameof(Got));
        RequireSection(Fix, nameof(Fix));
        RequireSection(DocsLink, nameof(DocsLink));

        this.Id = Id;
        this.Severity = Severity;
        this.Phase = Phase;
        this.Level = Level;
        this.ProjectionTypeName = ProjectionTypeName;
        this.ComponentTypeName = ComponentTypeName;
        this.Role = Role;
        this.FieldName = FieldName;
        this.What = What;
        this.Expected = Expected;
        this.Got = Got;
        this.Fix = Fix;
        this.Fallback = string.IsNullOrWhiteSpace(Fallback) ? null : Fallback;
        this.DocsLink = DocsLink;
        this.Properties = new ReadOnlyDictionary<string, string>(SanitizeProperties(Properties));
    }

    /// <summary>Stable HFC diagnostic identifier.</summary>
    public string Id { get; }

    /// <summary>Diagnostic severity without Roslyn or logging dependencies.</summary>
    public CustomizationDiagnosticSeverity Severity { get; }

    /// <summary>Phase where the diagnostic was produced.</summary>
    public CustomizationDiagnosticPhase Phase { get; }

    /// <summary>Customization level associated with the diagnostic.</summary>
    public CustomizationLevel Level { get; }

    /// <summary>Sanitized projection type name, when known.</summary>
    public string? ProjectionTypeName { get; }

    /// <summary>Sanitized component type name, when known.</summary>
    public string? ComponentTypeName { get; }

    /// <summary>Projection role or role label, when applicable.</summary>
    public string? Role { get; }

    /// <summary>Generated field name, when the diagnostic applies to a single field seam.</summary>
    public string? FieldName { get; }

    /// <summary>What happened.</summary>
    public string What { get; }

    /// <summary>Expected behavior or contract.</summary>
    public string Expected { get; }

    /// <summary>Observed behavior or contract.</summary>
    public string Got { get; }

    /// <summary>Actionable fix.</summary>
    public string Fix { get; }

    /// <summary>Fallback behavior when the framework can degrade safely.</summary>
    public string? Fallback { get; }

    /// <summary>Stable diagnostic documentation URL.</summary>
    public string DocsLink { get; }

    /// <summary>Sanitized structured properties safe for logs and dev diagnostics.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>
    /// Creates a validated diagnostic with the canonical teaching-section shape.
    /// </summary>
    public static CustomizationDiagnostic Create(
        string id,
        CustomizationDiagnosticSeverity severity,
        CustomizationDiagnosticPhase phase,
        CustomizationLevel level,
        string? projectionTypeName,
        string? componentTypeName,
        string? role,
        string? fieldName,
        string what,
        string expected,
        string got,
        string fix,
        string? fallback,
        string docsLink,
        IReadOnlyDictionary<string, string>? properties = null)
        => new(
            id,
            severity,
            phase,
            level,
            projectionTypeName,
            componentTypeName,
            role,
            fieldName,
            what,
            expected,
            got,
            fix,
            fallback,
            docsLink,
            properties);

    private static void RequireSection(string value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Customization diagnostics require non-empty teaching sections.", parameterName);
        }
    }

    private static Dictionary<string, string> SanitizeProperties(IReadOnlyDictionary<string, string>? properties) {
        Dictionary<string, string> sanitized = new(StringComparer.Ordinal);
        if (properties is null) {
            return sanitized;
        }

        foreach (KeyValuePair<string, string> property in properties) {
            // Blank keys are a programming mistake — surface in DEBUG so they aren't lost silently.
            Debug.Assert(
                !string.IsNullOrWhiteSpace(property.Key),
                "CustomizationDiagnostic.Properties received a blank key. Drop the entry at the call site or fix the key name.");
            if (string.IsNullOrWhiteSpace(property.Key) || ForbiddenPropertyNames.Contains(property.Key)) {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(property.Value)) {
                sanitized[property.Key] = property.Value;
            }
        }

        return sanitized;
    }
}
