namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

internal static class CommandTestSources {
    internal const string SingleStringFieldCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class SetNameCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}";

    internal const string MultiFieldCommand = @"
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[BoundedContext(""Orders"")]
public class PlaceOrderCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public bool Expedited { get; set; }
    public DateTime OrderedAt { get; set; }
}";

    internal const string RecordPositionalCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public record IncrementCounterCommand(string MessageId = """", int Amount = 0);";

    internal const string RecordPositionalCommand_NoDefaults = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public record IncrementCounterCommandNoDefaults(string MessageId, int Amount);";

    internal const string RecordPropertyCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public record DecrementCounterCommand
{
    public string MessageId { get; init; } = string.Empty;
    public int Amount { get; init; }
}";

    internal const string EmptyCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class EmptyCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal const string MissingMessageIdCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class NoMessageIdCommand
{
    public string Name { get; set; } = string.Empty;
}";

    internal const string DerivedFromAttributeCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class AnnotatedDerivedCommand
{
    public string MessageId { get; set; } = string.Empty;
    [DerivedFrom(DerivedFromSource.Context)]
    public string RequestIp { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}";

    internal const string WellKnownDerivableCommand = @"
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class KitchenSinkCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string CommandId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string Payload { get; set; } = string.Empty;
}";

    internal const string BaseRecordWithMessageId = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

public abstract record CommandBase(string MessageId = """");

[Command]
public record ChildCommand(string MessageId = """", string Extra = """") : CommandBase(MessageId);";

    internal const string BaseClassWithMessageId = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

public class CommandBaseClass
{
    public string MessageId { get; set; } = string.Empty;
}

[Command]
public class ChildClassCommand : CommandBaseClass
{
    public string Extra { get; set; } = string.Empty;
}";

    internal const string DisplayAttributeCommand = @"
using System.ComponentModel.DataAnnotations;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[Display(Name=""Place Order"")]
public class DisplayLabeledCommand
{
    public string MessageId { get; set; } = string.Empty;
    [Display(Name=""Customer Name"")]
    public string CustomerName { get; set; } = string.Empty;
}";

    internal const string UnsupportedFieldCommand = @"
using System.Collections.Generic;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class UnsupportedCommand
{
    public string MessageId { get; set; } = string.Empty;
    public object Raw { get; set; } = new();
    public Dictionary<string, string> Map { get; set; } = new();
}";

    internal const string PolicyProtectedCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""OrderApprover"")]
public class ApproveOrderCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}";

    internal const string EmptyPolicyCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""   "")]
public class EmptyPolicyCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal const string DuplicatePolicyCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""OrderApprover"")]
[RequiresPolicy(""OrderAuditor"")]
public class DuplicatePolicyCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    // Pass 3 / Pass-2 P27 — invalid-character policy-name fixtures covering the well-formedness
    // regex non-whitespace branches. Each must trigger HFC1056.
    internal const string PolicyWithSpaceCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""with space"")]
public class PolicyWithSpaceCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal const string PolicyWithSlashCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""with/slash"")]
public class PolicyWithSlashCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal const string PolicyWithStarCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""with*char"")]
public class PolicyWithStarCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal const string PolicyWithTabCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""\t"")]
public class PolicyWithTabCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    // Pass 3 — punctuation-only policy names like "-" / ":" / "::" pass the per-character regex but
    // produce no meaningful policy lookup. The Pass-3 alphanumeric requirement rejects them.
    internal const string PolicyPunctuationOnlyCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
[RequiresPolicy(""::"")]
public class PolicyPunctuationOnlyCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal const string StructCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public struct StructCommand
{
    public string MessageId { get; set; }
}";

    internal const string SystemNamespaceCommand = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace System.FakeNamespace;

[Command]
public class SystemNamespaceCommand
{
    public string MessageId { get; set; } = string.Empty;
}";

    internal static string TooManyPropertiesCommand(int count) {
        var sb = new System.Text.StringBuilder();
        _ = sb.AppendLine("using Hexalith.FrontComposer.Contracts.Attributes;");
        _ = sb.AppendLine("namespace TestDomain;");
        _ = sb.AppendLine("[Command]");
        _ = sb.AppendLine("public class TooManyPropertiesCommand {");
        _ = sb.AppendLine("    public string MessageId { get; set; } = string.Empty;");
        for (int i = 0; i < count; i++) {
            _ = sb.AppendLine("    public string Field" + i + " { get; set; } = string.Empty;");
        }
        _ = sb.AppendLine("}");
        return sb.ToString();
    }
}
