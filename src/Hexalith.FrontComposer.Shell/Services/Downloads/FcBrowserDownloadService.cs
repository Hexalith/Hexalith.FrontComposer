using Hexalith.FrontComposer.Contracts.Downloads;

using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Services.Downloads;

/// <summary>Transfers ephemeral streams to the browser and owns interop cleanup.</summary>
/// <param name="jsRuntime">The current circuit's JavaScript runtime.</param>
public sealed class FcBrowserDownloadService(IJSRuntime jsRuntime) : IFcBrowserDownloadService
{
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-download.js";

    /// <inheritdoc />
    public async Task DownloadStreamAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The download stream must be readable.", nameof(stream));
        }
        if (fileName is "." or ".." || fileName.Any(character => char.IsControl(character) || "/\\:*?\"<>|".Contains(character, StringComparison.Ordinal)))
        {
            throw new ArgumentException("A download filename must be a safe leaf name.", nameof(fileName));
        }

        cancellationToken.ThrowIfCancellationRequested();
        IJSObjectReference module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).ConfigureAwait(false);
        string operationId = Guid.NewGuid().ToString("N");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await module.InvokeVoidAsync("beginDownload", cancellationToken, operationId).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            using var reference = new DotNetStreamReference(stream, leaveOpen: true);
            await module.InvokeVoidAsync("downloadStream", cancellationToken, operationId, fileName, contentType, reference).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            // Also clean a beginDownload that completed as its caller cancelled.
            await CleanupAsync(module, operationId).ConfigureAwait(false);
            try
            {
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // The circuit has already released its browser resources.
            }
        }
    }

    /// <inheritdoc />
    public async Task DownloadFileAsync(ReadOnlyMemory<byte> content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content.ToArray(), writable: false);
        await DownloadStreamAsync(stream, fileName, contentType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DownloadJsonAsync(ReadOnlyMemory<byte> utf8Json, string fileName, CancellationToken cancellationToken = default)
        => DownloadFileAsync(utf8Json, fileName, "application/json", cancellationToken);

    private static async Task CleanupAsync(IJSObjectReference module, string operationId)
    {
        try
        {
            await module.InvokeVoidAsync("cancelDownload", operationId).ConfigureAwait(false);
        }
        catch (JSException)
        {
            // Operation-owned DOM resources are cleaned by the module's own finally block.
        }
    }
}
