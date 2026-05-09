namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a projection or command as belonging to a specific bounded context within the domain model.
/// The FrontComposer source generator uses the bounded context to group generated UI, Fluxor,
/// MCP metadata, and registration artifacts under the correct domain.
/// </summary>
/// <remarks>
/// The bounded context name appears in generated registration metadata and in IDE-visible
/// generated-source navigation. Diagnostics and IDE parity guidance are documented from
/// <see href="https://hexalith.github.io/FrontComposer/diagnostics/">the FrontComposer diagnostics pages</see>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BoundedContextAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedContextAttribute"/> class.
    /// </summary>
    /// <param name="name">The bounded context name.</param>
    public BoundedContextAttribute(string name) => Name = name;

    /// <summary>
    /// Gets the bounded context name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the optional display label for the bounded context.
    /// When set, overrides the default display name derived from the bounded context name.
    /// </summary>
    public string? DisplayLabel { get; set; }
}
