using System.Text;

namespace Hexalith.FrontComposer.Cli;

internal static class SourceFile {
    public const long MaxSupportedBytes = 16 * 1024 * 1024;

    public static async Task<SourceFileContent> ReadAsync(string path, CancellationToken cancellationToken) {
        FileInfo file = new(path);
        if (file.Exists && file.Length > MaxSupportedBytes) {
            throw new IOException("Source file exceeds the maximum supported migration size of 16 MiB.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        Encoding encoding = DetectEncoding(bytes);
        string text = encoding.GetString(PreambleFree(bytes, encoding));
        return new SourceFileContent(text, encoding);
    }

    public static async Task WriteAsync(string path, string text, Encoding encoding, CancellationToken cancellationToken) {
        string directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        string tempPath = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try {
            await File.WriteAllTextAsync(tempPath, text, encoding, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
        }
        finally {
            try {
                if (File.Exists(tempPath)) {
                    File.Delete(tempPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            }
        }
    }

    private static Encoding DetectEncoding(byte[] bytes) {
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) {
            return new UTF32Encoding(bigEndian: true, byteOrderMark: true);
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) {
            return new UTF32Encoding(bigEndian: false, byteOrderMark: true);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) {
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) {
            return Encoding.BigEndianUnicode;
        }

        // Strict UTF-8: fail closed on invalid bytes rather than silently replacing with U+FFFD.
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    }

    private static byte[] PreambleFree(byte[] bytes, Encoding encoding) {
        byte[] preamble = encoding.GetPreamble();
        return preamble.Length > 0 && bytes.AsSpan().StartsWith(preamble)
            ? bytes[preamble.Length..]
            : bytes;
    }
}
