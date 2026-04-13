namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

internal static class TestSources
{
    internal const string BasicProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Counter"")]
[Projection]
[ProjectionRole(ProjectionRole.StatusOverview)]
public partial class CounterProjection
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsActive { get; set; }
}";

    internal const string AllFieldTypesProjection = @"
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Inventory"")]
[Projection]
[ProjectionRole(ProjectionRole.DetailRecord)]
public partial class AllFieldTypesProjection
{
    // Primitives
    public string StringField { get; set; } = string.Empty;
    public int Int32Field { get; set; }
    public long Int64Field { get; set; }
    public decimal DecimalField { get; set; }
    public double DoubleField { get; set; }
    public float SingleField { get; set; }
    public bool BooleanField { get; set; }

    // Date/Time
    public DateTime DateTimeField { get; set; }
    public DateTimeOffset DateTimeOffsetField { get; set; }
    public DateOnly DateOnlyField { get; set; }
    public TimeOnly TimeOnlyField { get; set; }

    // Identity
    public Guid GuidField { get; set; }

    // Enum
    public ItemStatus StatusField { get; set; }

    // Nullable value types
    public int? NullableInt32 { get; set; }
    public long? NullableInt64 { get; set; }
    public decimal? NullableDecimal { get; set; }
    public double? NullableDouble { get; set; }
    public float? NullableSingle { get; set; }
    public bool? NullableBoolean { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public DateTimeOffset? NullableDateTimeOffset { get; set; }
    public DateOnly? NullableDateOnly { get; set; }
    public TimeOnly? NullableTimeOnly { get; set; }
    public Guid? NullableGuid { get; set; }
    public ItemStatus? NullableEnum { get; set; }

    // Nullable reference type
    public string? NullableString { get; set; }

    // Collections
    public List<string> ListField { get; set; } = new();
    public IEnumerable<int> EnumerableField { get; set; } = Array.Empty<int>();
    public IReadOnlyList<decimal> ReadOnlyListField { get; set; } = Array.Empty<decimal>();

    // Display attribute
    [Display(Name = ""Item Name"")]
    public string DisplayNameField { get; set; } = string.Empty;
}

public enum ItemStatus
{
    Active,
    Inactive,
    Archived,
}";

    internal const string UnsupportedFieldProjection = @"
using System;
using System.Collections.Generic;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public partial class UnsupportedFieldProjection
{
    public string ValidField { get; set; } = string.Empty;
    public byte[] ByteArrayField { get; set; } = Array.Empty<byte>();
    public Dictionary<string, int> DictField { get; set; } = new();
    public object ObjectField { get; set; } = new();
    public (int X, int Y) TupleField { get; set; }
}";

    internal const string NonPartialProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public class NonPartialProjection
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string StructProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Projection]
public partial struct StructProjection
{
    public string Name { get; set; }
}";

    internal const string RecordStructProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Projection]
public partial record struct RecordStructProjection(string Name);
";

    internal const string GenericProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Projection]
public partial class GenericProjection<T>
{
    public T Value { get; set; } = default!;
}";

    internal const string AbstractProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Projection]
public abstract partial class AbstractProjection
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string GlobalNamespaceProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

[BoundedContext(""Global"")]
[Projection]
public partial class GlobalProjection
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string MultiAttributeProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Sales"")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue)]
public partial class MultiAttributeProjection
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string BadgeMappingProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Orders"")]
[Projection]
public partial class BadgeMappingProjection
{
    public OrderStatus Status { get; set; }
}

public enum OrderStatus
{
    [ProjectionBadge(BadgeSlot.Neutral)]
    Pending,

    [ProjectionBadge(BadgeSlot.Info)]
    Processing,

    [ProjectionBadge(BadgeSlot.Success)]
    Completed,

    [ProjectionBadge(BadgeSlot.Danger)]
    Failed,
}";

    internal const string NullBoundedContextProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(null)]
[Projection]
public partial class NullBoundedContextProjection
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string InvalidProjectionRoleProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
[ProjectionRole((ProjectionRole)999)]
public partial class InvalidProjectionRoleProjection
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string InvalidBadgeSlotProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Orders"")]
[Projection]
public partial class InvalidBadgeSlotProjection
{
    public InvalidBadgeStatus Status { get; set; }
}

public enum InvalidBadgeStatus
{
    [ProjectionBadge((BadgeSlot)999)]
    Pending,
}";

    internal const string NonIntEnumProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Inventory"")]
[Projection]
public partial class NonIntEnumProjection
{
    public LargeStatus Status { get; set; }
}

public enum LargeStatus : long
{
    Active = 1,
    Archived = 2,
}";

    internal const string RecordProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public partial record RecordProjection(string Name, int Count);
";

    internal const string NestedInNonPartialProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

public class OuterClass
{
    [Projection]
    public partial class NestedProjection
    {
        public string Name { get; set; } = string.Empty;
    }
}";

    internal const string NullableContextDisabledProjection = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public partial class NullableContextDisabledProjection
{
    public string Name { get; set; }
    public int Count { get; set; }
}";
}
