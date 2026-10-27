namespace Hexalith.FrontComposer.Contracts.Downloads;

/// <summary>Downloads caller-owned bytes without logging, storing, or changing their contents.</summary>
public interface IFcBrowserDownloadService
{
    /// <summary>Downloads a readable stream from its current position; the caller retains ownership.</summary>
    /// <param name="stream">Caller-owned content stream.</param>
    /// <param name="fileName">Safe leaf filename; paths and control characters are rejected.</param>
    /// <param name="contentType">Caller-owned media type.</param>
    /// <param name="cancellationToken">Cancels transfer and prevents a pending browser click.</param>
    Task DownloadStreamAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Downloads unchanged file bytes with a safe caller-owned leaf filename.</summary>
    /// <param name="content">The file bytes.</param>
    /// <param name="fileName">Safe leaf filename.</param>
    /// <param name="contentType">The media type.</param>
    /// <param name="cancellationToken">Operation cancellation.</param>
    Task DownloadFileAsync(ReadOnlyMemory<byte> content, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Downloads unchanged UTF-8 JSON bytes without parsing or reserializing them.</summary>
    /// <param name="utf8Json">The exact JSON bytes.</param>
    /// <param name="fileName">Safe leaf filename.</param>
    /// <param name="cancellationToken">Operation cancellation.</param>
    Task DownloadJsonAsync(ReadOnlyMemory<byte> utf8Json, string fileName, CancellationToken cancellationToken = default);
}
