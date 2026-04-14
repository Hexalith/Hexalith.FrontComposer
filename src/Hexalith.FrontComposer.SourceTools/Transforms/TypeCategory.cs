#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Categorizes a property type for DataGrid column rendering.
/// </summary>
public enum TypeCategory
{
    /// <summary>Text column (string, default).</summary>
    Text,

    /// <summary>Right-aligned locale-formatted numeric column.</summary>
    Numeric,

    /// <summary>Yes/No boolean column.</summary>
    Boolean,

    /// <summary>Date or time column formatted per CultureInfo.</summary>
    DateTime,

    /// <summary>Humanized enum label column.</summary>
    Enum,

    /// <summary>Collection count column.</summary>
    Collection,

    /// <summary>Unsupported type -- column skipped during generation.</summary>
    Unsupported,
}
