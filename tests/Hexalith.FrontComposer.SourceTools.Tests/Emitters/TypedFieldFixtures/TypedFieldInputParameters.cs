namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Captures the actual component type and generated field parameter payloads.</summary>
/// <param name="Type">The actual generated Fluent component type.</param>
/// <param name="Values">The generated parameter values.</param>
internal sealed record TypedFieldInputParameters(Type Type, Dictionary<string, object?> Values);
