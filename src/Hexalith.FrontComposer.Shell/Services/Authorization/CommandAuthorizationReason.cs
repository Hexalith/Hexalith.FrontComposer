namespace Hexalith.FrontComposer.Shell.Services.Authorization;

public enum CommandAuthorizationReason {
    None,
    NoPolicy,
    Denied,
    Unauthenticated,
    Pending,
    MissingService,
    MissingPolicy,
    StaleTenantContext,
    Canceled,
    HandlerFailed,
    CatalogInconsistent,
}
