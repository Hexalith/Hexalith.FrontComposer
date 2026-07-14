namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record GeneratedCodeDiagnostic(
    GeneratedCodeFailureCategory Category,
    string Path,
    string Message);
