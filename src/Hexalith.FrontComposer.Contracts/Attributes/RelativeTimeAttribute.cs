namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Requests compact relative-time rendering for a DateTime-like projection property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class RelativeTimeAttribute : Attribute {
    /// <summary>The default relative rendering window in days.</summary>
    public const int DefaultRelativeWindowDays = 7;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeTimeAttribute"/> class.
    /// </summary>
    /// <param name="relativeWindowDays">Relative-time window in days; supported range is 1..365.</param>
    public RelativeTimeAttribute(int relativeWindowDays = DefaultRelativeWindowDays) {
        if (relativeWindowDays < 1 || relativeWindowDays > 365) {
            throw new ArgumentOutOfRangeException(nameof(relativeWindowDays), relativeWindowDays, "Relative-time window must be between 1 and 365 days.");
        }

        RelativeWindowDays = relativeWindowDays;
    }

    /// <summary>Gets the relative-time window in days.</summary>
    public int RelativeWindowDays { get; }
}
