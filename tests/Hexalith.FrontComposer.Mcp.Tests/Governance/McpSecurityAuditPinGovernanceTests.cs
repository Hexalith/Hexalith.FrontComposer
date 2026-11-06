using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Shouldly;

using Xunit.v3;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// E14R-AI-4 / F-6 (g): binds the focused tests that the SM-4 MCP leak and oracle audit
/// (<see cref="McpSecurityGovernanceTests"/>) relies on to the audit, so deleting, renaming, skipping, or moving one
/// out of the blocking lanes fails the Governance lane and names the test.
/// </summary>
[Trait("Category", "Governance")]
public sealed partial class McpSecurityAuditPinGovernanceTests {
    private const string McpTestsSourceRoot = "tests/Hexalith.FrontComposer.Mcp.Tests";
    private const string AuthRedactionStressSourcePath = "tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/AuthRedactionStressTests.cs";
    private const string AuthRedactionStressNamespace = "Hexalith.FrontComposer.Shell.Tests.Services.Auth";
    private const string AuthRedactionStressClassName = "AuthRedactionStressTests";

    /// <summary>
    /// The categories that the blocking test lanes in <c>.github/workflows/quality.yml</c> exclude. A pinned test
    /// that carries one of them no longer gates a change, which is the same outcome as skipping it.
    /// </summary>
    private static readonly string[] NonBlockingCategories = ["Quarantined", "GovernanceBuild", "Performance", "NightlyProperty", "e2e-palette"];

    private static readonly string[] TestAttributeNames = [
        "Fact", "Theory", "FactAttribute", "TheoryAttribute", "Xunit.Fact", "Xunit.Theory", "Xunit.FactAttribute", "Xunit.TheoryAttribute",
    ];

    private static readonly string[] TraitAttributeNames = ["Trait", "TraitAttribute", "Xunit.Trait", "Xunit.TraitAttribute"];

    /// <summary>
    /// Names are strings on purpose: a rename must fail this test by name, not break the build through
    /// <see langword="nameof"/>.
    /// </summary>
    private static readonly (string TypeName, string MethodName)[] FocusedTestPins = [
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ToolAdmissionTests", "HiddenTool_AndAbsentTool_ProduceStructurallyIdenticalUnknownEnvelope"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ToolAdmissionTests", "PolicyHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ToolAdmissionTests", "TenantHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "AdmissionFailures_ReturnDeterministicTaxonomy_AndDoNotQuery"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "QueryAndRenderFailures_ReturnSanitizedTaxonomy"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "InvalidAgentContext_ReturnsContextTaxonomy_AndDoesNotQuery"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "ResourceVisibilityDenied_IsHiddenEquivalent_AndDoesNotQueryOrEnumerateSuggestions"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "EpochChangeBeforeQuery_ReturnsStaleWithoutQuery"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "EpochChangeAfterQuery_ReturnsStaleWithoutRenderingPartialOutput"),
        ("Hexalith.FrontComposer.Mcp.Tests.Invocation.ProjectionReaderTaxonomyTests", "VisibilityFlipAfterQuery_ReturnsHiddenEquivalentWithoutRenderingPartialOutput"),
        ("Hexalith.FrontComposer.Mcp.Tests.Schema.SchemaNegotiationPrecedenceMatrixTests", "Negotiate_PrecedenceMatrix_RowYieldsExpectedClassification"),
        ("Hexalith.FrontComposer.Mcp.Tests.Schema.SchemaNegotiationPrecedenceMatrixTests", "LeakageGuards_LowerPriorityFieldsDoNotBleedIntoResult"),
        ("Hexalith.FrontComposer.Mcp.Tests.Schema.SchemaNegotiationPrecedenceMatrixTests", "LeakageGuards_NoFingerprintHashEverAppearsInPublicFields"),
        ("Hexalith.FrontComposer.Mcp.Tests.Schema.SchemaNegotiationPrecedenceMatrixTests", "LeakageGuards_DocsCodeIsBoundedShortStableIdentifier"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "ProductionMapping_RejectsAllowAllTenantGateBeforePublishingEndpoint"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "ProductionMapping_RejectsAllowAllResourceGateBeforePublishingEndpoint"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "ProductionMapping_RequiresAuthenticationBeforeSdkDispatch"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "SdkDispatch_UnregisteredResourceHasDistinctNotFoundError"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "SdkLifecycleSubscribeAnswersForeignPrincipalsAsNeverIssuedHandle"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "AppendedHostPolicyCannotRemoveTheAuthenticatedUserRequirement"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "HostDefaultPolicyStillAppliesToFrameworkOnlyMapping"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "PermissiveHostDefaultPolicyStillRequiresAnAuthenticatedCaller"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "MixedSchemeHostAuthenticatesTheAgentKeyForMcp"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "ApiKeyOnlyHostAuthenticatesConfiguredKeysBeforeAuthorization"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "MappingWithoutAuthenticationOrAuthorizationServicesNamesTheMissingCall"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "DevelopmentEnvironmentInAnyCaseAdmitsAllowAllGates"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "ProductionMappingResolvesGatesInAnAsyncDisposedScope"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "SdkUnknownToolAnswerIsIdenticalWhetherTheNameIsHiddenOrNeverRegistered"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "SdkCallerWithoutUsableIdentityCannotTellARegisteredToolFromAnAbsentOne"),
        ("Hexalith.FrontComposer.Mcp.Tests.Governance.McpSecurityGovernanceTests", "MappedHostListsAndInvokesTheToolAsTheApiKeyIdentity"),
        ("Hexalith.FrontComposer.Mcp.Tests.ApiKeyAuthenticationSchemeTests", "BlankOrUnmatchedHeaderFailsWithoutEchoingTheKey"),
        ("Hexalith.FrontComposer.Mcp.Tests.ApiKeyAuthenticationSchemeTests", "AccessorRejectsKeyWhenOtherCredentialsCarryAConflictingIdentity"),
        ("Hexalith.FrontComposer.Mcp.Tests.AuthContextAccessorTests", "ApiKeyWithConflictingIdentityClaimsFailsClosed"),
    ];

