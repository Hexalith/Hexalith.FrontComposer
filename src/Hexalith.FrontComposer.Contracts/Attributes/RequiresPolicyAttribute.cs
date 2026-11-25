using System.Text;

namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Declares the ASP.NET Core authorization policy required to execute a command.
/// Policy registration, claim mapping, and authorization handlers remain host concerns.
/// </summary>
/// <remarks>
/// The attribute applies only to the command class that declares it. Annotate each derived
/// command that requires a policy, even when its base class declares the same policy.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RequiresPolicyAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresPolicyAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The non-empty host authorization policy name.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyName"/> is null/empty/whitespace, contains at least one
    /// character that is not a letter, digit, '.', ':', '_', or '-', contains no
    /// alphanumeric character at all (e.g., <c>"-"</c> or <c>"::"</c>), or is not in Unicode
    /// Normalization Form C. Mirrors the source-generator HFC1056 validation, which spans the command
    /// parser's character rule and the generator's normalization rule, so reflection callers (Epic 8 MCP
    /// enumeration, custom command discovery) cannot bypass the well-formedness contract. The normalization
    /// rule needs ICU data: under invariant globalization every name is reported as normalized.
    /// </exception>
    public RequiresPolicyAttribute(string policyName) {
        if (string.IsNullOrWhiteSpace(policyName)) {
            throw new ArgumentException("Policy name must not be empty.", nameof(policyName));
        }

        string trimmed = policyName.Trim();
        if (!IsWellFormed(trimmed)) {
            throw new ArgumentException(
                "Policy name must contain only letters, digits, '.', ':', '_', or '-', include at least one alphanumeric character, and use Unicode Normalization Form C.",
                nameof(policyName));
        }

        PolicyName = trimmed;
    }

    /// <summary>
    /// Gets the host authorization policy name required for the annotated command.
    /// </summary>
    public string PolicyName { get; }

    private static bool IsWellFormed(string value) {
        bool hasAlphanumeric = false;
        foreach (char c in value) {
            if (char.IsLetterOrDigit(c)) {
                hasAlphanumeric = true;
                continue;
            }

            if (c is not ('.' or ':' or '_' or '-')) {
                return false;
            }
        }

        // Pass 3 — punctuation-only names like "-", ".", ":", or "::" pass the per-character check
        // but produce no meaningful policy lookup. Require at least one alphanumeric character.
        // DW-877 — policy lookup is ordinal, so a decomposed name never matches its composed registration.
        return hasAlphanumeric && value.IsNormalized(NormalizationForm.FormC);
    }
}
