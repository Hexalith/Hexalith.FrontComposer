using Hexalith.FrontComposer.Cli;

using CancellationTokenSource cancellation = new();
int cancelPresses = 0;
ConsoleCancelEventHandler handler = (_, eventArgs) => {
    if (Interlocked.Increment(ref cancelPresses) > 1) {
        eventArgs.Cancel = false;
        return;
    }

    eventArgs.Cancel = true;
    try {
        cancellation.Cancel();
    }
    catch (ObjectDisposedException) {
        // Race: the CTS may already have been disposed between unsubscribe and dispose.
        // Treat as already-cancelled and let the OS finish process termination.
    }
};

Console.CancelKeyPress += handler;

try {
    return await CliApplication.RunAsync(args, Console.Out, Console.Error, cancellation.Token)
        .ConfigureAwait(false);
}
finally {
    Console.CancelKeyPress -= handler;
}
