using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Output model for domain registration generation.
/// Each projection contributes its own partial class member.
/// </summary>
public sealed class RegistrationModel : IEquatable<RegistrationModel> {
    public RegistrationModel(
        string boundedContext,
        string typeName,
        string @namespace,
        string? displayLabel,
        bool isCommand = false,
        string? authorizationPolicyName = null,
        bool hasFullPageRoute = false)
        : this(boundedContext, typeName, @namespace, displayLabel, isCommand, authorizationPolicyName, hasFullPageRoute, null) {
    }

    internal RegistrationModel(
        string boundedContext,
        string typeName,
        string @namespace,
        string? displayLabel,
        bool isCommand,
        string? authorizationPolicyName,
        bool hasFullPageRoute,
        NestedProjectionIdentity? nestedProjection) {
        BoundedContext = boundedContext;
        TypeName = typeName;
        Namespace = @namespace;
        DisplayLabel = displayLabel;
        IsCommand = isCommand;
        AuthorizationPolicyName = authorizationPolicyName;
        HasFullPageRoute = hasFullPageRoute;
        NestedProjection = nestedProjection;
    }

    public string BoundedContext { get; }

    public string TypeName { get; }

    private NestedProjectionIdentity? NestedProjection { get; }

    /// <summary>Gets the C# reference to the registered type.</summary>
    internal string TypeReference => NestedProjection?.SourceName ?? TypeName;

    /// <summary>Gets the registration type stem.</summary>
    internal string ArtifactName => NestedProjection?.ArtifactName ?? TypeName;

    public string Namespace { get; }

    /// <summary>Gets the namespace containing the emitted artifacts.</summary>
    internal string GeneratedNamespace => NestedProjection?.GeneratedNamespace ?? Namespace;

    public string? DisplayLabel { get; }

    /// <summary>
    /// Gets a value indicating whether the registered type is a command (otherwise a projection).
    /// Drives the placement of <c>typeof(...).FullName</c> into <c>Commands</c> vs <c>Projections</c>.
    /// </summary>
    public bool IsCommand { get; }

    public string? AuthorizationPolicyName { get; }

    /// <summary>
    /// Gets a value indicating whether this command registration has an emitted full-page route.
    /// </summary>
    public bool HasFullPageRoute { get; }

    public bool Equals(RegistrationModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return BoundedContext == other.BoundedContext
            && TypeName == other.TypeName
            && NestedProjection == other.NestedProjection
            && Namespace == other.Namespace
            && DisplayLabel == other.DisplayLabel
            && IsCommand == other.IsCommand
            && AuthorizationPolicyName == other.AuthorizationPolicyName
            && HasFullPageRoute == other.HasFullPageRoute;
    }

    public override bool Equals(object? obj) => Equals(obj as RegistrationModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (NestedProjection?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DisplayLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsCommand.GetHashCode();
            hash = (hash * 31) + (AuthorizationPolicyName?.GetHashCode() ?? 0);
            hash = (hash * 31) + HasFullPageRoute.GetHashCode();
            return hash;
        }
    }
}
