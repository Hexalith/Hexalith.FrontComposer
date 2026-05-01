using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionViewOverrides;

/// <summary>
/// Immutable source of Level 4 projection-view override descriptors.
/// </summary>
public sealed class ProjectionViewOverrideDescriptorSource {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionViewOverrideDescriptorSource"/> class.
    /// </summary>
    public ProjectionViewOverrideDescriptorSource(IReadOnlyList<ProjectionViewOverrideDescriptor> descriptors) {
        ArgumentNullException.ThrowIfNull(descriptors);

        ProjectionViewOverrideDescriptor[] copy = new ProjectionViewOverrideDescriptor[descriptors.Count];
        for (int i = 0; i < descriptors.Count; i++) {
            copy[i] = descriptors[i]
                ?? throw new ArgumentException($"Descriptor at index {i} must not be null.", nameof(descriptors));
        }

        Descriptors = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the frozen descriptor list.</summary>
    public IReadOnlyList<ProjectionViewOverrideDescriptor> Descriptors { get; }
}