    private static readonly string[] AuthRedactionStressPins = [
        "ColonClaim_FailureLogs_NeverContainClaimValue",
        "ConflictingAlias_FailureLogs_NeverContainClaimValue",
        "TokenAcquisitionException_DoesNotIncludeJwtSubstring_InMessage",
        "CircuitTokenFallback_DoesNotLogJwtShapedAccessToken",
    ];

    [Fact]
    public void FocusedAuditTestsInThisAssemblyStillRunUnskipped() {
        Dictionary<string, List<MethodDeclarationSyntax>> sources = IndexMcpTestsSources(LocateRepositoryRoot());
        string[] violations = [.. FocusedTestPins.SelectMany(pin => ReflectionPinViolations(pin.TypeName, pin.MethodName)
            .Concat(SourceBodyViolations(pin.TypeName + "." + pin.MethodName, sources)))];

        violations.ShouldBeEmpty("Every focused test the SM-4 MCP audit relies on must exist, be a discoverable Fact or Theory, and run unskipped in a blocking lane.");

        // Non-vacuity: every branch reports, naming the probed method.
        string thisType = typeof(McpSecurityAuditPinGovernanceTests).FullName!;
        ReflectionPinViolations(thisType, "NoSuchPinnedAuditTest").ShouldHaveSingleItem().ShouldContain(thisType + ".NoSuchPinnedAuditTest");
        string helper = thisType + "." + nameof(ReflectionPinViolations);
        ShouldReport(ReflectionPinViolations(thisType, nameof(ReflectionPinViolations)), helper, "is not public");
        ShouldReport(ReflectionPinViolations(thisType, nameof(ReflectionPinViolations)), helper, "is no longer a Fact or Theory");
        string fixture = typeof(PinProbeFixture).FullName!;
        ShouldReport(ReflectionPinViolations(fixture, nameof(PinProbeFixture.SkippedFixture)), fixture + "." + nameof(PinProbeFixture.SkippedFixture), "is skipped");
        ShouldReport(ReflectionPinViolations(fixture, nameof(PinProbeFixture.ExplicitFixture)), fixture + "." + nameof(PinProbeFixture.ExplicitFixture), "is explicit");
        ShouldReport(ReflectionPinViolations(fixture, nameof(PinProbeFixture.SkipExceptionsFixture)), fixture + "." + nameof(PinProbeFixture.SkipExceptionsFixture), "skips on exceptions");
        ShouldReport(ReflectionPinViolations(fixture, nameof(PinProbeFixture.SkippedRowFixture)), fixture + "." + nameof(PinProbeFixture.SkippedRowFixture), "has a skipped data row");
        ShouldReport(ReflectionPinViolations(fixture, nameof(PinProbeFixture.SkippedRowFixture)), fixture + "." + nameof(PinProbeFixture.SkippedRowFixture), "carries Category=quarantined");
        ShouldReport(ReflectionPinViolations(fixture, nameof(PinProbeFixture.ExplicitFixture)), fixture + "." + nameof(PinProbeFixture.ExplicitFixture), "is declared on a type xUnit does not discover");
        ShouldReport(SourceBodyViolations(fixture + "." + nameof(PinProbeFixture.InBodySkipFixture), sources), fixture + "." + nameof(PinProbeFixture.InBodySkipFixture), "calls Assert.Skip");
    }

