
using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Transforms a DomainModel IR into a RegistrationModel for domain registration generation.
/// </summary>
public static class RegistrationModelTransform {
    /// <summary>
    /// Transforms a parsed domain model into a registration output model.
    /// </summary>
    /// <param name="model">The domain model IR from the Parse stage.</param>
    /// <returns>A RegistrationModel ready for the Emit stage.</returns>
    public static RegistrationModel Transform(DomainModel model) {
        // BoundedContext from attribute, or fallback to namespace last segment
        string boundedContext = model.BoundedContext
            ?? GetNamespaceLastSegment(model.Namespace);

        return new RegistrationModel(
            boundedContext,
            model.TypeName,
            model.Namespace,
            model.BoundedContextDisplayLabel);
    }

    private static string GetNamespaceLastSegment(string @namespace) {
        if (string.IsNullOrEmpty(@namespace)) {
            return "Global";
        }

        int lastDot = @namespace.LastIndexOf('.');
        return lastDot >= 0 ? @namespace.Substring(lastDot + 1) : @namespace;
    }
}
