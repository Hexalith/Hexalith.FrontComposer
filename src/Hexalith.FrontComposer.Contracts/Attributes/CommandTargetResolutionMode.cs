namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Selects how a generated command form resolves its explicit fresh-row target.
/// </summary>
public enum CommandTargetResolutionMode
{
    /// <summary>Resolve the target through one registered typed identity provider.</summary>
    Provider,

    /// <summary>Copy the framework-owned generated projection-row snapshot captured before dispatch.</summary>
    SameAsSource,
}
