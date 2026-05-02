namespace Hexalith.FrontComposer.Mcp;

public enum FrontComposerMcpFailureCategory {
    None,
    AuthFailed,
    TenantMissing,
    UnknownTool,
    UnknownResource,
    MalformedRequest,
    ValidationFailed,
    UnsupportedSchema,
    CommandRejected,
    QueryRejected,
    Timeout,
    Canceled,
    DownstreamFailed,
    DuplicateDescriptor,
    MissingManifest,
    PolicyGateMissing,
}
