using Counter.Specimens;
using Counter.Specimens.Domain;

using Hexalith.FrontComposer.Shell.Services.Authorization;

namespace Counter.Web;

/// <summary>Test-only submit policy seam; all other authorization uses the real evaluator.</summary>
internal sealed class AccessibilitySpecimenAuthorizationEvaluator(
    CommandAuthorizationEvaluator inner,
    AccessibilitySpecimenState scenarios) : ICommandAuthorizationEvaluator
{
    /// <inheritdoc />
    public Task<CommandAuthorizationDecision> EvaluateAsync(CommandAuthorizationRequest request, CancellationToken cancellationToken = default)
        => request.CommandType == typeof(AccessibilityZeroFieldCommand)
            && request.Command is not null && scenarios.Outcome == "policy"
                ? Task.FromResult(CommandAuthorizationDecision.Denied("accessibility-specimen"))
                : inner.EvaluateAsync(request, cancellationToken);
}
