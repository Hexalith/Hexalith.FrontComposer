using System.ComponentModel;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

[Command]
[BoundedContext("TestCommands")]
public sealed class ConstructorDefaultsCommand {
    public ConstructorDefaultsCommand() => RelatedReference = ExternalReference;

    public string MessageId { get; set; } = string.Empty;

    [DerivedFrom(DerivedFromSource.Context)]
    public string ExternalReference { get; set; } = Guid.NewGuid().ToString();

    [DerivedFrom(DerivedFromSource.Context)]
    public string RelatedReference { get; set; }

    [DerivedFrom(DerivedFromSource.Context)]
    public string Constant { get; set; } = "constant";

    [DefaultValue("explicit")]
    [DerivedFrom(DerivedFromSource.Context)]
    public string Override { get; set; } = "constructor";
}