    [Fact]
    public void AuthRedactionStressTestsInShellTestsStillRunUnskipped() {
        string path = Path.Combine(LocateRepositoryRoot(), AuthRedactionStressSourcePath);
        File.Exists(path).ShouldBeTrue($"{AuthRedactionStressSourcePath} is pinned by the SM-4 MCP audit but no longer exists.");
        string source = File.ReadAllText(path);

        SourcePinViolations(source).ShouldBeEmpty("Every AuthRedactionStressTests method the SM-4 MCP audit relies on must exist, be a Fact or Theory, and run unskipped.");

        // Non-vacuity: a skip, a rename, a non-blocking class trait, and an in-body skip are each reported by name.
        string skipped = AuthRedactionStressPins[0];
        string skippedInBody = AuthRedactionStressPins[1];
        string renamed = AuthRedactionStressPins[^1];
        ClassDeclarationSyntax testClass = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)
            .GetCompilationUnitRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(declaration => string.Equals(declaration.Identifier.ValueText, AuthRedactionStressClassName, StringComparison.Ordinal));
        AttributeListSyntax skippedAttributes = FindMethod(testClass, skipped).AttributeLists[0];
        BlockSyntax inBodyBlock = FindMethod(testClass, skippedInBody).Body.ShouldNotBeNull();
        (int Start, int Length, string Text)[] edits = [
            (testClass.SpanStart, 0, "[Trait(\"Category\", \"Performance\")]\n"),
            (skippedAttributes.SpanStart, skippedAttributes.Span.Length, "[Theory(Skip = \"pin probe\")]"),
            (inBodyBlock.OpenBraceToken.Span.End, 0, " Assert.Skip(\"probe\");"),
        ];
        string probe = source;
        foreach ((int start, int length, string text) in edits.OrderByDescending(edit => edit.Start)) {
            probe = probe.Remove(start, length).Insert(start, text);
        }

