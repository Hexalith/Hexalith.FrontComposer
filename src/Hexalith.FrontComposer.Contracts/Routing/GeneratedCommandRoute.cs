using System.Text;

namespace Hexalith.FrontComposer.Contracts.Routing;

/// <summary>
/// Builds the canonical route for a source-generated full-page command.
/// </summary>
public static class GeneratedCommandRoute {
    private const string DefaultBoundedContext = "Default";

    /// <summary>
    /// Builds a case-preserving <c>/commands/{BoundedContext}/{CommandTypeName}</c> route.
    /// </summary>
    /// <param name="boundedContext">
    /// The bounded-context route segment. <see langword="null"/> or an empty value uses
    /// <c>Default</c>. A whitespace-only value is invalid.
    /// </param>
    /// <param name="commandTypeName">
    /// The command type name. Fully qualified names are reduced to their final segment.
    /// </param>
    /// <returns>The canonical generated-command route.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when either route segment is whitespace-only or normalizes to a relative path
    /// marker, or when <paramref name="commandTypeName"/> has no simple type-name segment.
    /// </exception>
    public static string Build(string? boundedContext, string commandTypeName) {
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            throw new ArgumentException("The command type name cannot be null, empty, or whitespace.", nameof(commandTypeName));
        }

        int lastDot = commandTypeName.LastIndexOf('.');
        string simpleTypeName = lastDot >= 0 ? commandTypeName.Substring(lastDot + 1) : commandTypeName;
        if (string.IsNullOrWhiteSpace(simpleTypeName)) {
            throw new ArgumentException("The command type name must contain a simple type-name segment.", nameof(commandTypeName));
        }

        if (boundedContext is not null && boundedContext.Length > 0 && string.IsNullOrWhiteSpace(boundedContext)) {
            throw new ArgumentException("The bounded context cannot be whitespace.", nameof(boundedContext));
        }

        string contextSegment = string.IsNullOrEmpty(boundedContext) ? DefaultBoundedContext : boundedContext!;
        string sanitizedContext = SanitizeSegment(contextSegment);
        string sanitizedCommand = SanitizeSegment(simpleTypeName);
        ValidateSafeSegment(sanitizedContext, nameof(boundedContext));
        ValidateSafeSegment(sanitizedCommand, nameof(commandTypeName));
        return "/commands/" + sanitizedContext + "/" + sanitizedCommand;
    }

    private static string SanitizeSegment(string segment) {
        if (segment.Length == 0) {
            return "_";
        }

        StringBuilder builder = new(segment.Length);
        foreach (char character in segment) {
            _ = builder.Append(
                char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_'
                    ? character
                    : '-');
        }

        return builder.ToString();
    }

    private static void ValidateSafeSegment(string segment, string parameterName) {
        if (segment is "." or "..") {
            throw new ArgumentException("Route segments cannot be relative path markers.", parameterName);
        }
    }
}
