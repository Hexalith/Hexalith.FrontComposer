using System;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D5 — thrown by <see cref="ICommandService"/> implementations when the server
/// returns HTTP 400 Bad Request with validation feedback. Generated forms map
/// <see cref="Problem"/>'s <c>ValidationErrors</c> through their allowlisted property names
/// and route unknown / global errors to a form-level validation MessageBar while preserving
/// user-entered values.
/// </summary>
/// <remarks>
/// Distinct from <see cref="CommandRejectedException"/>: validation describes correctable
/// input shape (field-level), whereas a domain rejection (HTTP 409) is a business outcome
/// after server processing.
/// </remarks>
public class CommandValidationException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandValidationException"/> class.
    /// </summary>
    /// <param name="problem">Parsed ProblemDetails (validation errors + plain-text title/detail).</param>
    public CommandValidationException(ProblemDetailsPayload problem)
        : base(problem?.Title ?? "Command validation failed.") {
        Problem = problem ?? throw new ArgumentNullException(nameof(problem));
    }

    /// <summary>Gets the parsed ProblemDetails payload (plain text + validation map).</summary>
    public ProblemDetailsPayload Problem { get; }
}
