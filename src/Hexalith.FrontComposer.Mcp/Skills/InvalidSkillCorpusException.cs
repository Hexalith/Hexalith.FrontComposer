using System.Globalization;
using System.Text;

namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>Reports that startup validation rejected the skill corpus.</summary>
/// <remarks>The exception message combines operator diagnostics and can contain local paths.</remarks>
public sealed class InvalidSkillCorpusException : Exception {
    /// <summary>Initializes the exception from the validation diagnostics.</summary>
    /// <param name="diagnostics">The diagnostics available to the corpus operator.</param>
    public InvalidSkillCorpusException(IReadOnlyList<SkillCorpusDiagnostic> diagnostics)
        : base(BuildMessage(diagnostics)) => Diagnostics = diagnostics;

    /// <summary>Gets operator diagnostics. Redact local paths before sharing with tenants or agents.</summary>
    public IReadOnlyList<SkillCorpusDiagnostic> Diagnostics { get; }

    private static string BuildMessage(IReadOnlyList<SkillCorpusDiagnostic> diagnostics) {
        ArgumentNullException.ThrowIfNull(diagnostics);
        StringBuilder sb = new();
        _ = sb.Append("Skill corpus failed validation at startup. ");
        _ = sb.Append(diagnostics.Count.ToString(CultureInfo.InvariantCulture));
        _ = sb.AppendLine(" diagnostic(s):");
        foreach (SkillCorpusDiagnostic d in diagnostics) {
            _ = sb.Append("- [");
            _ = sb.Append(d.Category);
            _ = sb.Append("] ");
            _ = sb.Append(d.Source);
            _ = sb.Append(": ");
            _ = sb.AppendLine(d.Message);
        }

        return sb.ToString();
    }
}
