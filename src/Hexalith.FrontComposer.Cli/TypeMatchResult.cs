namespace Hexalith.FrontComposer.Cli;

internal sealed record TypeMatchResult(bool Success, InspectReport? Report, string Error, int ExitCode) {
    public static TypeMatchResult Ok(InspectReport report) => new(true, report, string.Empty, ExitCodes.Success);

    public static TypeMatchResult Fail(string error, int exitCode) => new(false, null, error, exitCode);
}
