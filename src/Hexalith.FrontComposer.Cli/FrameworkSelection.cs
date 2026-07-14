namespace Hexalith.FrontComposer.Cli;

internal sealed record FrameworkSelection(bool Success, string? Framework, string? GeneratedDirectory, string Error, int ExitCode) {
    public static FrameworkSelection Ok(string framework, string generatedDirectory) => new(true, framework, generatedDirectory, string.Empty, ExitCodes.Success);

    public static FrameworkSelection Fail(string error, int exitCode) => new(false, null, null, error, exitCode);
}
