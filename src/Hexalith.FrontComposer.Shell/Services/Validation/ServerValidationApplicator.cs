using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.AspNetCore.Components.Forms;

namespace Hexalith.FrontComposer.Shell.Services.Validation;

/// <summary>
/// Story 5-2 D5 / T5 — translates a <see cref="CommandValidationException"/> into
/// <see cref="ValidationMessageStore"/> entries on a generated command form's
/// <see cref="EditContext"/>. Field paths are mapped through an
/// <see cref="ICommandValidationFieldAllowlist"/>; unknown / nested / hostile paths and
/// global errors are returned as unmapped, support-safe form-level messages. Story 13.3 —
/// <see cref="ApplyRejection"/> returns a <see cref="ServerValidationApplicationResult"/> that also
/// states whether any allowlisted field received a message (mapped versus unmapped recovery).
/// </summary>
/// <remarks>
/// Stale server-side messages MUST be cleared before invoking
/// <see cref="Apply"/>: the convention is to call <see cref="ValidationMessageStore.Clear()"/>
/// in the form's <c>OnFieldChanged</c> handler and again before each new submit attempt.
/// </remarks>
public sealed class ServerValidationApplicator {
    /// <summary>
    /// Applies validation messages from the supplied exception to the message store. Returns
    /// any global / unmapped error strings, which a generated form lists as unlinked entries in
    /// its focused validation summary (Story 13.3 AM-18; originally a form-level MessageBar per AC2 / D5).
    /// </summary>
    /// <param name="messageStore">The store associated with the form's EditContext.</param>
    /// <param name="exception">The raised <see cref="CommandValidationException"/>.</param>
    /// <param name="allowlist">The per-command allowlist (typically <see cref="ReflectionCommandValidationFieldAllowlist{TCommand}"/>).</param>
    /// <param name="model">The form's editable model instance (used to construct <see cref="FieldIdentifier"/>s).</param>
    /// <returns>Plain-text form-level messages (never null; may be empty).</returns>
    public static IReadOnlyList<string> Apply(
        ValidationMessageStore messageStore,
        CommandValidationException exception,
        ICommandValidationFieldAllowlist allowlist,
        object model) {
        System.ArgumentNullException.ThrowIfNull(messageStore);
        System.ArgumentNullException.ThrowIfNull(exception);
        System.ArgumentNullException.ThrowIfNull(allowlist);
        System.ArgumentNullException.ThrowIfNull(model);

        return ApplyMap(
            messageStore,
            exception.Problem.ValidationErrors,
            exception.Problem.GlobalErrors,
            exception.Problem.Detail,
            allowlist,
            model).UnmappedMessages;
    }

    /// <summary>
    /// Applies the bounded field map carried by an authoritative command rejection.
    /// </summary>
    /// <param name="messageStore">The validation store associated with the form.</param>
    /// <param name="exception">The authoritative rejection.</param>
    /// <param name="allowlist">The generated command's safe editable-field allowlist.</param>
    /// <param name="model">The current editable model.</param>
    /// <returns>A result that explicitly distinguishes mapped from unmapped recovery.</returns>
    public static ServerValidationApplicationResult ApplyRejection(
        ValidationMessageStore messageStore,
        CommandRejectedException exception,
        ICommandValidationFieldAllowlist allowlist,
        object model) {
        System.ArgumentNullException.ThrowIfNull(exception);
        return ApplyMap(
            messageStore,
            exception.Problem.ValidationErrors,
            exception.Problem.GlobalErrors,
            exception.Problem.Detail,
            allowlist,
            model);
    }

    private static ServerValidationApplicationResult ApplyMap(
        ValidationMessageStore messageStore,
        IReadOnlyDictionary<string, IReadOnlyList<string>> validationErrors,
        IReadOnlyList<string> globalErrors,
        string? detail,
        ICommandValidationFieldAllowlist allowlist,
        object model) {
        System.ArgumentNullException.ThrowIfNull(messageStore);
        System.ArgumentNullException.ThrowIfNull(validationErrors);
        System.ArgumentNullException.ThrowIfNull(globalErrors);
        System.ArgumentNullException.ThrowIfNull(allowlist);
        System.ArgumentNullException.ThrowIfNull(model);

        List<string> formLevel = [.. globalErrors];
        int mappedFieldCount = 0;

        foreach (KeyValuePair<string, IReadOnlyList<string>> entry in validationErrors) {
            if (allowlist.TryGetEditableField(entry.Key, out string normalized)) {
                FieldIdentifier identifier = new(model, normalized);
                bool mappedAny = false;
                foreach (string message in entry.Value) {
                    messageStore.Add(identifier, message);
                    mappedAny = true;
                }

                if (mappedAny) {
                    mappedFieldCount++;
                }
            }
            else {
                // Map unknown / nested-hostile paths to form-level messages so they remain
                // visible without polluting an unrelated allowlisted field.
                foreach (string message in entry.Value) {
                    formLevel.Add(message);
                }
            }
        }

        if (formLevel.Count == 0
            && detail is { Length: > 0 }
            && validationErrors.Count == 0) {
            // ProblemDetails carried no field map but did include a top-level detail string —
            // surface it form-level so the user gets something actionable.
            formLevel.Add(detail);
        }

        return new ServerValidationApplicationResult(formLevel, mappedFieldCount);
    }

    /// <summary>
    /// Always empty; retained for source compatibility. Unmapped messages are returned by
    /// <see cref="Apply"/> and carried by <see cref="ServerValidationApplicationResult.UnmappedMessages"/>.
    /// </summary>
    public static IReadOnlyList<string> ResultUnmappedMessages { get; } = System.Array.Empty<string>();
}
