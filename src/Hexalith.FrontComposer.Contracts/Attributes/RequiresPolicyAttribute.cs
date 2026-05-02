namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Declares the ASP.NET Core authorization policy required to execute a command.
/// Policy registration, claim mapping, and authorization handlers remain host concerns.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RequiresPolicyAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresPolicyAttribute"/> class.
    /// </summary>
    /// <param name="policyName">The non-empty host authorization policy name.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyName"/> is null/empty/whitespace, contains at least one
    /// character that is not a letter, digit, '.', ':', '_', or '-', or contains no
    /// alphanumeric character at all (e.g., <c>"-"</c> or <c>"::"</c>). Mirrors the source-generator
    /// HFC1056 validation so reflection callers (Epic 8 MCP enumeration, custom command discovery)
    /// cannot bypass the well-formedness contract.
    /// </exception>
    public RequiresPolicyAttribute(string policyName) {
        if (string.IsNullOrWhiteSpace(policyName)) {
            throw new ArgumentException("Policy name must not be empty.", nameof(policyName));
        }

        string trimmed = policyName.Trim();
        if (!IsWellFormed(trimmed)) {
            throw new ArgumentException(
                "Policy name must contain only letters, digits, '.', ':', '_', or '-' and include at least one alphanumeric character.",
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
        return hasAlphanumeric;
    }
}
