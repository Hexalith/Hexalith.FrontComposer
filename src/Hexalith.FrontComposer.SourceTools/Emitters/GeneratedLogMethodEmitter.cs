using System;
using System.Globalization;
using System.Text;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Emits a cached <c>LoggerMessage.Define</c> delegate plus its private static wrapper method into a
/// generated component partial class (Story 11.21 CA1848/CA1873 burn-down).
/// </summary>
/// <remarks>
/// The compile-time <c>[LoggerMessage]</c> attribute cannot be used from a source generator: Roslyn
/// never feeds one generator's output into another, so the compile-time logging generator does not
/// observe an emitted <c>static partial void</c> declaration and the consumer build fails with
/// CS8795 (verified 2026-08-07 on SDK 10.0.302 / Roslyn 5.6.0). Cached
/// <c>Microsoft.Extensions.Logging.LoggerMessage.Define</c> delegates are the equivalent CA1848
/// remedy -- they are what the compile-time generator itself produces -- and they keep the
/// <c>IsEnabled</c> short-circuit inside the delegate so disabled levels stay cheap.
/// </remarks>
internal static class GeneratedLogMethodEmitter {
    /// <summary>
    /// <c>LoggerMessage.Define</c> is declared for at most six generic message arguments. Emitting a
    /// seventh binds a method that does not exist and the adopter build fails with CS0308/CS1501, so
    /// the limit is enforced here rather than discovered in generated output.
    /// </summary>
    internal const int MaxMessageArguments = 6;

    private const string LoggerType = "global::Microsoft.Extensions.Logging.ILogger";

    private const string ExceptionType = "global::System.Exception";

    /// <summary>
    /// Appends one cached delegate field and its private static wrapper method to a generated
    /// partial class body.
    /// </summary>
    /// <param name="sb">Target buffer, positioned inside the generated class body.</param>
    /// <param name="methodName">PascalCase wrapper method name, for example <c>LogCommandSubmitted</c>.</param>
    /// <param name="eventId">Deterministic event id taken from the generated-code 5900+ band.</param>
    /// <param name="eventName">Deterministic event name.</param>
    /// <param name="level">Unqualified <c>LogLevel</c> member name, for example <c>Warning</c>.</param>
    /// <param name="messageTemplate">
    /// Raw message template. Its text and placeholder set must stay identical to the direct
    /// <c>ILogger</c> call it replaces, and the placeholder count must equal
    /// <paramref name="parameters"/>'s length.
    /// </param>
    /// <param name="hasException">
    /// When <see langword="true"/> the wrapper takes an exception as its second parameter, matching
    /// the repository rule that places <c>ILogger</c> first and <c>Exception</c> second.
    /// </param>
    /// <param name="parameters">Ordered template arguments as fully qualified type / camelCase name pairs.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="sb"/>, <paramref name="messageTemplate"/>, or <paramref name="parameters"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="parameters"/> holds more than <see cref="MaxMessageArguments"/> entries, which
    /// <c>LoggerMessage.Define</c> cannot bind.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="messageTemplate"/> is malformed, or its placeholder count differs from
    /// <paramref name="parameters"/>'s length — either shape emits source that does not compile or a
    /// message whose holes never bind.
    /// </exception>
    internal static void Emit(
        StringBuilder sb,
        string methodName,
        int eventId,
        string eventName,
        string level,
        string messageTemplate,
        bool hasException,
        params (string Type, string Name)[] parameters) {
        if (sb is null) {
            throw new ArgumentNullException(nameof(sb));
        }

        if (messageTemplate is null) {
            throw new ArgumentNullException(nameof(messageTemplate));
        }

        if (parameters is null) {
            throw new ArgumentNullException(nameof(parameters));
        }

        ValidateArguments(methodName, messageTemplate, parameters);

        string fieldName = "_" + char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);

        _ = sb.Append("    private static readonly global::System.Action<").Append(LoggerType);
        foreach ((string type, string _) in parameters) {
            _ = sb.Append(", ").Append(type);
        }

        _ = sb.Append(", ").Append(ExceptionType).Append("?> ").Append(fieldName).AppendLine(" =");
        _ = sb.Append("        global::Microsoft.Extensions.Logging.LoggerMessage.Define");
        if (parameters.Length > 0) {
            _ = sb.Append('<');
            for (int i = 0; i < parameters.Length; i++) {
                if (i > 0) {
                    _ = sb.Append(", ");
                }

                _ = sb.Append(parameters[i].Type);
            }

            _ = sb.Append('>');
        }

