namespace Hexalith.FrontComposer.Cli;

internal sealed record InspectLoadResult(bool Success, InspectReport? Report, string Error, int ExitCode) {
    public static InspectLoadResult Ok(InspectReport report) => new(true, report, string.Empty, ExitCodes.Success);

    public static InspectLoadResult Fail(string error, int exitCode) => new(false, null, error, exitCode);
}
