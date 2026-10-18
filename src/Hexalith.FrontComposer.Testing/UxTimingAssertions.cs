namespace Hexalith.FrontComposer.Testing;

/// <summary>Assertions at exact fake-time boundaries.</summary>
public static class UxTimingAssertions
{
    /// <summary>Advances from the operation start to N-1 ms and N ms, asserting the observed transition.</summary>
    public static void AssertBoundary(TestTimeProvider clock, TimeSpan budget, Func<bool> hasFired)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(hasFired);
        ArgumentOutOfRangeException.ThrowIfLessThan(budget, TimeSpan.FromMilliseconds(1));
        clock.Advance(budget - TimeSpan.FromMilliseconds(1));
        if (hasFired()) { throw new InvalidOperationException("UX timing transition fired before its canonical boundary (N-1 ms)."); }
        clock.Advance(TimeSpan.FromMilliseconds(1));
        if (!hasFired()) { throw new InvalidOperationException("UX timing transition did not fire at its canonical boundary (N ms)."); }
    }
}
