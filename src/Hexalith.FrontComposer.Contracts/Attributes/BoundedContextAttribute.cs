namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a class as belonging to a specific bounded context within the domain model.
/// Used by the source generator to group projections and commands under a domain.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BoundedContextAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedContextAttribute"/> class.
    /// </summary>
    /// <param name="name">The bounded context name.</param>
    public BoundedContextAttribute(string name)
    {
        Name = name;
    }

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
