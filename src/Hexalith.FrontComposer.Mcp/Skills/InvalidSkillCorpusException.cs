using System.Globalization;
using System.Text;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed class InvalidSkillCorpusException : Exception {
    public InvalidSkillCorpusException(IReadOnlyList<SkillCorpusDiagnostic> diagnostics)
        : base(BuildMessage(diagnostics)) => Diagnostics = diagnostics;

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
