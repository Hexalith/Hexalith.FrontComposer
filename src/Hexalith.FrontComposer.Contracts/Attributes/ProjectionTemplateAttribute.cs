namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a Razor component (typically through its companion <c>.razor.cs</c> partial class) as a
/// Level 2 typed projection-template override (Story 6-2 T2 / D1 / AC3 / AC5 / AC6).
/// SourceTools discovers the marker at compile time, validates it against the targeted
/// <c>[Projection]</c> type, and emits a generated template manifest consumed by the
/// Shell's runtime template registry.
/// </summary>
/// <remarks>
/// <para>
/// Markers must be authored on a class — usually a <c>public partial class MyTemplate</c> in a
/// <c>.razor.cs</c> file. SourceTools intentionally does NOT inspect Razor-generated
/// <c>.g.cs</c> output, so adopters keep the marker in user-authored code.
/// </para>
/// <para>
/// Adopters do not call any runtime registration API for Level 2 — the generated manifest
/// feeds the typed registry automatically.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionTemplateAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionTemplateAttribute"/> class.
    /// </summary>
    /// <param name="projectionType">The projection type the template targets. Must reference
    /// a class annotated with <c>[Projection]</c>.</param>
    /// <param name="expectedContractVersion">The Level 2 contract version the template was
    /// authored against. Should be set to
    /// <see cref="Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateContractVersion.Current"/>
    /// at the time of authoring.</param>
    public ProjectionTemplateAttribute(Type projectionType, int expectedContractVersion) {
        if (projectionType is null) {
            throw new ArgumentNullException(nameof(projectionType));
        }

        ProjectionType = projectionType;
        ExpectedContractVersion = expectedContractVersion;
    }

    /// <summary>Gets the projection type this template targets.</summary>
    public Type ProjectionType { get; }

    /// <summary>Gets the Level 2 contract version the template was authored against.</summary>
    public int ExpectedContractVersion { get; }

    /// <summary>
    /// Gets or sets the optional <see cref="ProjectionRole"/> this template applies to. When
    /// left unset the template matches every role of the projection (Story 6-2 T6). The
    /// SourceTools marker validator inspects whether the <c>Role</c> named argument is
    /// present at the call site (rather than reading this property directly) so the
    /// numeric default value (<see cref="ProjectionRole.ActionQueue"/> = 0) is not mistaken
    /// for an explicit selection.
    /// </summary>
    public ProjectionRole Role { get; set; }
}
