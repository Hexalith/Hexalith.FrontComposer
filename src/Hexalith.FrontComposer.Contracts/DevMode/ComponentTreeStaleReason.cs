namespace Hexalith.FrontComposer.Contracts.DevMode;

/// <summary>
/// Reasons a dev-mode component-tree snapshot can no longer be trusted for current starter emission.
/// </summary>
public enum ComponentTreeStaleReason {
    /// <summary>Required annotation metadata was missing.</summary>
    MissingMetadata,

    /// <summary>The generated component-tree contract version differs from the running Shell contract.</summary>
    ContractVersionMismatch,

    /// <summary>The descriptor hash differs from the currently registered descriptor metadata.</summary>
    DescriptorHashMismatch,

    /// <summary>The source component identity differs from the currently rendered component identity.</summary>
    SourceComponentIdentityMismatch,

    /// <summary>The generated contract version and running contract version drifted apart.</summary>
    GeneratedRunningContractDrift,
}
