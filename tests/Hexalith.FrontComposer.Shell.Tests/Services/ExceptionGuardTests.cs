using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Shell.Services;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class ExceptionGuardTests {
    [Fact]
    public void IsFatal_FourAuthoritativeFatalTypes_ReturnsTrue() {
        Exception[] exceptions = [
            new OutOfMemoryException(),
            new StackOverflowException(),
            (ThreadAbortException)RuntimeHelpers.GetUninitializedObject(typeof(ThreadAbortException)),
            new AccessViolationException(),
        ];

        exceptions.ShouldAllBe(exception => ExceptionGuard.IsFatal(exception));
    }

    [Fact]
    public void IsFatal_CancellationAndRepresentativeRecoverableTypes_ReturnsFalse() {
        Exception[] exceptions = [
            new OperationCanceledException(),
            new InvalidOperationException(),
            new IOException(),
        ];

        exceptions.ShouldAllBe(exception => !ExceptionGuard.IsFatal(exception));
    }
}
