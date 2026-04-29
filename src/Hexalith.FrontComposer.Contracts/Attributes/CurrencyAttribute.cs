namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Requests current-culture currency rendering for a numeric projection property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class CurrencyAttribute : Attribute;
