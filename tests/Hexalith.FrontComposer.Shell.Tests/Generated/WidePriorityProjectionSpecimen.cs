using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2.5 AC1/AC2 specimen — bounded context for the wide (&gt;15-column) column-prioritization
/// end-to-end render pin.
/// </summary>
[BoundedContext("WidePriority")]
public sealed class WidePriorityDomain {
}

/// <summary>
/// Story 2.5 AC1/AC2 specimen — a deliberately WIDE projection (18 columns &gt; the strict 15-column
/// threshold, D6) whose properties carry a scrambled mix of <see cref="ColumnPriorityAttribute"/>
/// annotations so that priority order is provably DIFFERENT from declaration order.
/// </summary>
/// <remarks>
/// Declaration order vs. the <c>(Priority ?? int.MaxValue, declarationOrder)</c> stable sort
/// (<c>RazorModelTransform</c>) yields the generator-emitted <c>_allColumnsDescriptor</c> /
/// prioritizer-checkbox order:
/// <c>Gamma(1), Delta(2), Alpha(3), Theta(4), Zeta(5)</c> (annotated, ascending) then the
/// unannotated columns trailing in declaration order
/// <c>Id, Beta, Epsilon, Eta, Iota, Kappa, Lambda, Mu, Nu, Xi, Omicron, Pi, Rho</c>.
/// With <c>MaxVisibleColumns = 10</c> the eight columns at sorted indices 10..17
/// (<c>Kappa, Lambda, Mu, Nu, Xi, Omicron, Pi, Rho</c>) are hidden by default.
/// </remarks>
[Projection]
[BoundedContext("WidePriority")]
public partial class WidePriorityProjection {
    /// <summary>Gets or sets the row identity (unannotated — trails the annotated columns).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the third-priority column.</summary>
    [ColumnPriority(3)]
    public string? Alpha { get; set; }

    /// <summary>Gets or sets an unannotated column (declared before its annotated neighbours).</summary>
    public string? Beta { get; set; }

    /// <summary>Gets or sets the first-priority column (sorts to the front).</summary>
    [ColumnPriority(1)]
    public string? Gamma { get; set; }

    /// <summary>Gets or sets the second-priority column.</summary>
    [ColumnPriority(2)]
    public string? Delta { get; set; }

    /// <summary>Gets or sets an unannotated column.</summary>
    public string? Epsilon { get; set; }

    /// <summary>Gets or sets the fifth-priority column.</summary>
    [ColumnPriority(5)]
    public string? Zeta { get; set; }

    /// <summary>Gets or sets an unannotated column.</summary>
    public string? Eta { get; set; }

    /// <summary>Gets or sets the fourth-priority column.</summary>
    [ColumnPriority(4)]
    public string? Theta { get; set; }

    /// <summary>Gets or sets an unannotated column.</summary>
    public string? Iota { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Kappa { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Lambda { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Mu { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Nu { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Xi { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Omicron { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Pi { get; set; }

    /// <summary>Gets or sets an unannotated column (hidden by default).</summary>
    public string? Rho { get; set; }
}
