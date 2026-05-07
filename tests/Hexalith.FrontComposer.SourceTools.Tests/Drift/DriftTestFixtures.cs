using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift;

/// <summary>
/// Shared test infrastructure for drift-related tests. Story 9-1 review CM-2: chunk-C tests
/// previously reached into <c>DriftClassifierProjectionPropertyTests</c> via <c>using static</c>;
/// helpers live here instead so renaming or scoping that class does not cascade across files.
/// </summary>
internal static class DriftTestFixtures {
    internal sealed class InMemoryAdditionalText(string path, string text) : AdditionalText {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
            => SourceText.From(text, Encoding.UTF8);
    }
}
