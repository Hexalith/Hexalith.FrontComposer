namespace Hexalith.FrontComposer.Shell.Services.Authorization;

public interface ICommandAuthorizationEvaluator {
    Task<CommandAuthorizationDecision> EvaluateAsync(
        CommandAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
