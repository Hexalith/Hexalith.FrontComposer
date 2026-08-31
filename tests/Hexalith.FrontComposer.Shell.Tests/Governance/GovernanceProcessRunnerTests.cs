using System.Diagnostics;
using System.Globalization;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Collection(AnalyzerPolicyGovernanceTestGroup.Name)]
[Trait("Category", "Governance")]
public sealed class GovernanceProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_ProcessSucceeds_DrainsBothPipes()
    {
        ProcessStartInfo startInfo = ShellProcess("printf 'stdout-success\\n'; printf 'stderr-success\\n' >&2");

        (int exitCode, string output) = await GovernanceProcessRunner.RunAsync(
            startInfo,
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        exitCode.ShouldBe(0);
        output.ShouldContain("stdout-success");
        output.ShouldContain("stderr-success");
    }

    [Fact]
    public async Task RunAsync_ProcessFails_ReturnsExitCodeAndDrainsBothPipes()
    {
        ProcessStartInfo startInfo = ShellProcess(
            "printf 'stdout-success\\n'; printf 'stderr-success\\n' >&2; exit 7");

        (int exitCode, string output) = await GovernanceProcessRunner.RunAsync(
            startInfo,
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        exitCode.ShouldBe(7);
        output.ShouldContain("stdout-success");
        output.ShouldContain("stderr-success");
    }

    [Fact]
    public async Task RunAsync_DeadlineExpires_KillsTreeAndReportsDrainedOutput()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "fc-governance-runner-timeout-" + Guid.NewGuid().ToString("N"));
        string childPidPath = Path.Combine(temporaryRoot, "child.pid");
        _ = Directory.CreateDirectory(temporaryRoot);
        try
        {
            ProcessStartInfo startInfo = LongRunningShellProcess(childPidPath);
            TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
                GovernanceProcessRunner.RunAsync(
                    startInfo,
                    TimeSpan.FromSeconds(3),
                    TestContext.Current.CancellationToken)).ConfigureAwait(true);

            exception.Message.ShouldContain("exceeded");
            exception.Message.ShouldContain("stdout-before-wait");
            exception.Message.ShouldContain("stderr-before-wait");
            int childPid = await ReadChildPidAsync(childPidPath).ConfigureAwait(true);
            (await ProcessExitedAsync(childPid).ConfigureAwait(true))
                .ShouldBeTrue($"deadline cleanup left child process {childPid} running");
        }
        finally
        {
            CleanupChild(childPidPath);
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_CallerCancels_KillsTreeDrainsOutputAndPreservesCancellation()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "fc-governance-runner-cancel-" + Guid.NewGuid().ToString("N"));
        string childPidPath = Path.Combine(temporaryRoot, "child.pid");
        _ = Directory.CreateDirectory(temporaryRoot);
        using CancellationTokenSource cancellation = new();
        try
        {
            ProcessStartInfo startInfo = LongRunningShellProcess(childPidPath);
            Task<(int ExitCode, string Output)> run = GovernanceProcessRunner.RunAsync(
                startInfo,
                TimeSpan.FromSeconds(30),
                cancellation.Token);
            _ = await WaitForChildPidAsync(childPidPath).ConfigureAwait(true);
            cancellation.Cancel();
            OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() =>
                run).ConfigureAwait(true);

            exception.CancellationToken.ShouldBe(cancellation.Token);
            exception.Message.ShouldContain("stdout-before-wait");
            exception.Message.ShouldContain("stderr-before-wait");
            int childPid = await ReadChildPidAsync(childPidPath).ConfigureAwait(true);
            (await ProcessExitedAsync(childPid).ConfigureAwait(true))
                .ShouldBeTrue($"caller-cancellation cleanup left child process {childPid} running");
        }
        finally
        {
            CleanupChild(childPidPath);
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_ExitedParentLeavesPipeHolder_CleanupRemainsBounded()
    {
        Assert.SkipWhen(
            OperatingSystem.IsWindows(),
            "the detached pipe-holder cleanup bound is proven with POSIX setsid semantics");

        string temporaryRoot = Path.Combine(Path.GetTempPath(), "fc-governance-runner-pipe-" + Guid.NewGuid().ToString("N"));
        string childPidPath = Path.Combine(temporaryRoot, "child.pid");
        _ = Directory.CreateDirectory(temporaryRoot);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            ProcessStartInfo startInfo = new("/bin/bash");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(
                "setsid /bin/bash -c 'sleep 60' & child=$!; "
                + "printf '%s' \"$child\" > \"$1\"; printf 'stdout-before-detach\\n'; exit 0");
            startInfo.ArgumentList.Add("governance-runner");
            startInfo.ArgumentList.Add(childPidPath);

            TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() =>
                GovernanceProcessRunner.RunAsync(
                    startInfo,
                    TimeSpan.FromMilliseconds(500),
                    TestContext.Current.CancellationToken)).ConfigureAwait(true);

            stopwatch.Stop();
            exception.Message.ShouldContain("redirected pipe drainage exceeded");
            stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(8));
        }
        finally
        {
            CleanupChild(childPidPath);
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    private static ProcessStartInfo ShellProcess(string unixCommand)
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessStartInfo windows = new("powershell.exe");
            windows.ArgumentList.Add("-NoProfile");
            windows.ArgumentList.Add("-NonInteractive");
            windows.ArgumentList.Add("-Command");
            windows.ArgumentList.Add(unixCommand
                .Replace("printf 'stdout-success\\n'", "Write-Output 'stdout-success'", StringComparison.Ordinal)
                .Replace("printf 'stderr-success\\n' >&2", "[Console]::Error.WriteLine('stderr-success')", StringComparison.Ordinal));
            return windows;
        }

        ProcessStartInfo unix = new("/bin/bash");
        unix.ArgumentList.Add("-c");
        unix.ArgumentList.Add(unixCommand);
        return unix;
    }

    private static ProcessStartInfo LongRunningShellProcess(string childPidPath)
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessStartInfo windows = new("powershell.exe");
            windows.ArgumentList.Add("-NoProfile");
            windows.ArgumentList.Add("-NonInteractive");
            windows.ArgumentList.Add("-Command");
            // `-Command` does not bind a `param()` block from positional arguments, so the PID
            // path travels through the environment instead.
            windows.ArgumentList.Add(
                "Write-Output 'stdout-before-wait'; "
                + "[Console]::Error.WriteLine('stderr-before-wait'); "
                + "$child = Start-Process -FilePath $env:ComSpec -ArgumentList '/d /c ping -t 127.0.0.1 ^>nul' -PassThru; "
                + "Set-Content -NoNewline -Path $env:FC_GOVERNANCE_CHILD_PID_PATH -Value $child.Id; "
                + "Wait-Process -Id $child.Id");
            windows.Environment["FC_GOVERNANCE_CHILD_PID_PATH"] = childPidPath;
            return windows;
        }

        ProcessStartInfo unix = new("/bin/bash");
        unix.ArgumentList.Add("-c");
        unix.ArgumentList.Add(
            "printf 'stdout-before-wait\\n'; printf 'stderr-before-wait\\n' >&2; "
            + "sleep 60 & child=$!; printf '%s' \"$child\" > \"$1\"; wait \"$child\"");
        unix.ArgumentList.Add("governance-runner");
        unix.ArgumentList.Add(childPidPath);
        return unix;
    }

    private static async Task<int> ReadChildPidAsync(string childPidPath)
    {
        File.Exists(childPidPath).ShouldBeTrue("the child process must publish its PID before waiting");
        string text = await File.ReadAllTextAsync(
            childPidPath,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        return int.Parse(text, CultureInfo.InvariantCulture);
    }

    private static async Task<int> WaitForChildPidAsync(string childPidPath)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (File.Exists(childPidPath))
            {
                string text = await File.ReadAllTextAsync(
                    childPidPath,
                    TestContext.Current.CancellationToken).ConfigureAwait(true);
                if (int.TryParse(text, CultureInfo.InvariantCulture, out int processId))
                {
                    return processId;
                }
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(50),
                TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        throw new TimeoutException("the child process did not publish its PID within five seconds");
    }

    private static async Task<bool> ProcessExitedAsync(int processId)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                using Process child = Process.GetProcessById(processId);
                if (child.HasExited)
                {
                    return true;
                }
            }
            catch (ArgumentException)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        return false;
    }

    private static void CleanupChild(string childPidPath)
    {
        if (!File.Exists(childPidPath)
            || !int.TryParse(File.ReadAllText(childPidPath), CultureInfo.InvariantCulture, out int processId))
        {
            return;
        }

        try
        {
            using Process child = Process.GetProcessById(processId);
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
            }
        }
        catch (ArgumentException)
        {
            // The process already exited and was reaped.
        }
        catch (InvalidOperationException)
        {
            // The process exited between HasExited and Kill.
        }
    }
}
