namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// Classifies typed evidence about whether a terminal command changed projection material.
/// </summary>
public enum CommandMateriality
{
    /// <summary>The adapter cannot prove materiality without inference.</summary>
    Unknown = 0,

    /// <summary>The adapter has affirmative evidence that projection-affecting work occurred.</summary>
    Material = 1,

    /// <summary>The adapter has affirmative evidence that no projection-affecting work occurred.</summary>
    NoOp = 2,
}
