using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.Cli;

internal sealed class FrontComposerMigrationCodeFixProvider : CodeFixProvider {
    public const string ObsoleteApi = "AddFrontComposerDebugOverlay";
    public const string ReplacementApi = "AddFrontComposerDevMode";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MigrationDiagnostics.ObsoleteDevOverlay.Id);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        foreach (Diagnostic diagnostic in context.Diagnostics.Where(x => x.Id == MigrationDiagnostics.ObsoleteDevOverlay.Id)) {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Replace obsolete FrontComposer dev-mode API",
                    cancellationToken => ReplaceAsync(context.Document, diagnostic, cancellationToken),
                    equivalenceKey: MigrationDiagnostics.ObsoleteDevOverlay.Id),
                diagnostic);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<Document> ReplaceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken) {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) {
            return document;
        }

        SyntaxToken token = root.FindToken(diagnostic.Location.SourceSpan.Start);
        if (token.ValueText != ObsoleteApi || token.Parent is not IdentifierNameSyntax) {
            return document;
        }

        SyntaxToken replacement = SyntaxFactory.Identifier(token.LeadingTrivia, ReplacementApi, token.TrailingTrivia);
        return document.WithSyntaxRoot(root.ReplaceToken(token, replacement));
    }
}
