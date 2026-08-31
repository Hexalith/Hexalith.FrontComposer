using System.Diagnostics;
using System.Text;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

/// <summary>
/// Runs a redirected governance child process within one deadline that includes pipe drainage.
/// </summary>
internal static class GovernanceProcessRunner
{
    private static readonly TimeSpan CleanupGrace = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Runs the configured process, returning its exit code and combined standard output and error.
    /// </summary>
    /// <param name="startInfo">The fully configured process start information.</param>
    /// <param name="deadline">The maximum duration for process exit and redirected pipe drainage.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The process exit code and combined redirected output.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="deadline"/> is not positive.</exception>
    /// <exception cref="TimeoutException">Thrown after the process tree is terminated and its output is drained on deadline expiry.</exception>
    /// <exception cref="OperationCanceledException">Thrown after the process tree is terminated and its output is drained on caller cancellation.</exception>
    internal static Task<(int ExitCode, string Output)> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan deadline,
        CancellationToken cancellationToken)
    {
        // Caller cancellation must surface as a faulted OperationCanceledException that still
        // carries the drained child output. Letting RunCoreAsync's task cancel directly would
        // discard that message, so the cancellation signal is relayed through this source.
        TaskCompletionSource<(int ExitCode, string Output)> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _ = TransferCompletionAsync(
            RunCoreAsync(startInfo, deadline, cancellationToken),
            completion);
        return completion.Task;
    }

    private static async Task<(int ExitCode, string Output)> RunCoreAsync(
        ProcessStartInfo startInfo,
        TimeSpan deadline,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        if (deadline <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deadline), deadline, "The process deadline must be positive.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start {startInfo.FileName}.");
        }

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
        Task<(int ExitCode, string Output)> completion = CompleteAsync(process, standardOutput, standardError);

        // On deadline or cancellation this task is abandoned while `process` is disposed under
        // it, so reading ExitCode can fault. Observe it here to keep that an inert result rather
        // than an unobserved task exception.
        _ = completion.ContinueWith(
            static abandoned => _ = abandoned.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        try
        {
            return await completion.WaitAsync(deadline, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException exception)
        {
            (string output, string cleanupFailure) = await TerminateAndDrainAsync(
                process,
                standardOutput,
                standardError).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                throw new CallerCancellationSignalException(
                    $"{FormatCommand(startInfo)} was canceled by the caller."
                    + FormatFailureDetails(output, cleanupFailure),
                    exception,
                    cancellationToken);
            }

            throw new TimeoutException(
                $"{FormatCommand(startInfo)} exceeded the {deadline.TotalSeconds:F3}-second governance bound."
                + FormatFailureDetails(output, cleanupFailure),
                exception);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            (string output, string cleanupFailure) = await TerminateAndDrainAsync(
                process,
                standardOutput,
                standardError).ConfigureAwait(false);
            throw new CallerCancellationSignalException(
                $"{FormatCommand(startInfo)} was canceled by the caller."
                + FormatFailureDetails(output, cleanupFailure),
                exception,
                cancellationToken);
        }
        catch
        {
            _ = await TerminateAndDrainAsync(process, standardOutput, standardError).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task TransferCompletionAsync(
        Task<(int ExitCode, string Output)> run,
        TaskCompletionSource<(int ExitCode, string Output)> completion)
    {
        try
        {
            completion.SetResult(await run.ConfigureAwait(false));
        }
        catch (CallerCancellationSignalException exception)
        {
            completion.SetException(new OperationCanceledException(
                exception.Message,
                exception.InnerException,
                exception.CancellationToken));
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }

    private static async Task<(int ExitCode, string Output)> CompleteAsync(
        Process process,
        Task<string> standardOutput,
        Task<string> standardError)
    {
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        string[] output = await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
        return (process.ExitCode, string.Concat(output));
    }

    private static async Task<(string Output, string CleanupFailure)> TerminateAndDrainAsync(
        Process process,
        Task<string> standardOutput,
        Task<string> standardError)
    {
        List<string> cleanupFailures = [];
        using CancellationTokenSource cleanupDeadline = new(CleanupGrace);
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between HasExited and Kill. The governing timeout or
            // cancellation remains authoritative, and the pipes are still drained below.
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            cleanupFailures.Add($"process-tree termination failed: {exception.Message}");
        }

        try
        {
            await process.WaitForExitAsync(cleanupDeadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cleanupDeadline.IsCancellationRequested)
        {
            cleanupFailures.Add(
                $"process exit observation exceeded the {CleanupGrace.TotalSeconds:F0}-second cleanup bound");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            cleanupFailures.Add($"process exit observation failed: {exception.Message}");
        }

        try
        {
            _ = await Task.WhenAll(standardOutput, standardError)
                .WaitAsync(cleanupDeadline.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cleanupDeadline.IsCancellationRequested)
        {
            cleanupFailures.Add(
                $"redirected pipe drainage exceeded the {CleanupGrace.TotalSeconds:F0}-second cleanup bound");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            cleanupFailures.Add($"redirected pipe drainage failed: {exception.Message}");
        }

        string stdout = CompletedPipeOutput(standardOutput, "stdout", cleanupFailures);
        string stderr = CompletedPipeOutput(standardError, "stderr", cleanupFailures);
        return (stdout + stderr, string.Join("; ", cleanupFailures));
    }

    private static string CompletedPipeOutput(
        Task<string> pipe,
        string name,
        List<string> cleanupFailures)
    {
        if (pipe.IsCompletedSuccessfully)
        {
            return pipe.Result;
        }

        if (pipe.IsFaulted)
        {
            cleanupFailures.Add($"{name} drainage failed: {pipe.Exception?.GetBaseException().Message}");
        }

        return string.Empty;
    }

    private static string FormatCommand(ProcessStartInfo startInfo)
        => string.Join(' ', new[] { startInfo.FileName }.Concat(startInfo.ArgumentList));

    private static string FormatFailureDetails(string output, string cleanupFailure)
    {
        StringBuilder details = new();
        if (!string.IsNullOrEmpty(output))
        {
            _ = details.AppendLine().AppendLine("Captured output:").Append(output);
        }

        if (!string.IsNullOrEmpty(cleanupFailure))
        {
            _ = details.AppendLine().Append("Cleanup diagnostics: ").Append(cleanupFailure);
        }

        return details.ToString();
    }

    private sealed class CallerCancellationSignalException(
        string message,
        Exception innerException,
        CancellationToken cancellationToken)
        : Exception(message, innerException)
    {
        internal CancellationToken CancellationToken { get; } = cancellationToken;
    }
}
