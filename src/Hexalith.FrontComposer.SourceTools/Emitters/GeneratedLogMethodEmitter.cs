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
    internal static void Emit(
        StringBuilder sb,
        string methodName,
        int eventId,
        string eventName,
        string level,
        string messageTemplate,
        bool hasException,
        params (string Type, string Name)[] parameters) {
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
}
