#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Transforms;

using Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Transforms a DomainModel IR into a RegistrationModel for domain registration generation.
/// </summary>
public static class RegistrationModelTransform
{
    /// <summary>
    /// Transforms a parsed domain model into a registration output model.
    /// </summary>
    /// <param name="model">The domain model IR from the Parse stage.</param>
    /// <returns>A RegistrationModel ready for the Emit stage.</returns>
    public static RegistrationModel Transform(DomainModel model)
    {
        // BoundedContext from attribute, or fallback to namespace last segment
        string boundedContext = model.BoundedContext
            ?? GetNamespaceLastSegment(model.Namespace);

        // DisplayLabel is not currently carried in DomainModel IR;
        // BoundedContextAttribute.DisplayLabel support will be added in a future story.
        return new RegistrationModel(
            boundedContext,
            model.TypeName,
            model.Namespace,
            displayLabel: null);
    }

    private static string GetNamespaceLastSegment(string @namespace)
    {
        if (string.IsNullOrEmpty(@namespace))
        {
            return "Global";
        }

        int lastDot = @namespace.LastIndexOf('.');
        return lastDot >= 0 ? @namespace.Substring(lastDot + 1) : @namespace;
    }
}
