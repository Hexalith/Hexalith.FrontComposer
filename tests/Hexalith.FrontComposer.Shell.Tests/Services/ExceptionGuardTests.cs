using System.Runtime.CompilerServices;

using Hexalith.FrontComposer.Shell.Services;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class ExceptionGuardTests {
    [Fact]
    public void IsFatal_FourAuthoritativeFatalTypes_ReturnsTrue() {
        Exception[] exceptions = [
#pragma warning disable CA2201 // This intentional fatal-exception construction is the fixture under test.
            new OutOfMemoryException(),
#pragma warning restore CA2201
#pragma warning disable CA2201 // This intentional fatal-exception construction is the fixture under test.
            new StackOverflowException(),
#pragma warning restore CA2201
            (ThreadAbortException)RuntimeHelpers.GetUninitializedObject(typeof(ThreadAbortException)),
#pragma warning disable CA2201 // This intentional fatal-exception construction is the fixture under test.
            new AccessViolationException(),
#pragma warning restore CA2201
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
