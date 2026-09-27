using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.FrontComposer.UI.Tests;

internal sealed class CommandsOnlyRegistry : IFrontComposerRegistry
{
    public void AddNavGroup(string name, string boundedContext)
    {
    }

    public IReadOnlyList<DomainManifest> GetManifests()
        => [new DomainManifest("Accounting", "Accounting", [], ["Accounting.PostCommand"])];

    public void RegisterDomain(DomainManifest manifest)
    {
    }
}
