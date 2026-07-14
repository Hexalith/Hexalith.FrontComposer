namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record GeneratedCodeValidationResult(IReadOnlyList<GeneratedCodeDiagnostic> Diagnostics) {
    public bool IsValid => Diagnostics.Count == 0;
}
