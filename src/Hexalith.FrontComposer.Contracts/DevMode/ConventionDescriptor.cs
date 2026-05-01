namespace Hexalith.FrontComposer.Contracts.DevMode;

/// <summary>
/// Describes the generated convention or customization seam represented by a dev-mode annotation.
/// </summary>
public sealed record ConventionDescriptor {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionDescriptor"/> class.
    /// </summary>
    public ConventionDescriptor(
        string name,
        string description,
        string recommendedOverride,
        CustomizationLevel recommendedOverrideLevel) {
        Validate(name, description, recommendedOverride);

        Name = name;
        Description = description;
        RecommendedOverride = recommendedOverride;
        RecommendedOverrideLevel = recommendedOverrideLevel;
    }

    /// <summary>Gets the stable convention name displayed by the overlay.</summary>
    public string Name { get; init; }

    /// <summary>Gets the localized or descriptor-derived explanation for the convention.</summary>
    public string Description { get; init; }

    /// <summary>Gets the short recommendation for the lowest viable override level.</summary>
    public string RecommendedOverride { get; init; }

    /// <summary>Gets the recommended customization level for common changes.</summary>
    public CustomizationLevel RecommendedOverrideLevel { get; init; }

    private static void Validate(string name, string description, string recommendedOverride) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("Convention name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description)) {
            throw new ArgumentException("Convention description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(recommendedOverride)) {
            throw new ArgumentException("Convention recommended override is required.", nameof(recommendedOverride));
        }
    }
}
