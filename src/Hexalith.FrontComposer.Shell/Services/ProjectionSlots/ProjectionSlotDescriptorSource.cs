using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

/// <summary>
/// Descriptor source for runtime Level 3 field-slot registrations.
/// </summary>
/// <param name="descriptors">Descriptor collection to expose.</param>
public sealed class ProjectionSlotDescriptorSource(IReadOnlyList<ProjectionSlotDescriptor> descriptors) {
    /// <summary>Gets immutable slot descriptors supplied by one registration source.</summary>
    public IReadOnlyList<ProjectionSlotDescriptor> Descriptors { get; } =
        descriptors ?? throw new ArgumentNullException(nameof(descriptors));
}
