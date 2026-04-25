namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Declares the Fluent UI icon rendered by a <see cref="CommandAttribute"/>-annotated type's
/// generated renderer. Applied at the class level.
/// </summary>
/// <remarks>
/// Icon-name format is the FrontComposer icon contract fragment (e.g. <c>Regular.Size16.Play</c>,
/// <c>Regular.Size20.Settings</c>). The source generator resolves the name through the shell's
/// Fluent UI v5 icon factory and falls back to <c>Regular.Size16.Play</c> when unsupported.
/// Parse-time icon-format validation is deferred to the Epic 9 analyzer.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class IconAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="IconAttribute"/> class.
    /// </summary>
    /// <param name="iconName">The FrontComposer icon contract fragment (e.g. <c>Regular.Size16.Play</c>).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="iconName"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public IconAttribute(string iconName) {
        if (string.IsNullOrWhiteSpace(iconName)) {
            throw new ArgumentException("Icon name cannot be null, empty, or whitespace.", nameof(iconName));
        }

        IconName = iconName;
    }

    /// <summary>
    /// Gets the FrontComposer icon contract fragment.
    /// </summary>
    public string IconName { get; }
}
