namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Classifies exceptions that must bypass Shell fallback, retry, and fault-isolation paths.
/// </summary>
internal static class ExceptionGuard {
    /// <summary>
    /// Determines whether the specified exception or any of its causes represents a process-fatal condition.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true"/> when the exception must propagate; otherwise, <see langword="false"/>.</returns>
    internal static bool IsFatal(Exception exception)
    {
        Stack<Exception>? pending = null;
        HashSet<Exception>? visited = null;
        Exception? current = exception;
        while (current is not null)
        {
            if (current is OutOfMemoryException or StackOverflowException or ThreadAbortException or AccessViolationException)
            {
                return true;
            }

            if (current is AggregateException { InnerExceptions.Count: > 1 } && visited is null)
            {
                pending = new();
                visited = new(ReferenceEqualityComparer.Instance);
            }

            if (visited is null || visited.Add(current))
            {
                if (current is AggregateException aggregate)
                {
                    // InnerException already visits the first cause. Save any additional causes.
                    for (int index = aggregate.InnerExceptions.Count - 1; index >= 1; index--)
                    {
                        pending!.Push(aggregate.InnerExceptions[index]);
                    }
                }

                current = current.InnerException;
            }
            else
            {
                current = null;
            }

            if (current is null && pending is not null)
            {
                _ = pending.TryPop(out current);
            }
        }

        return false;
    }
}
