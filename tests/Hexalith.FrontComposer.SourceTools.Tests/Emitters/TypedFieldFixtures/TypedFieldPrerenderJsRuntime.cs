using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Models the supported absence of JavaScript during server prerendering.</summary>
internal sealed class TypedFieldPrerenderJsRuntime : IJSRuntime
{
    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object? []? args) => ValueTask.FromException<TValue>(new InvalidOperationException("JavaScript is unavailable during prerender."));
    /// <inheritdoc/>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object? []? args) => ValueTask.FromException<TValue>(new InvalidOperationException("JavaScript is unavailable during prerender."));
}
