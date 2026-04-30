using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

/// <summary>
/// Descriptor source for runtime Level 3 field-slot registrations.
/// </summary>
/// <remarks>
/// Defensive-copies the supplied list at construction so adopters cannot mutate registry
/// inputs after registration; rejects null elements with an explicit indexed
/// <see cref="ArgumentException"/> so a corrupt source does not crash the registry mid-loop.
/// </remarks>
public sealed class ProjectionSlotDescriptorSource {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSlotDescriptorSource"/> class.
    /// </summary>
    /// <param name="descriptors">Descriptor collection to expose. Must not contain null elements.</param>
    public ProjectionSlotDescriptorSource(IReadOnlyList<ProjectionSlotDescriptor> descriptors) {
        ArgumentNullException.ThrowIfNull(descriptors);

        ProjectionSlotDescriptor[] copy = new ProjectionSlotDescriptor[descriptors.Count];
        for (int i = 0; i < descriptors.Count; i++) {
            ProjectionSlotDescriptor entry = descriptors[i]
                ?? throw new ArgumentException(
                    $"Descriptor list contains a null entry at index {i}.",
                    nameof(descriptors));
            copy[i] = entry;
        }

        Descriptors = copy;
    }

    /// <summary>Gets the immutable defensive-copied slot descriptors supplied by one registration source.</summary>
    public IReadOnlyList<ProjectionSlotDescriptor> Descriptors { get; }
}
