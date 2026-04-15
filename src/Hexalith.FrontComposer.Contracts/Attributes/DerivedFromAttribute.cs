namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a command property as being derived from an infrastructure source
/// (e.g., context, user session, timestamp). Derived properties are excluded from
/// auto-generated command forms.
/// </summary>
/// <remarks>
/// Applied per-property. The source generator also classifies properties named
/// <c>MessageId</c>, <c>CommandId</c>, <c>CorrelationId</c>, <c>TenantId</c>, <c>UserId</c>,
/// <c>Timestamp</c>, <c>CreatedAt</c>, and <c>ModifiedAt</c> as derivable without
/// requiring this attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class DerivedFromAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedFromAttribute"/> class.
    /// </summary>
    /// <param name="source">The infrastructure source that supplies the property value.</param>
    public DerivedFromAttribute(DerivedFromSource source) {
        Source = source;
    }

    /// <summary>
    /// Gets the infrastructure source that supplies the property value at dispatch time.
    /// </summary>
    public DerivedFromSource Source { get; }
}

/// <summary>
/// Infrastructure sources that populate derivable command properties.
/// </summary>
public enum DerivedFromSource {
    /// <summary>Derived from the current request/session context.</summary>
    Context,

    /// <summary>Derived from the authenticated user identity.</summary>
    User,

    /// <summary>Derived from the current UTC timestamp.</summary>
    Timestamp,

    /// <summary>Derived from a ULID generated at dispatch time.</summary>
    MessageId,
}
