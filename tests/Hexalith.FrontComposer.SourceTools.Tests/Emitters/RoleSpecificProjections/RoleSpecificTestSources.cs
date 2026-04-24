namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters.RoleSpecificProjections;

/// <summary>
/// Story 4-1 T5.2 — synthetic role-specific projection fixtures used by
/// <see cref="RoleSpecificProjectionApprovalTests"/>.
/// Five happy-path fixtures (one per concrete role) and three negative-path fixtures
/// that exercise the HFC1022 / HFC1023 fallback surfaces documented in D10 / D11 / D13 / D16.
/// The fixtures live as const strings so the generator can be driven through the
/// existing <c>CompilationHelper</c> pattern without a parallel .cs corpus polluting
/// the test assembly's real compilation.
/// </summary>
internal static class RoleSpecificTestSources
{
    // ---------- Happy path ----------

    internal const string ActionQueueProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Orders;

[BoundedContext(""Orders"")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = ""Pending,Submitted"")]
public partial class ActionQueueProjection
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public OrderLifecycle Status { get; set; }
    public int Priority { get; set; }
}

public enum OrderLifecycle
{
    [ProjectionBadge(BadgeSlot.Neutral)]
    Pending,

    [ProjectionBadge(BadgeSlot.Info)]
    Submitted,

    [ProjectionBadge(BadgeSlot.Success)]
    Approved,

    [ProjectionBadge(BadgeSlot.Danger)]
    Rejected,
}";

    internal const string StatusOverviewProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Tickets;

[BoundedContext(""Tickets"")]
[Projection]
[ProjectionRole(ProjectionRole.StatusOverview)]
public partial class StatusOverviewProjection
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TicketState Status { get; set; }
}

public enum TicketState
{
    [ProjectionBadge(BadgeSlot.Info)]
    Open,

    [ProjectionBadge(BadgeSlot.Warning)]
    InProgress,

    [ProjectionBadge(BadgeSlot.Success)]
    Resolved,

    [ProjectionBadge(BadgeSlot.Neutral)]
    Closed,
}";

    internal const string DetailRecordProjection = @"
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Customers;

[BoundedContext(""Customers"")]
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
public partial class DetailRecordProjection
{
    public string Id { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public DateTime OnboardedAt { get; set; }
    public string AccountManager { get; set; } = string.Empty;
    public string LoyaltyTier { get; set; } = string.Empty;
    public int Seats { get; set; }
}";

    internal const string TimelineProjection = @"
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Audit;

[BoundedContext(""Audit"")]
[Projection]
[ProjectionRole(ProjectionRole.Timeline)]
public partial class TimelineProjection
{
    public string Id { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public AuditSeverity Severity { get; set; }
}

public enum AuditSeverity
{
    [ProjectionBadge(BadgeSlot.Neutral)]
    Info,

    [ProjectionBadge(BadgeSlot.Warning)]
    Warning,

    [ProjectionBadge(BadgeSlot.Danger)]
    Critical,
}";

    internal const string DashboardProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Metrics;

[BoundedContext(""Metrics"")]
[Projection]
[ProjectionRole(ProjectionRole.Dashboard)]
public partial class DashboardProjection
{
    public string Id { get; set; } = string.Empty;
    public string Kpi { get; set; } = string.Empty;
    public decimal Value { get; set; }
}";

    // ---------- Negative path ----------

    /// <summary>
    /// ActionQueue role + WhenState declared, but the projection has NO enum property to filter
    /// against. D10 specifies: emit HFC1022 at transform and fall back to an unfiltered source.
    /// </summary>
    internal const string ActionQueueNoEnumProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Negative;

[BoundedContext(""Support"")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = ""Pending"")]
public partial class ActionQueueNoEnumProjection
{
    public string Id { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Priority { get; set; }
}";

    /// <summary>
    /// ActionQueue role + WhenState containing an unknown enum member.
    /// Per D3: parse emits HFC1022 listing valid members, unknown member still flows through
    /// (fail-soft — runtime filter simply never matches that token).
    /// </summary>
    internal const string WhenStateTypoProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Negative;

[BoundedContext(""Orders"")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = ""Pending,Pendng"")]
public partial class WhenStateTypoProjection
{
    public string Id { get; set; } = string.Empty;
    public TypoStatus Status { get; set; }
}

public enum TypoStatus
{
    [ProjectionBadge(BadgeSlot.Neutral)]
    Pending,

    [ProjectionBadge(BadgeSlot.Info)]
    Submitted,

    [ProjectionBadge(BadgeSlot.Success)]
    Approved,
}";

    /// <summary>
    /// Dashboard role on a projection whose shape is not dashboard-appropriate. Per D16 / AC10:
    /// Transform emits HFC1023 Information and Emit dispatches to <c>EmitDashboardBody</c>
    /// which delegates to <c>EmitDefaultBody</c> — so the output is the Default DataGrid shape.
    /// </summary>
    internal const string DashboardWrongShapeProjection = @"
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace RoleSpecific.Negative;

[BoundedContext(""Reporting"")]
[Projection]
[ProjectionRole(ProjectionRole.Dashboard)]
public partial class DashboardWrongShapeProjection
{
    public string Id { get; set; } = string.Empty;
    public string FreeFormNote { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}";
}
