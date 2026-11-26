namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Includes a declared-first member whose value does not sort first.</summary>
public enum NonzeroPriority
{
    /// <summary>The first declared priority.</summary>
    [Obsolete("Use Manual for newly authored commands.", error: true)]
    @default = 12,

    /// <summary>A distinct priority that an adopter can prefill.</summary>
    Manual = 3,
}
