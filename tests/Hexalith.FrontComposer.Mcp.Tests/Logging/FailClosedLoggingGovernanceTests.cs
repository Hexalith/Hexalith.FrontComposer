using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Logging;

[Trait("Category", "Governance")]
public sealed class FailClosedLoggingGovernanceTests
{
    private static readonly HashSet<string> DirectLogMethodNames =
    [
        "Log",
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical",
    ];

    private static readonly string[] SecuritySourcePaths =
    [
        "src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs",
        "src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs",
    ];

    private static readonly HashSet<string> AllowedUnwrappedParameterNames = new(StringComparer.Ordinal)
    {
        "exceptionType",
        "diagnosticId",
    };

    [Fact]
    public void McpSources_HaveExactGeneratedFailClosedInventoryAndNoDirectCalls()
    {
        SourceFile[] sources = LoadSources("src/Hexalith.FrontComposer.Mcp");

        sources.ShouldNotBeEmpty("the MCP logging governance scan must cover production sources");
        foreach (string path in SecuritySourcePaths)
        {
            SourceFile source = sources.Where(candidate => candidate.Path == path).ShouldHaveSingleItem();
            source.Content.ShouldNotBeNullOrWhiteSpace($"{path} must be a non-empty source location");
        }

        DirectLogSite[] directCalls = [.. sources.SelectMany(FindDirectLogSites)];
        directCalls.ShouldBeEmpty(
            "MCP has no remaining direct ILogger.Log* inventory; add generated helpers before introducing a call. "
            + FormatSites(directCalls));

        string[] unwrappedArguments = [.. sources.SelectMany(FindUnwrappedIdentifierArguments)];
        unwrappedArguments.ShouldBeEmpty(
            "generated security log calls must not pass raw string parameters directly; wrap with a sanitizing helper. "
            + string.Join(", ", unwrappedArguments));

        LoggerEvent[] events = [.. sources.SelectMany(FindLoggerEvents)];
        AssertUniqueEventIds(events);
        events.Select(static entry => entry.EventId).Order().ShouldBe([
            8300,
            8310,
            8311,
            8312,
            8313,
            8314,
            8315,
            8316,
            8317,
            8318,
        ]);

        LoggerEvent[] failClosedEvents = [.. events.Where(static entry => entry.Path.EndsWith("/FrontComposerMcpLog.cs", StringComparison.Ordinal))];
        failClosedEvents.Select(static entry => entry.EventId).Order().ShouldBe(Enumerable.Range(8310, 9));
        foreach (LoggerEvent entry in failClosedEvents.Where(static entry => entry.EventId >= 8315))
        {
            entry.EventName.ShouldNotBeNullOrWhiteSpace($"{entry.Location} must declare an explicit EventName");
            entry.HasExceptionParameter.ShouldBeFalse($"{entry.Location} must not capture an Exception parameter");
        }
    }

    [Fact]
    public void GovernanceGuard_SyntheticDirectCallDuplicateIdAndExceptionParameter_AreReported()
    {
        SourceFile[] sources =
        [
            new(
                SecuritySourcePaths[0],
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class Gate { "
                + "void Run(ILogger logger) => logger.LogWarning(\"unsafe\"); }"),
            new(
                "src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs",
                "using System; using Microsoft.Extensions.Logging; namespace Synthetic; internal static partial class FrontComposerMcpLog { "
                + "[LoggerMessage(EventId = 8315, EventName = \"First\", Level = LogLevel.Warning, Message = \"first\")] "
                + "static partial void First(ILogger logger, Exception exception); "
                + "[LoggerMessage(EventId = 8315, EventName = \"Second\", Level = LogLevel.Warning, Message = \"second\")] "
                + "static partial void Second(ILogger logger); }"),
        ];

        DirectLogSite directCall = sources.SelectMany(FindDirectLogSites).ShouldHaveSingleItem();
        directCall.Path.ShouldBe(SecuritySourcePaths[0]);

        LoggerEvent[] events = [.. sources.SelectMany(FindLoggerEvents)];
        events.GroupBy(static entry => entry.EventId).ShouldContain(group => group.Count() == 2);
        events.ShouldContain(static entry => entry.HasExceptionParameter);
    }

    [Fact]
    public void GovernanceGuard_SyntheticUnwrappedIdentifierArgument_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs",
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal static partial class FrontComposerMcpLog { "
                + "public static void Unsafe(ILogger logger, string tenantId) { LogUnsafe(logger, tenantId); } "
                + "[LoggerMessage(EventId = 9999, EventName = \"Unsafe\", Level = LogLevel.Warning, Message = \"{TenantId}\")] "
                + "private static partial void LogUnsafe(ILogger logger, string tenantId); }"),
        ];

