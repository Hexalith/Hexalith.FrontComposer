namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Classifies exceptions that must bypass Shell fallback, retry, and fault-isolation paths.
/// </summary>
internal static class ExceptionGuard {
    /// <summary>
    /// Determines whether the specified exception represents a process-fatal condition.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true"/> when the exception must propagate; otherwise, <see langword="false"/>.</returns>
    internal static bool IsFatal(Exception exception)
        => exception is OutOfMemoryException
            or StackOverflowException
            or System.Threading.ThreadAbortException
            or AccessViolationException;
}
