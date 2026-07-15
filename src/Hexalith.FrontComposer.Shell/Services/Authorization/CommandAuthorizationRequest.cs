using System.Text;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

public sealed record CommandAuthorizationRequest(
    Type CommandType,
    string? PolicyName,
    object? Command,
    string? BoundedContext,
    string DisplayLabel,
    CommandAuthorizationSurface SourceSurface = CommandAuthorizationSurface.DirectDispatch) {
    private bool PrintMembers(StringBuilder builder) {
        _ = builder.Append("CommandType = ").Append(CommandType.FullName ?? CommandType.Name)
            .Append(", PolicyName = ").Append(PolicyName ?? "<none>")
            .Append(", Command = <redacted>")
            .Append(", BoundedContext = ").Append(BoundedContext ?? "<none>")
            .Append(", DisplayLabel = ").Append(DisplayLabel)
            .Append(", SourceSurface = ").Append(SourceSurface);
        return true;
    }
}
