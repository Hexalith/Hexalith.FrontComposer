namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Regression;

/// <summary>
/// Story 9-1 review CH-3: culture-invariance tests mutate process-wide
/// <see cref="System.Globalization.CultureInfo.CurrentCulture"/>. Run them serially in a
/// dedicated xUnit collection so concurrent test runs in the same process don't observe the
/// mutation.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DriftCultureCollection {
    public const string Name = "DriftCulture";
}
