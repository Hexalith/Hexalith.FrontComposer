namespace Hexalith.FrontComposer.Contracts.DevMode;

/// <summary>
/// Contract version for the dev-mode component-tree read model.
/// </summary>
public static class ComponentTreeContractVersion {
    /// <summary>Current major version.</summary>
    public const int Major = 1;

    /// <summary>Current minor version.</summary>
    public const int Minor = 0;

    /// <summary>Current build version.</summary>
    public const int Build = 0;

    /// <summary>Current packed version: <c>major * 1,000,000 + minor * 1,000 + build</c>.</summary>
    public const int Current = (Major * 1_000_000) + (Minor * 1_000) + Build;
}
