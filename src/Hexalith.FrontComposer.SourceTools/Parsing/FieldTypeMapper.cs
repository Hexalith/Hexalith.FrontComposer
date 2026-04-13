#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Parsing;

using System.Collections.Generic;

/// <summary>
/// Maps .NET type names to IR type representations.
/// Public static class -- unit-testable in isolation without CSharpGeneratorDriver.
/// </summary>
public static class FieldTypeMapper
{
    private static readonly Dictionary<string, string> SupportedTypes = new Dictionary<string, string>
    {
        // Primitives
        ["System.String"] = "String",
        ["string"] = "String",
        ["System.Int32"] = "Int32",
        ["int"] = "Int32",
        ["System.Int64"] = "Int64",
        ["long"] = "Int64",
        ["System.Decimal"] = "Decimal",
        ["decimal"] = "Decimal",
        ["System.Double"] = "Double",
        ["double"] = "Double",
        ["System.Single"] = "Single",
        ["float"] = "Single",
        ["System.Boolean"] = "Boolean",
        ["bool"] = "Boolean",

        // Date/Time
        ["System.DateTime"] = "DateTime",
        ["System.DateTimeOffset"] = "DateTimeOffset",
        ["System.DateOnly"] = "DateOnly",
        ["System.TimeOnly"] = "TimeOnly",

        // Identity
        ["System.Guid"] = "Guid",
    };

    private static readonly HashSet<string> CollectionTypes = new HashSet<string>
    {
        "System.Collections.Generic.List",
        "System.Collections.Generic.IEnumerable",
        "System.Collections.Generic.IReadOnlyList",
    };

    /// <summary>
    /// Maps a fully qualified .NET type name to its IR type representation.
    /// </summary>
    /// <param name="fullyQualifiedTypeName">The fully qualified type name (e.g., "System.Int32").</param>
    /// <param name="isEnum">Whether the type is an enum.</param>
    /// <returns>The IR type name, or null if unsupported.</returns>
    public static string? MapType(string fullyQualifiedTypeName, bool isEnum)
    {
        if (isEnum)
        {
            return "Enum";
        }

        if (SupportedTypes.TryGetValue(fullyQualifiedTypeName, out string? irType))
        {
            return irType;
        }

        // Check collection types (generic -- compare the unconstructed name)
        foreach (string collectionPrefix in CollectionTypes)
        {
            if (fullyQualifiedTypeName.StartsWith(collectionPrefix + "<", System.StringComparison.Ordinal)
                || fullyQualifiedTypeName == collectionPrefix)
            {
                return "Collection";
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a fully qualified type name is a known collection type.
    /// </summary>
    public static bool IsCollectionType(string fullyQualifiedTypeName)
    {
        foreach (string collectionPrefix in CollectionTypes)
        {
            if (fullyQualifiedTypeName.StartsWith(collectionPrefix + "<", System.StringComparison.Ordinal)
                || fullyQualifiedTypeName == collectionPrefix)
            {
                return true;
            }
        }

        return false;
    }
}
