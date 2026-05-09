namespace Hexalith.FrontComposer.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Count == 0 || string.IsNullOrWhiteSpace(args[0]) || IsHelp(args[0])) {
            WriteHelp(output);
            return ExitCodes.Success;
        }

        try {
            return args[0] switch {
                "inspect" => await InspectCommand.RunAsync(CommandOptions.Parse(args.Skip(1)), output, error, cancellationToken)
                    .ConfigureAwait(false),
                "migrate" => await MigrationCommand.RunAsync(CommandOptions.Parse(args.Skip(1)), output, error, cancellationToken)
                    .ConfigureAwait(false),
                _ => Invalid(error, $"Unknown command '{OutputSanitizer.Sanitize(args[0])}'. Run 'frontcomposer --help'."),
            };
        }
        catch (OperationCanceledException) {
            await error.WriteLineAsync("Operation was cancelled. No further files were written.").ConfigureAwait(false);
            return ExitCodes.ApplyWriteFailure;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            await error.WriteLineAsync("File-system operation failed: " + OutputSanitizer.Sanitize(ex.Message)).ConfigureAwait(false);
            return ExitCodes.ApplyWriteFailure;
        }
    }

    private static bool IsHelp(string value) => value is "--help" or "-h" or "help";

    private static int Invalid(TextWriter error, string message)
    {
        error.WriteLine(message);
        return ExitCodes.InvalidArguments;
    }

    private static void WriteHelp(TextWriter output)
    {
        output.WriteLine("Hexalith FrontComposer CLI");
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  frontcomposer inspect [--summary] [--type <metadata-name>] [--project <path>] [--solution <path>] [--configuration <name>] [--framework <tfm>] [--build] [--format text|json] [--severity hidden|info|warning|error] [--fail-on-warning] [--fail-on-error] [--absolute-paths]");
        output.WriteLine("  frontcomposer migrate --from <version> --to <version> [--dry-run|--apply] [--project <path>] [--solution <path>] [--format text|json] [--fail-on-findings]");
        output.WriteLine();
        output.WriteLine("Exit codes: 0 success, 1 explicit fail-on-findings, 2 invalid/ambiguous input, 3 generated output unavailable, 4 apply/write failure.");
    }
}
