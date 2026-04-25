using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.AspNetCore.Components.Forms;

namespace Hexalith.FrontComposer.Shell.Services.Validation;

/// <summary>
/// Story 5-2 D5 / T5 — translates a <see cref="CommandValidationException"/> into
/// <see cref="ValidationMessageStore"/> entries on a generated command form's
/// <see cref="EditContext"/>. Field paths are mapped through an
/// <see cref="ICommandValidationFieldAllowlist"/>; unknown / nested / hostile paths and
/// global errors collapse into <see cref="ResultUnmappedMessages"/> for form-level rendering.
/// </summary>
/// <remarks>
/// Stale server-side messages MUST be cleared before invoking
/// <see cref="Apply"/>: the convention is to call <see cref="ValidationMessageStore.Clear()"/>
/// in the form's <c>OnFieldChanged</c> handler and again before each new submit attempt.
/// </remarks>
public sealed class ServerValidationApplicator {
    /// <summary>
    /// Applies validation messages from the supplied exception to the message store. Returns
    /// any global / unmapped error strings that should be surfaced through a form-level
    /// validation MessageBar (per AC2 / D5).
    /// </summary>
    /// <param name="messageStore">The store associated with the form's EditContext.</param>
    /// <param name="exception">The raised <see cref="CommandValidationException"/>.</param>
    /// <param name="allowlist">The per-command allowlist (typically <see cref="ReflectionCommandValidationFieldAllowlist{TCommand}"/>).</param>
    /// <param name="model">The form's editable model instance (used to construct <see cref="FieldIdentifier"/>s).</param>
    /// <returns>Plain-text messages destined for the form-level MessageBar (never null; may be empty).</returns>
    public static IReadOnlyList<string> Apply(
        ValidationMessageStore messageStore,
        CommandValidationException exception,
        ICommandValidationFieldAllowlist allowlist,
        object model) {
        System.ArgumentNullException.ThrowIfNull(messageStore);
        System.ArgumentNullException.ThrowIfNull(exception);
        System.ArgumentNullException.ThrowIfNull(allowlist);
        System.ArgumentNullException.ThrowIfNull(model);

        List<string> formLevel = new();
        foreach (string global in exception.Problem.GlobalErrors) {
            formLevel.Add(global);
        }

        foreach (KeyValuePair<string, IReadOnlyList<string>> entry in exception.Problem.ValidationErrors) {
            if (allowlist.TryGetEditableField(entry.Key, out string normalized)) {
                FieldIdentifier identifier = new(model, normalized);
                foreach (string message in entry.Value) {
                    messageStore.Add(identifier, message);
                }
            }
            else {
                // Map unknown / nested-hostile paths to the form-level MessageBar so they
                // remain visible without polluting an unrelated allowlisted field.
                foreach (string message in entry.Value) {
                    formLevel.Add(message);
                }
            }
        }

        if (formLevel.Count == 0
            && exception.Problem.Detail is { Length: > 0 } detail
            && exception.Problem.ValidationErrors.Count == 0) {
            // ProblemDetails carried no field map but did include a top-level detail string —
            // surface it form-level so the user gets something actionable.
            formLevel.Add(detail);
        }

        return formLevel;
    }

    /// <summary>
    /// Property name retained for documentation symmetry — there is no separate "result"
    /// type because the method's return value carries the unmapped messages directly.
    /// </summary>
    public static IReadOnlyList<string> ResultUnmappedMessages { get; } = System.Array.Empty<string>();
}
