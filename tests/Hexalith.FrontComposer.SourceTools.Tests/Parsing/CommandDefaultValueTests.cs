using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public class CommandDefaultValueTests {
    [Fact]
    public void HFC1012_Allows_DefaultValue_TypeString_Overload_ForMatchingType() {
        string source = """
            using System;
            using System.ComponentModel;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Command]
            public class TimestampedCommand {
                public string MessageId { get; set; } = string.Empty;

                [DefaultValue(typeof(DateTime), "2026-04-16T12:00:00Z")]
                public DateTime CreatedAt { get; set; }
            }
            """;

        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.TimestampedCommand");

        result.Diagnostics.Any(d => d.Id == "HFC1012").ShouldBeFalse();
    }

    [Fact]
    public void HFC1012_Rejects_DefaultValue_TypeString_Overload_ForMismatchedType() {
        string source = """
            using System;
            using System.ComponentModel;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Command]
            public class BadTimestampCommand {
                public string MessageId { get; set; } = string.Empty;

                [DefaultValue(typeof(Guid), "00000000-0000-0000-0000-000000000000")]
                public DateTime CreatedAt { get; set; }
            }
            """;

        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.BadTimestampCommand");

        result.Diagnostics.Any(d => d.Id == "HFC1012" && d.Severity == "Error").ShouldBeTrue();
    }
}
