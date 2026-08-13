namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Declares the one projection target that may receive a fresh-row indicator for a command.
/// </summary>
/// <param name="projectionType">The explicitly targeted <see cref="ProjectionAttribute"/> type.</param>
/// <param name="resolutionMode">The target-identity resolution mode.</param>
/// <param name="changeKind">The declared material change kind.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class CommandTargetAttribute(
    Type projectionType,
    CommandTargetResolutionMode resolutionMode,
    CommandTargetChangeKind changeKind) : Attribute
{
    /// <summary>Gets the explicitly targeted projection type.</summary>
    public Type ProjectionType { get; } = projectionType ?? throw new ArgumentNullException(nameof(projectionType));

    /// <summary>Gets the target-identity resolution mode.</summary>
    public CommandTargetResolutionMode ResolutionMode { get; } = resolutionMode;

    /// <summary>Gets the declared material change kind.</summary>
    public CommandTargetChangeKind ChangeKind { get; } = changeKind;

    /// <summary>
    /// Gets or sets the optional declaration-fixed canonical view key. A provider value must match it.
    /// </summary>
    public string? ViewKey { get; set; }

    /// <summary>
    /// Gets or sets the optional declaration-fixed expected status. A provider value must match it.
    /// </summary>
    public string? ExpectedStatus { get; set; }
}