        _ = sb.AppendLine("(");
        _ = sb.AppendLine("            global::Microsoft.Extensions.Logging.LogLevel." + level + ",");
        _ = sb.AppendLine(
            "            new global::Microsoft.Extensions.Logging.EventId("
            + eventId.ToString(CultureInfo.InvariantCulture)
            + ", \"" + eventName + "\"),");
        _ = sb.AppendLine("            \"" + GeneratedLiteral.Escape(messageTemplate) + "\");");
        _ = sb.AppendLine();

        _ = sb.Append("    private static void ").Append(methodName).Append('(').Append(LoggerType).Append(" logger");
        if (hasException) {
            _ = sb.Append(", ").Append(ExceptionType).Append(" exception");
        }

        foreach ((string type, string name) in parameters) {
            _ = sb.Append(", ").Append(type).Append(' ').Append(name);
        }

        _ = sb.AppendLine(")");
        _ = sb.Append("        => ").Append(fieldName).Append("(logger");
        foreach ((string _, string name) in parameters) {
            _ = sb.Append(", ").Append(name);
        }

        _ = sb.Append(", ").Append(hasException ? "exception" : "null").AppendLine(");");
        _ = sb.AppendLine();
    }

    /// <summary>
    /// Counts the message-template holes in <paramref name="messageTemplate"/>, treating <c>{{</c>
    /// and <c>}}</c> as escaped braces rather than holes.
    /// </summary>
    /// <param name="messageTemplate">Raw message template.</param>
    /// <returns>The number of <c>{Placeholder}</c> holes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messageTemplate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The template contains an unterminated hole.</exception>
    internal static int CountTemplatePlaceholders(string messageTemplate) {
        if (messageTemplate is null) {
            throw new ArgumentNullException(nameof(messageTemplate));
        }

        int count = 0;
        for (int index = 0; index < messageTemplate.Length; index++) {
            char current = messageTemplate[index];
            if (current == '}') {
                if (index + 1 < messageTemplate.Length && messageTemplate[index + 1] == '}') {
                    index++;
                }

                continue;
            }

            if (current != '{') {
                continue;
            }

            if (index + 1 < messageTemplate.Length && messageTemplate[index + 1] == '{') {
                index++;
                continue;
            }

            int close = messageTemplate.IndexOf('}', index + 1);
            if (close < 0) {
                throw new ArgumentException(
                    "The generated log message template has an unterminated placeholder: '"
                    + messageTemplate + "'.",
                    nameof(messageTemplate));
            }

            count++;
            index = close;
        }

        return count;
    }

    /// <summary>
    /// Fails the generation rather than emitting a logging helper that cannot compile or whose
    /// message holes cannot bind.
    /// </summary>
    /// <remarks>
    /// Both conditions are silent in the emitter but loud in the adopter build: more than
    /// <see cref="MaxMessageArguments"/> arguments emits a <c>LoggerMessage.Define</c> overload that
    /// does not exist, and a placeholder/parameter mismatch emits a template whose holes never render
    /// the values that were passed. Throwing here surfaces the defect in this repository's emitter
    /// tests instead of in a consumer's generated output.
    /// </remarks>
    private static void ValidateArguments(
        string methodName,
        string messageTemplate,
        (string Type, string Name)[] parameters) {
        if (parameters.Length > MaxMessageArguments) {
            throw new ArgumentOutOfRangeException(
                nameof(parameters),
                parameters.Length,
                "LoggerMessage.Define binds at most "
                + MaxMessageArguments.ToString(CultureInfo.InvariantCulture)
                + " message arguments; generated log method '" + methodName + "' requested "
                + parameters.Length.ToString(CultureInfo.InvariantCulture) + ".");
        }

        int placeholders = CountTemplatePlaceholders(messageTemplate);
        if (placeholders != parameters.Length) {
            throw new ArgumentException(
                "Generated log method '" + methodName + "' has "
                + parameters.Length.ToString(CultureInfo.InvariantCulture)
                + " parameter(s) but its message template has "
                + placeholders.ToString(CultureInfo.InvariantCulture)
                + " placeholder(s): '" + messageTemplate + "'.",
                nameof(messageTemplate));
        }
    }
}
