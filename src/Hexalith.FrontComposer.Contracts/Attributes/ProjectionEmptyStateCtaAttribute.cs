namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Declares the command type name that should be used as the primary call to action
/// when a generated projection renders its empty state.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionEmptyStateCtaAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionEmptyStateCtaAttribute"/> class.
    /// </summary>
    /// <param name="commandTypeName">The command type name to resolve from the FrontComposer registry.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="commandTypeName"/> is null, empty, or whitespace.</exception>
    public ProjectionEmptyStateCtaAttribute(string commandTypeName) {
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            throw new ArgumentException("Command type name cannot be null, empty, or whitespace.", nameof(commandTypeName));
        }

        CommandTypeName = commandTypeName;
    }

    /// <summary>
    /// Gets the command type name to resolve from the FrontComposer registry.
    /// </summary>
    public string CommandTypeName { get; }
}