        probe = probe.Replace(renamed + "(", renamed + "Renamed(", StringComparison.Ordinal);
        string[] probeViolations = SourcePinViolations(probe);
        ShouldReport(probeViolations, AuthRedactionStressClassName + "." + skipped, "is skipped");
        ShouldReport(probeViolations, AuthRedactionStressClassName + "." + renamed, "is missing");
        ShouldReport(probeViolations, AuthRedactionStressClassName, "carries Category=Performance");
        ShouldReport(probeViolations, AuthRedactionStressClassName + "." + skippedInBody, "calls Assert.Skip");
    }

    private static void ShouldReport(IEnumerable<string> violations, string subject, string reason)
        => violations.ShouldContain(
            violation => violation.StartsWith(subject + " ", StringComparison.Ordinal) && violation.Contains(reason, StringComparison.Ordinal),
            $"Expected a violation for '{subject}' that says '{reason}'.");

    private static IEnumerable<string> ReflectionPinViolations(string typeName, string methodName) {
        string pin = typeName + "." + methodName;
        Type? type = typeof(McpSecurityAuditPinGovernanceTests).Assembly.GetType(typeName, throwOnError: false);
        if (type is null) {
            yield return $"{pin} is pinned by the SM-4 MCP audit, but its class no longer exists.";
            yield break;
        }

        MethodInfo[] methods = [.. type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))];
        if (methods.Length == 0) {
            yield return $"{pin} is pinned by the SM-4 MCP audit, but it no longer exists; restore it or re-pin its replacement.";
            yield break;
        }

        if (!type.IsVisible || (type.IsAbstract && !type.IsSealed)) {
            yield return $"{pin} is declared on a type xUnit does not discover; the class must be public and not abstract.";
        }

        foreach (MethodInfo method in methods) {
            if (!method.IsPublic) {
                yield return $"{pin} is not public; a pinned test must be a public test method.";
            }

            FactAttribute[] facts = [.. method.GetCustomAttributes<FactAttribute>(inherit: true)];
            if (facts.Length == 0) {
                yield return $"{pin} is no longer a Fact or Theory, so it no longer runs.";
                continue;
            }

            foreach (FactAttribute fact in facts) {
                if (!string.IsNullOrEmpty(fact.Skip)) {
                    yield return $"{pin} is skipped: {fact.Skip}";
                }

                if (fact.Explicit) {
                    yield return $"{pin} is explicit, so default runs skip it.";
                }

                if (fact.SkipExceptions is { Length: > 0 } skipExceptions) {
                    yield return $"{pin} skips on exceptions: {string.Join(", ", skipExceptions.Select(exception => exception.FullName))}.";
                }
            }

            foreach (IDataAttribute data in method.GetCustomAttributes(inherit: true).OfType<IDataAttribute>()) {
                if (!string.IsNullOrEmpty(data.Skip)) {
                    yield return $"{pin} has a skipped data row: {data.Skip}";
                }

                if (data.Explicit == true) {
                    yield return $"{pin} has an explicit data row, so default runs skip it.";
                }
            }

            foreach (string category in CategoryTraits(method).Where(category => NonBlockingCategories.Contains(category, StringComparer.OrdinalIgnoreCase))) {
                yield return $"{pin} carries Category={category}, which the blocking test lanes exclude.";
            }
        }
    }

    private static IEnumerable<string> CategoryTraits(MethodInfo method) {
        List<object> sources = [.. method.GetCustomAttributes(inherit: true)];
        for (Type? type = method.DeclaringType; type is not null; type = type.DeclaringType) {
            sources.AddRange(type.GetCustomAttributes(inherit: true));
        }

        sources.AddRange(method.Module.Assembly.GetCustomAttributes(inherit: true));
        return sources
            .OfType<ITraitAttribute>()
            .SelectMany(attribute => attribute.GetTraits())
            .Where(trait => string.Equals(trait.Key, "Category", StringComparison.OrdinalIgnoreCase))
            .Select(trait => trait.Value);
    }

    private static Dictionary<string, List<MethodDeclarationSyntax>> IndexMcpTestsSources(string repositoryRoot) {
        Dictionary<string, List<MethodDeclarationSyntax>> index = new(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(Path.Combine(repositoryRoot, McpTestsSourceRoot), "*.cs", SearchOption.AllDirectories)) {
            string relativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
            if (relativePath.Split('/').Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: relativePath);
            foreach (MethodDeclarationSyntax method in tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()) {
                string key = QualifiedMethodName(method);
                if (!index.TryGetValue(key, out List<MethodDeclarationSyntax>? declarations)) {
                    declarations = [];
                    index[key] = declarations;
                }

                declarations.Add(method);
            }
        }

        return index;
    }

    private static string QualifiedMethodName(MethodDeclarationSyntax method) {
        string namespaceName = string.Join('.', method.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(declaration => declaration.Name.ToString()));
        string typeName = string.Join('+', method.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(declaration => declaration.Identifier.ValueText));
        return (namespaceName.Length == 0 ? string.Empty : namespaceName + ".") + typeName + "." + method.Identifier.ValueText;
    }

    private static IEnumerable<string> SourceBodyViolations(string pin, Dictionary<string, List<MethodDeclarationSyntax>> sources) {
        if (!sources.TryGetValue(pin, out List<MethodDeclarationSyntax>? declarations)) {
            yield return $"{pin} has no source declaration under {McpTestsSourceRoot}, so its body cannot be checked for in-body skips.";
            yield break;
        }

        foreach (MethodDeclarationSyntax method in declarations) {
            foreach (string call in InBodySkipCalls(method)) {
                yield return $"{pin} calls {call} in its body.";
            }
        }
    }

    private static IEnumerable<string> InBodySkipCalls(MethodDeclarationSyntax method)
        => method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Where(access => access.Name.Identifier.ValueText is "Skip" or "SkipWhen" or "SkipUnless"
                && access.Expression.ToString() is "Assert" or "Xunit.Assert" or "global::Xunit.Assert")
            .Select(access => $"{access} at {access.SyntaxTree.GetLineSpan(access.Span).Path}:{access.SyntaxTree.GetLineSpan(access.Span).StartLinePosition.Line + 1}");

    private static IEnumerable<string> NonBlockingTraitCategories(SyntaxList<AttributeListSyntax> attributeLists) {
        foreach (AttributeSyntax attribute in attributeLists.SelectMany(list => list.Attributes)) {
            if (!TraitAttributeNames.Contains(attribute.Name.ToString(), StringComparer.Ordinal)
                || attribute.ArgumentList is not { Arguments.Count: 2 } arguments
                || !string.Equals(LiteralText(arguments.Arguments[0].Expression), "Category", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            string category = LiteralText(arguments.Arguments[1].Expression);
            if (NonBlockingCategories.Contains(category, StringComparer.OrdinalIgnoreCase)) {
                yield return category;
            }
        }
    }

    private static string LiteralText(ExpressionSyntax expression)
        => expression is LiteralExpressionSyntax literal ? literal.Token.ValueText : expression.ToString();

    private static MethodDeclarationSyntax FindMethod(ClassDeclarationSyntax type, string name)
        => type.Members
            .OfType<MethodDeclarationSyntax>()
            .First(declaration => string.Equals(declaration.Identifier.ValueText, name, StringComparison.Ordinal));

    private static string[] SourcePinViolations(string source) {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: AuthRedactionStressSourcePath);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        ClassDeclarationSyntax? testClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(declaration => string.Equals(declaration.Identifier.ValueText, AuthRedactionStressClassName, StringComparison.Ordinal));
        string? namespaceName = testClass?.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
        if (testClass is null || !string.Equals(namespaceName, AuthRedactionStressNamespace, StringComparison.Ordinal)) {
            return [$"{AuthRedactionStressSourcePath} no longer declares {AuthRedactionStressNamespace}.{AuthRedactionStressClassName}."];
        }

        List<string> violations = [];
        foreach (string category in NonBlockingTraitCategories(testClass.AttributeLists)) {
            violations.Add($"{AuthRedactionStressClassName} carries Category={category}, which the blocking test lanes exclude.");
        }

        foreach (string name in AuthRedactionStressPins) {
            string subject = AuthRedactionStressClassName + "." + name;
            MethodDeclarationSyntax? method = testClass.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(declaration => string.Equals(declaration.Identifier.ValueText, name, StringComparison.Ordinal));
            if (method is null) {
                violations.Add($"{subject} is missing from {AuthRedactionStressSourcePath}; restore it or re-pin its replacement.");
                continue;
            }

            bool isTest = method.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(attribute => TestAttributeNames.Contains(attribute.Name.ToString(), StringComparer.Ordinal));
            if (!isTest) {
                violations.Add($"{subject} is no longer a Fact or Theory, so it no longer runs.");
            }

            foreach (string category in NonBlockingTraitCategories(method.AttributeLists)) {
                violations.Add($"{subject} carries Category={category}, which the blocking test lanes exclude.");
            }

            foreach (string call in InBodySkipCalls(method)) {
                violations.Add($"{subject} calls {call} in its body.");
            }
        }

        foreach ((Regex pattern, string reason) in new[] {
            (SkipAssignment(), "is skipped ('Skip =')"),
            (ExplicitAssignment(), "is explicit ('Explicit =')"),
        }) {
            foreach (Match match in pattern.Matches(source)) {
                int line = tree.GetLineSpan(new TextSpan(match.Index, match.Length)).StartLinePosition.Line + 1;
                string member = root.FindToken(match.Index).Parent?.AncestorsAndSelf()
                    .Select(node => node switch {
                        MethodDeclarationSyntax method => method.Identifier.ValueText,
                        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                        _ => null,
                    })
                    .FirstOrDefault(identifier => identifier is not null) ?? "<file>";
                string subject = string.Equals(member, AuthRedactionStressClassName, StringComparison.Ordinal)
                    ? AuthRedactionStressClassName
                    : AuthRedactionStressClassName + "." + member;
                violations.Add($"{subject} {reason} at {AuthRedactionStressSourcePath}:{line}.");
            }
        }

        return [.. violations];
    }

    [GeneratedRegex(@"\bSkip\s*=(?!=)", RegexOptions.CultureInvariant)]
    private static partial Regex SkipAssignment();

    [GeneratedRegex(@"\bExplicit\s*=(?!=)", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitAssignment();

    private static string LocateRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FrontComposer repository root.");
    }

#pragma warning disable xUnit1000 // Never discovered by design: the pin probes reflect over this fixture instead of running it.
    /// <summary>
    /// Probe fixture for the reflection and in-body pins. It is not public, so xUnit never discovers or runs it; each
    /// member trips one pin branch, and the lowercase class trait checks case-insensitive category matching.
    /// </summary>
    [Trait("Category", "quarantined")]
    private sealed class PinProbeFixture {
        [Fact(Skip = "pin probe")]
        public void SkippedFixture() {
        }

        [Fact(Explicit = true)]
        public void ExplicitFixture() {
        }

        [Fact(SkipExceptions = [typeof(NotSupportedException)])]
        public void SkipExceptionsFixture() {
        }

        [Theory]
        [InlineData(1, Skip = "pin probe")]
        public void SkippedRowFixture(int value) => _ = value;

        [Fact]
        public void InBodySkipFixture() => Assert.Skip("pin probe");
    }
#pragma warning restore xUnit1000
}
