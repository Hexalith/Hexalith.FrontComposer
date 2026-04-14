namespace Hexalith.FrontComposer.Shell.Registration;

using System.Reflection;
using System.Runtime.ExceptionServices;

using Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Encapsulates a deferred domain registration discovered from a generated Registration class.
/// Registered as a singleton in DI by <c>AddHexalithDomain&lt;T&gt;()</c> and consumed
/// by <see cref="FrontComposerRegistry"/> on construction.
/// </summary>
internal sealed class DomainRegistrationAction
{
    private readonly Action<IFrontComposerRegistry> _apply;

    public DomainRegistrationAction(MethodInfo registerMethod)
        : this(registry => registerMethod.Invoke(null, [registry]))
    {
    }

    public DomainRegistrationAction(Action<IFrontComposerRegistry> apply)
    {
        _apply = apply;
    }

    public void Apply(IFrontComposerRegistry registry)
    {
        try
        {
            _apply(registry);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }
}
