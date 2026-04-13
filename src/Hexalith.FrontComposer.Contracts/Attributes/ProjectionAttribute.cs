namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a class as a projection (read model) that the source generator will process
/// to emit UI components and registration code.
/// </summary>
/// <remarks>
/// For display metadata (Name, Description, Order, etc.), use
/// <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> from the BCL.
/// No custom display attribute is needed — the source generator reads DisplayAttribute
/// via Roslyn symbol analysis.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionAttribute : Attribute
{
}
