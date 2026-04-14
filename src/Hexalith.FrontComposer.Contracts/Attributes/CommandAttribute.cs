namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a class as a command that the source generator will process
/// to emit form components and dispatch registration code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute {
}
