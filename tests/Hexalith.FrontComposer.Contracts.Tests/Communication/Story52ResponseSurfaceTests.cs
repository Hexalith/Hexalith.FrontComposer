using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Communication;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Communication;

/// <summary>
/// Story 5-2 T7 / AC8 — append-only contract compatibility for the new response surface.
/// Each fact pins a property the framework promises remain valid for downstream consumers
/// (generated forms, projection page loaders, badge readers) so 5.2 → 5.3 evolution can
/// not silently regress the typed exception taxonomy.
/// </summary>
public class Story52ResponseSurfaceTests {
    [Fact]
    public void QueryResult_NotModified_HasEmptyItems_ZeroCount_AndIsNotModifiedFlag() {
        QueryResult<string> result = QueryResult<string>.NotModified("\"v1\"");

        result.IsNotModified.ShouldBeTrue();
        result.ETag.ShouldBe("\"v1\"");
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void QueryResult_NotModifiedFromCache_PreservesItems_AndKeepsIsNotModifiedFlag() {
        QueryResult<string> result = QueryResult<string>.NotModifiedFromCache(["a", "b"], totalCount: 42, etag: "\"v2\"");

        result.IsNotModified.ShouldBeTrue();
        result.ETag.ShouldBe("\"v2\"");
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(42);
    }

    [Fact]
    public void CommandValidationException_CarriesProblemDetails_AndDefaultMessage() {
        ProblemDetailsPayload problem = new(
            Title: "Invalid input",
            Detail: null,
            Status: 400,
            EntityLabel: null,
            ValidationErrors: new Dictionary<string, IReadOnlyList<string>> {
                ["Quantity"] = new[] { "must be > 0" },
            },
            GlobalErrors: System.Array.Empty<string>());

        CommandValidationException ex = new(problem);

        ex.Message.ShouldBe("Invalid input");
        ex.Problem.ShouldBeSameAs(problem);
        ex.Problem.ValidationErrors["Quantity"][0].ShouldBe("must be > 0");
    }

    [Fact]
    public void CommandWarningException_CarriesKindRetryAfterAndProblem() {
        CommandWarningException ex = new(
            CommandWarningKind.RateLimited,
            ProblemDetailsPayload.Empty,
            retryAfter: System.TimeSpan.FromSeconds(30));

        ex.Kind.ShouldBe(CommandWarningKind.RateLimited);
        ex.RetryAfter.ShouldBe(System.TimeSpan.FromSeconds(30));
        ex.Problem.ShouldBeSameAs(ProblemDetailsPayload.Empty);
    }

    [Fact]
    public void AuthRedirectRequiredException_DefaultsToFrameworkReason() {
        AuthRedirectRequiredException ex = new();

        ex.Message.ShouldContain("401");
    }

    [Fact]
    public void QueryFailureException_PreservesKindAndProblem() {
        QueryFailureException ex = new(
            QueryFailureKind.Forbidden,
            new ProblemDetailsPayload("Forbidden", "scope: orders.write", 403, null,
                new Dictionary<string, IReadOnlyList<string>>(),
                System.Array.Empty<string>()));

        ex.Kind.ShouldBe(QueryFailureKind.Forbidden);
        ex.Problem.Title.ShouldBe("Forbidden");
        ex.Problem.Detail.ShouldBe("scope: orders.write");
    }

    [Fact]
    public void CommandRejectedException_RemainsCompatibleWithGeneratedForms() {
        // AC2 / D7 — 409 Conflict still flows through the existing rejection contract so Story
        // 2-5's domain-rejection UX is preserved.
        CommandRejectedException ex = new("Order locked", "Wait for the previous edit to commit.");

        ex.Message.ShouldBe("Order locked");
        ex.Resolution.ShouldBe("Wait for the previous edit to commit.");
    }

    [Fact]
    public void ProblemDetailsPayload_Empty_HasNoErrors() {
        ProblemDetailsPayload.Empty.GlobalErrors.ShouldBeEmpty();
        ProblemDetailsPayload.Empty.ValidationErrors.Count.ShouldBe(0);
        ProblemDetailsPayload.Empty.Title.ShouldBeNull();
    }
}
