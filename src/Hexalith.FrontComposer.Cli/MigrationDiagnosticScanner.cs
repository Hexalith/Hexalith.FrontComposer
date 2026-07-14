using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationDiagnosticScanner {
    public static async Task<ImmutableArray<Diagnostic>> ScanAsync(Document document, CancellationToken cancellationToken) {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) {
            return [];
        }

        SyntaxTree tree = root.SyntaxTree;
        ImmutableArray<Diagnostic>.Builder builder = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()) {
            if (IsInsideNameOf(identifier)) {
                continue;
            }

            string name = identifier.Identifier.ValueText;
            if (name == FrontComposerMigrationCodeFixProvider.ObsoleteApi) {
                builder.Add(Diagnostic.Create(MigrationDiagnostics.ObsoleteDevOverlay, Location.Create(tree, identifier.Span)));
            }
        }

        return builder.ToImmutable();
    }

    private static bool IsInsideNameOf(IdentifierNameSyntax identifier) {
        for (SyntaxNode? parent = identifier.Parent; parent is not null; parent = parent.Parent) {
            if (parent is InvocationExpressionSyntax invocation
                && invocation.Expression is IdentifierNameSyntax target
                && target.Identifier.ValueText == "nameof") {
                return true;
            }
        }

        return false;
    }
}
