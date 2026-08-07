namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

/// <summary>
/// Story 11.21 review: the skill-corpus culture theory mutates process-wide
/// <see cref="System.Globalization.CultureInfo.CurrentCulture"/> and
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>. CI runs this assembly with the
/// default parallel collection behaviour, so the mutation would otherwise be observable by every
/// other test running concurrently in the same process. Members of this collection run serially and
/// never alongside another collection.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class McpCultureTestGroup {
    /// <summary>The xUnit collection name shared by every culture-mutating MCP test.</summary>
    public const string Name = "McpCulture";
}
