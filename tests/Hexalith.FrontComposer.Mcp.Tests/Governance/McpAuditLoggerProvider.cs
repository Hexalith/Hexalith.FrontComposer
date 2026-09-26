using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

internal sealed class McpAuditLoggerProvider : ILoggerProvider {
    internal ConcurrentQueue<string> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new McpAuditLogger(categoryName, Entries);

    public void Dispose() {
    }
}