        string[] unwrappedArguments = [.. sources.SelectMany(FindUnwrappedIdentifierArguments)];
        unwrappedArguments.ShouldContain("src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs:Unsafe:tenantId");
    }

    [Fact]
    public void GovernanceGuard_EmptySourceCensusAndOverBroadSecurityAllowlist_AreReported()
    {
        Should.Throw<ShouldAssertException>(() =>
            Array.Empty<SourceFile>().ShouldNotBeEmpty("the MCP logging governance scan must cover production sources"));

        SourceFile[] overBroadSources =
        [
            new(
                SecuritySourcePaths[0],
                "using Microsoft.Extensions.Logging; namespace Synthetic; internal sealed class Gate { "
                + "void Run(ILogger logger) => logger.LogWarning(\"still raw\"); }"),
        ];
        DirectLogSite[] overBroadCalls = [.. overBroadSources.SelectMany(FindDirectLogSites)];
        Should.Throw<ShouldAssertException>(() =>
            overBroadCalls.ShouldBeEmpty(
                "MCP has no remaining direct ILogger.Log* inventory; add generated helpers before introducing a call."));
    }

    private static IEnumerable<string> FindUnwrappedIdentifierArguments(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        SyntaxNode root = tree.GetRoot();
        HashSet<string> generatedMethodNames = [.. root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(IsLoggerMessageAttribute)
            .Select(static attribute => attribute.FirstAncestorOrSelf<MethodDeclarationSyntax>())
            .OfType<MethodDeclarationSyntax>()
            .Select(static method => method.Identifier.ValueText)];

        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not IdentifierNameSyntax invokedName || invocation.ArgumentList is null)
            {
                continue;
            }

            MethodDeclarationSyntax? enclosingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (enclosingMethod is null)
            {
                continue;
            }

            bool isGeneratedCall = generatedMethodNames.Contains(invokedName.Identifier.ValueText);
            bool isDelegateCall = !isGeneratedCall && enclosingMethod.ParameterList.Parameters.Any(parameter =>
                string.Equals(parameter.Identifier.ValueText, invokedName.Identifier.ValueText, StringComparison.Ordinal)
                && parameter.Type?.ToString().Contains("Action", StringComparison.Ordinal) == true);
            if (!isGeneratedCall && !isDelegateCall)
            {
                continue;
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is not IdentifierNameSyntax identifier
                    || AllowedUnwrappedParameterNames.Contains(identifier.Identifier.ValueText))
                {
                    continue;
                }

                string name = identifier.Identifier.ValueText;
                ParameterSyntax? parameter = enclosingMethod.ParameterList.Parameters
                    .FirstOrDefault(candidate => string.Equals(candidate.Identifier.ValueText, name, StringComparison.Ordinal));
                if (parameter?.Type?.ToString().TrimEnd('?') == "string")
                {
                    yield return $"{source.Path}:{enclosingMethod.Identifier.ValueText}:{name}";
                }
            }
        }
    }

    private static IEnumerable<DirectLogSite> FindDirectLogSites(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            string? methodName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
                _ => null,
            };
            if (methodName is null || !DirectLogMethodNames.Contains(methodName))
            {
                continue;
            }

            int line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            yield return new(source.Path, line, methodName);
        }
    }

    private static IEnumerable<LoggerEvent> FindLoggerEvents(SourceFile source)
    {
        SyntaxTree tree = Parse(source);
        foreach (AttributeSyntax attribute in tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Where(IsLoggerMessageAttribute))
        {
            MethodDeclarationSyntax? method = attribute.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            int? eventId = ReadEventId(attribute);
            if (method is null || eventId is null)
            {
                continue;
            }

            string? eventName = attribute.ArgumentList?.Arguments
                .FirstOrDefault(static argument => argument.NameEquals?.Name.Identifier.ValueText == "EventName")
                ?.Expression is LiteralExpressionSyntax literal
                ? literal.Token.ValueText
                : null;
            bool hasExceptionParameter = method.ParameterList.Parameters.Any(static parameter =>
                IsExceptionParameterType(parameter.Type));
            int line = tree.GetLineSpan(attribute.Span).StartLinePosition.Line + 1;
            yield return new(source.Path, line, eventId.Value, eventName, hasExceptionParameter);
        }
    }

    private static bool IsExceptionParameterType(TypeSyntax? type)
    {
        if (type is null)
        {
            return false;
        }

        string text = type.ToString().TrimEnd('?');
        if (text.StartsWith("global::", StringComparison.Ordinal))
        {
            text = text["global::".Length..];
        }

        return text.EndsWith("Exception", StringComparison.Ordinal);
    }

    private static void AssertUniqueEventIds(IEnumerable<LoggerEvent> events)
    {
        string[] duplicates = [.. events
            .GroupBy(static entry => entry.EventId)
            .Where(static group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(static entry => entry.Location))}")];
        duplicates.ShouldBeEmpty("LoggerMessage EventIds must be unique. " + string.Join("; ", duplicates));
    }

    private static int? ReadEventId(AttributeSyntax attribute)
    {
        AttributeArgumentSyntax? argument = attribute.ArgumentList?.Arguments
            .FirstOrDefault(static candidate => candidate.NameEquals?.Name.Identifier.ValueText == "EventId")
            ?? attribute.ArgumentList?.Arguments.FirstOrDefault(static candidate => candidate.NameEquals is null);
        return argument is not null && int.TryParse(argument.Expression.ToString(), out int eventId)
            ? eventId
            : null;
    }

    private static bool IsLoggerMessageAttribute(AttributeSyntax attribute)
        => attribute.Name.ToString() is "LoggerMessage" or "LoggerMessageAttribute";

    private static SyntaxTree Parse(SourceFile source)
        => CSharpSyntaxTree.ParseText(
            source.Content,
            new CSharpParseOptions(LanguageVersion.Latest),
            source.Path);

    private static SourceFile[] LoadSources(string relativeRoot)
    {
        string repositoryRoot = LocateRepositoryRoot();
        string sourceRoot = Path.Combine(repositoryRoot, relativeRoot);
        return [.. Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsBuildPath(path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(path => new SourceFile(
                Normalize(Path.GetRelativePath(repositoryRoot, path)),
                File.ReadAllText(path)))];
    }

    private static bool IsBuildPath(string path)
        => Normalize(path).Split('/').Any(static segment => segment is "bin" or "obj");

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FrontComposer repository root.");
    }

    private static string FormatSites(IEnumerable<DirectLogSite> sites)
        => string.Join(", ", sites.Select(static site => site.Location));

    private static string Normalize(string path) => path.Replace('\\', '/');

    private sealed record SourceFile(string Path, string Content);

    private sealed record DirectLogSite(string Path, int Line, string MethodName)
    {
        public string Location => $"{Path}:{Line}:{MethodName}";
    }

    private sealed record LoggerEvent(
        string Path,
        int Line,
        int EventId,
        string? EventName,
        bool HasExceptionParameter)
    {
        public string Location => $"{Path}:{Line}";
    }
}
