using System.Collections.Immutable;
using System.Text;

using FsCheck.Xunit;

using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

/// <summary>
/// Story 2-2 Task 1.4 tests — density classification on CommandModel + parse-time diagnostics
/// (HFC1011/HFC1012/HFC1014). Decisions D3, D17.
/// </summary>
public class CommandDensityTests {
    // 1. FsCheck property — density boundaries for any non-negative integer count.
    // FsCheck.Xunit.v3 [Property] auto-generates arbitraries for parameters; negative values are
    // filtered by returning true vacuously (domain precondition).
    [Property]
    public bool Density_ClassificationProperty_MatchesSpec(int count) {
        if (count < 0) {
            return true;
        }

        CommandDensity actual = CommandModel.ComputeDensity(count);
        CommandDensity expected = count switch {
            <= 1 => CommandDensity.Inline,
            <= 4 => CommandDensity.CompactInline,
            _ => CommandDensity.FullPage,
        };
        return actual == expected;
    }

    // 2. Boundary snapshot — 0/1/2/4/5 fields produce Inline/Inline/CompactInline/CompactInline/FullPage.
    [Fact]
    public void Density_BoundarySnapshot_AtZeroOneTwoFourFive() {
        (int fieldCount, CommandDensity expected)[] boundaries = [
            (0, CommandDensity.Inline),
            (1, CommandDensity.Inline),
            (2, CommandDensity.CompactInline),
            (4, CommandDensity.CompactInline),
            (5, CommandDensity.FullPage),
        ];

        foreach ((int fieldCount, CommandDensity expected) in boundaries) {
            string source = BuildCommandSource("B" + fieldCount, fieldCount);
            CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.B" + fieldCount);
            _ = result.Model.ShouldNotBeNull();
            result.Model.NonDerivableProperties.Count.ShouldBe(fieldCount, $"non-derivable count for N={fieldCount}");
            result.Model.Density.ShouldBe(expected, $"density for N={fieldCount}");
        }
    }

    // 3. Equality: two CommandModels differing only by Density / IconName must be non-equal.
    [Fact]
    public void CommandModel_Equality_IncludesDensityAndIconName() {
        var props = new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty);
        var zero = new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty);

        // Simulate two shapes — one with 0 non-derivables (Inline), one with 5 (FullPage).
        PropertyModel p(string n) => new(n, "string", false, false, null, new EquatableArray<BadgeMappingEntry>(ImmutableArray<BadgeMappingEntry>.Empty));
        var fiveProps = new EquatableArray<PropertyModel>(ImmutableArray.Create(p("A"), p("B"), p("C"), p("D"), p("E")));

        var inline = new CommandModel("X", "N", null, null, null, fiveProps, zero, zero);
        var full = new CommandModel("X", "N", null, null, null, fiveProps, zero, fiveProps);
        inline.Density.ShouldBe(CommandDensity.Inline);
        full.Density.ShouldBe(CommandDensity.FullPage);
        inline.Equals(full).ShouldBeFalse();

        var withIcon = new CommandModel("X", "N", null, null, null, props, zero, zero, iconName: "Regular.Size16.Play");
        var withoutIcon = new CommandModel("X", "N", null, null, null, props, zero, zero);
        withIcon.Equals(withoutIcon).ShouldBeFalse();
    }

    // 4. Hash-code participation.
    [Fact]
    public void CommandModel_HashCode_IncludesDensityAndIconName() {
        var zero = new EquatableArray<PropertyModel>(ImmutableArray<PropertyModel>.Empty);

        var a = new CommandModel("X", "N", null, null, null, zero, zero, zero);
        var b = new CommandModel("X", "N", null, null, null, zero, zero, zero, iconName: "Regular.Size16.Play");

        a.GetHashCode().ShouldNotBe(b.GetHashCode());

        // Same inputs produce same hash.
        var a2 = new CommandModel("X", "N", null, null, null, zero, zero, zero);
        a.GetHashCode().ShouldBe(a2.GetHashCode());
    }

    // 5. HFC1011 — 201-property command is rejected with an Error diagnostic.
    [Fact]
    public void HFC1011_RejectsGreaterThan200Properties() {
        string source = BuildCommandSource("Huge", 201);
        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.Huge");
        result.Diagnostics.Any(d => d.Id == "HFC1011" && d.Severity == "Error").ShouldBeTrue();
    }

    // 6. HFC1012 — [DefaultValue("hello")] int Amount is rejected.
    [Fact]
    public void HFC1012_RejectsDefaultValueTypeMismatch() {
        string source = """
            using System.ComponentModel;
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Command]
            public class BadDefaultCommand {
                public string MessageId { get; set; } = string.Empty;
                [DefaultValue("hello")]
                public int Amount { get; set; }
            }
            """;
        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.BadDefaultCommand");
        result.Diagnostics.Any(d => d.Id == "HFC1012" && d.Severity == "Error").ShouldBeTrue();
    }

    // 8. HFC1017 — generic [Command] is rejected with Error (Story 2-3 Hindsight H9).
    [Fact]
    public void HFC1017_RejectsGenericCommand() {
        string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Command]
            public class GenericCommand<T> {
                public string MessageId { get; set; } = string.Empty;
            }
            """;
        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.GenericCommand`1");
        result.Model.ShouldBeNull();
        result.Diagnostics.Any(d => d.Id == "HFC1017" && d.Severity == "Error").ShouldBeTrue();
    }

    // 7. HFC1014 — nested [Command] is rejected with Error.
    [Fact]
    public void HFC1014_RejectsNestedCommand() {
        string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            public class Outer {
                [Command]
                public class InnerCommand {
                    public string MessageId { get; set; } = string.Empty;
                }
            }
            """;
        CommandParseResult result = CompilationHelper.ParseCommand(source, "TestDomain.Outer+InnerCommand");
        result.Model.ShouldBeNull();
        result.Diagnostics.Any(d => d.Id == "HFC1014" && d.Severity == "Error").ShouldBeTrue();
    }

    private static string BuildCommandSource(string typeName, int nonDerivableCount) {
        StringBuilder sb = new();
        sb.AppendLine("using Hexalith.FrontComposer.Contracts.Attributes;");
        sb.AppendLine("namespace TestDomain;");
        sb.AppendLine("[Command]");
        sb.Append("public class ").AppendLine(typeName).AppendLine(" {");
        sb.AppendLine("    public string MessageId { get; set; } = string.Empty;");
        for (int i = 0; i < nonDerivableCount; i++) {
            sb.Append("    public string Field").Append(i).AppendLine(" { get; set; } = string.Empty;");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
