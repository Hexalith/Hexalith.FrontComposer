namespace Hexalith.FrontComposer.Mcp;

public sealed class FrontComposerMcpException : Exception {
    public FrontComposerMcpException(FrontComposerMcpFailureCategory category)
        : base(category.ToString()) => Category = category;

    public FrontComposerMcpException(FrontComposerMcpFailureCategory category, string message)
        : base(message) => Category = category;

    public FrontComposerMcpException(FrontComposerMcpFailureCategory category, string message, Exception? innerException)
        : base(message, innerException) => Category = category;

    public FrontComposerMcpFailureCategory Category { get; }
}
