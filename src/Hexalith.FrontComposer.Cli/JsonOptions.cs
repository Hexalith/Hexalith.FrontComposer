using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class JsonOptions {
    public static readonly JsonSerializerOptions Stable = new() {
        WriteIndented = true,
    };
}
