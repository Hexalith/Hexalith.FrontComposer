using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Inputs supplied to a configured projection-page callback.</summary>
public sealed record ProjectionPageRequest(
    string ProjectionTypeFqn,
    int Skip,
    int Take,
    IImmutableDictionary<string, string> Filters,
    string? SortColumn,
    bool SortDescending,
    string? SearchQuery);
