using NSubstitute;

namespace Hexalith.FrontComposer.Shell.Tests;

internal static class ArgEx
{
    public static T Is<T>(Func<T, bool> predicate)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Arg.Is<T>(candidate => candidate != null && predicate(candidate));
    }
}
