using System.Collections.Generic;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D14 / T1 — per-command-type field allowlist used by generated forms when
/// mapping server-side validation errors back to <c>ValidationMessageStore</c>. Only field
/// names that resolve EXACTLY to an editable property pass through; everything else falls
/// back to a form-level validation MessageBar (preventing arbitrary ProblemDetails paths
/// from polluting unrelated UI fields).
/// </summary>
/// <remarks>
/// The source generator emits one allowlist per command type alongside the form. Resolution
/// is done by exact (case-insensitive) name match; nested paths (<c>"Address.City"</c>) are
/// only accepted when the command actually exposes a corresponding nested editable field.
/// </remarks>
public interface ICommandValidationFieldAllowlist {
    /// <summary>
    /// Determines whether the supplied server-side field path corresponds to a generated
    /// editable field on the command form.
    /// </summary>
    /// <param name="serverFieldPath">The field path returned by the server (e.g. <c>"Quantity"</c> or <c>"Items[0].Sku"</c>).</param>
    /// <returns><see langword="true"/> when the path maps to an editable field; otherwise <see langword="false"/>.</returns>
    bool TryGetEditableField(string serverFieldPath, out string normalizedFieldName);

    /// <summary>Gets the set of editable field names known to this allowlist.</summary>
    IReadOnlyCollection<string> EditableFields { get; }
}
