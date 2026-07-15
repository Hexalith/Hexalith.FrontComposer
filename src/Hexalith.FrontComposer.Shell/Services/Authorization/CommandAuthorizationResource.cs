using System.Text;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

public sealed record CommandAuthorizationResource(
    Type CommandType,
    string PolicyName,
    string? BoundedContext,
    string DisplayLabel,
    CommandAuthorizationSurface SourceSurface,
    TenantContextSnapshot? TenantContext) {
    private bool PrintMembers(StringBuilder builder) {
        _ = builder.Append("CommandType = ").Append(CommandType.FullName ?? CommandType.Name)
            .Append(", PolicyName = ").Append(PolicyName)
            .Append(", BoundedContext = ").Append(BoundedContext ?? "<none>")
            .Append(", DisplayLabel = ").Append(DisplayLabel)
            .Append(", SourceSurface = ").Append(SourceSurface)
            .Append(", TenantContext = ").Append(TenantContext is null ? "<none>" : "<redacted>");
        return true;
    }
}
