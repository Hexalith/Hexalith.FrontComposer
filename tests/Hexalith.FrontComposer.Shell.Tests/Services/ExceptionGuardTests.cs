using System.Reflection;
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
    public void IsFatal_NestedReflectionAndAggregateCauses_ReturnsTrue()
    {
        var fatal = (OutOfMemoryException)RuntimeHelpers.GetUninitializedObject(typeof(OutOfMemoryException));
        var reflected = new TargetInvocationException(new InvalidOperationException("wrapper", fatal));
        ExceptionGuard.IsFatal(reflected).ShouldBeTrue();
        ExceptionGuard.IsFatal(new AggregateException(new IOException(), new AggregateException(reflected))).ShouldBeTrue();
    }

    [Fact]
    public void IsFatal_AggregateWithOnlyRecoverableCauses_ReturnsFalse()
    {
        var aggregate = new AggregateException(new OperationCanceledException(), new TargetInvocationException(new IOException()));

        ExceptionGuard.IsFatal(aggregate).ShouldBeFalse();
    }

    [Fact]
    public void IsFatal_DeeplyNestedRecoverableCauses_ReturnsFalse()
    {
        Exception exception = new IOException();
        for (int index = 0; index < 10_000; index++)
        {
            exception = new InvalidOperationException("wrapper", exception);
        }

        ExceptionGuard.IsFatal(exception).ShouldBeFalse();
    }

    [Fact]
    public void IsFatal_SharedAggregateCauses_ReturnsFalse()
    {
        Exception exception = new IOException();
        for (int index = 0; index < 40; index++)
        {
            exception = new AggregateException(exception, exception);
        }

        ExceptionGuard.IsFatal(exception).ShouldBeFalse();
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
