using FsCheck;
using FsCheck.Xunit;

using Hexalith.FrontComposer.SourceTools.Emitters;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Verifies the canonical generated C# string-literal body formatter and its role-body seam.
/// </summary>
public sealed class GeneratedLiteralTests {
    public static TheoryData<string> EscapeEdgeCases => new() {
        string.Empty,
        "quoted \"value\"",
        "back\\slash",
        "controls:\0\a\b\f\n\r\t\v",
        "next-line:\u0085",
        "line-separator:\u2028",
        "paragraph-separator:\u2029",
        "combined:\"\\\0\a\b\f\n\r\t\v\u0085\u2028\u2029",
    };

    [Theory]
    [MemberData(nameof(EscapeEdgeCases))]
    public void Escape_EdgeCaseLiteralBody_ParsesAndDecodesToOriginal(string value) {
        string escaped = GeneratedLiteral.Escape(value);

        LiteralExpressionSyntax literal = ParseLiteral(escaped);

        literal.Token.ValueText.ShouldBe(value);
    }

    [Theory]
    [MemberData(nameof(EscapeEdgeCases))]
    public void RoleBodyHelpers_EscapeString_DelegatesCanonicalEdgeCases(string value) {
        string escaped = RoleBodyHelpers.EscapeString(value);

        escaped.ShouldBe(GeneratedLiteral.Escape(value));
        ParseLiteral(escaped).Token.ValueText.ShouldBe(value);
    }

    [Property(MaxTest = 500)]
    public bool Escape_ArbitraryString_RoundTripsThroughRoslynLiteral(NonNull<string> input) {
        ArgumentNullException.ThrowIfNull(input);
        LiteralExpressionSyntax? literal = SyntaxFactory.ParseExpression(
            "\"" + GeneratedLiteral.Escape(input.Get) + "\"") as LiteralExpressionSyntax;

        return literal is not null
            && !literal.ContainsDiagnostics
            && string.Equals(literal.Token.ValueText, input.Get, StringComparison.Ordinal);
    }

    private static LiteralExpressionSyntax ParseLiteral(string escaped) {
        ExpressionSyntax expression = SyntaxFactory.ParseExpression("\"" + escaped + "\"");
        expression.ContainsDiagnostics.ShouldBeFalse(expression.GetDiagnostics().FirstOrDefault()?.ToString());
        return expression.ShouldBeOfType<LiteralExpressionSyntax>();
    }
}
