using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Communication;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Communication;

public sealed class QueryRequestTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CanonicalConstructor_Defaults_AreStable()
    {
        QueryRequest request = QueryRequest.Create(new ProjectionQuery("OrdersProjection"), "acme");

        request.Criteria.ShouldBe(new ProjectionQuery("OrdersProjection"));
        request.TenantId.ShouldBe("acme");
        request.ETag.ShouldBeNull();
        request.Domain.ShouldBeNull();
        request.AggregateId.ShouldBeNull();
        request.QueryType.ShouldBeNull();
        request.EntityId.ShouldBeNull();
        request.ProjectionActorType.ShouldBeNull();
        request.ETags.ShouldBeNull();
        request.CacheDiscriminator.ShouldBeNull();
        request.CachePayloadVersion.ShouldBe(1);
    }

    [Fact]
    public void CanonicalAndLegacyConstruction_SameValues_AreEqual()
    {
        Dictionary<string, string> columnFilters = new(StringComparer.Ordinal) { ["Name"] = "acme" };
        string[] statusFilters = ["Pending", "Approved"];
        string[] etags = ["\"etag-1\""];
        QueryRequest canonical = QueryRequest.Create(
            new ProjectionQuery("OrdersProjection", 2, 3, columnFilters, statusFilters, "foo", "Name", true),
            "acme",
            "\"etag\"",
            "orders",
            "order-1",
            "GetOrders",
            "line-1",
            "OrdersProjectionActor",
            etags,
            "orders-grid",
            4);

#pragma warning disable HFC0001 // Intentional v1.12 compatibility construction.
        QueryRequest legacy = new(
            ProjectionType: "OrdersProjection",
            TenantId: "acme",
            Skip: 2,
            Take: 3,
            ETag: "\"etag\"",
            ColumnFilters: columnFilters,
            StatusFilters: statusFilters,
            SearchQuery: "foo",
            SortColumn: "Name",
            SortDescending: true,
            Domain: "orders",
            AggregateId: "order-1",
            QueryType: "GetOrders",
            EntityId: "line-1",
            ProjectionActorType: "OrdersProjectionActor",
            ETags: etags,
            CacheDiscriminator: "orders-grid",
            CachePayloadVersion: 4);
#pragma warning restore HFC0001

        legacy.ShouldBe(canonical);
        legacy.GetHashCode().ShouldBe(canonical.GetHashCode());
        JsonSerializer.Serialize(legacy, WebJson).ShouldBe(JsonSerializer.Serialize(canonical, WebJson));
    }

    [Fact]
    public void CanonicalAndLegacyWith_CriteriaChanges_StaySynchronized()
    {
        QueryRequest original = QueryRequest.Create(new ProjectionQuery("OrdersProjection", Take: 10), "acme");
        Dictionary<string, string> columnFilters = new(StringComparer.Ordinal) { ["Status"] = "Open" };
        string[] statusFilters = ["Open", "Pending"];
        ProjectionQuery changedCriteria = new(
            "ArchivedOrdersProjection",
            Skip: 3,
            Take: 20,
            ColumnFilters: columnFilters,
            StatusFilters: statusFilters,
            SearchQuery: "needle",
            SortColumn: "CreatedAt",
            SortDescending: true);
        QueryRequest canonicalCopy = original with { Criteria = changedCriteria };
#pragma warning disable HFC0001 // Intentional flattened with-expression compatibility proof.
        QueryRequest legacyCopy = original with
        {
            ProjectionType = "ArchivedOrdersProjection",
            Skip = 3,
            Take = 20,
            ColumnFilters = columnFilters,
            StatusFilters = statusFilters,
            SearchQuery = "needle",
            SortColumn = "CreatedAt",
            SortDescending = true,
            Filter = "legacy-only",
        };
        legacyCopy.Take.ShouldBe(20);
        legacyCopy.SearchQuery.ShouldBe("needle");
        legacyCopy.Filter.ShouldBe("legacy-only");
        legacyCopy.Criteria.ShouldBe(canonicalCopy.Criteria);
        (legacyCopy with { Filter = null }).ShouldBe(canonicalCopy);
#pragma warning restore HFC0001
        original.Criteria.ShouldBe(new ProjectionQuery("OrdersProjection", Take: 10));
    }

    [Fact]
    public void LegacyDeconstruct_ReturnsAllNineteenV112Values()
    {
#pragma warning disable HFC0001 // Intentional v1.12 deconstruction compatibility proof.
        QueryRequest request = new("Orders", "acme", "legacy", 1, 2, "etag", null, null, "find", "Name", true, "orders", "42", "List", "line", "actor", ["e1"], "grid", 3);
        (
            string projectionType,
            string? tenantId,
            string? filter,
            int? skip,
            int? take,
            string? etag,
            IReadOnlyDictionary<string, string>? columnFilters,
            IReadOnlyList<string>? statusFilters,
            string? searchQuery,
            string? sortColumn,
            bool sortDescending,
            string? domain,
            string? aggregateId,
            string? queryType,
            string? entityId,
            string? projectionActorType,
            IReadOnlyList<string>? etags,
            string? cacheDiscriminator,
            int cachePayloadVersion) = request;
        QueryRequest jsonRoundTrip = JsonSerializer.Deserialize<QueryRequest>(JsonSerializer.Serialize(request, WebJson), WebJson).ShouldNotBeNull();
        jsonRoundTrip.Filter.ShouldBe("legacy");
#pragma warning restore HFC0001

        projectionType.ShouldBe("Orders");
        tenantId.ShouldBe("acme");
        filter.ShouldBe("legacy");
        skip.ShouldBe(1);
        take.ShouldBe(2);
        etag.ShouldBe("etag");
        columnFilters.ShouldBeNull();
        statusFilters.ShouldBeNull();
        searchQuery.ShouldBe("find");
        sortColumn.ShouldBe("Name");
        sortDescending.ShouldBeTrue();
        domain.ShouldBe("orders");
        aggregateId.ShouldBe("42");
        queryType.ShouldBe("List");
        entityId.ShouldBe("line");
        projectionActorType.ShouldBe("actor");
        etags.ShouldBe(["e1"]);
        cacheDiscriminator.ShouldBe("grid");
        cachePayloadVersion.ShouldBe(3);
    }

    [Fact]
    public void DirectJson_CanonicalRequest_PreservesFlatV112ShapeAndRoundTrips()
    {
        QueryRequest request = QueryRequest.Create(
            new ProjectionQuery(
                "OrdersProjection",
                2,
                3,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["Name"] = "acme" },
                ["Pending"],
                "needle",
                "Name",
                true),
            "acme",
            "\"etag\"",
            "orders",
            "order-1",
            "GetOrders",
            "line-1",
            "OrdersProjectionActor",
            ["\"etag-1\""],
            "orders-grid",
            2);

        string json = JsonSerializer.Serialize(request, WebJson);

        json.ShouldBe("{\"projectionType\":\"OrdersProjection\",\"tenantId\":\"acme\",\"filter\":null,\"skip\":2,\"take\":3,\"eTag\":\"\\u0022etag\\u0022\",\"columnFilters\":{\"Name\":\"acme\"},\"statusFilters\":[\"Pending\"],\"searchQuery\":\"needle\",\"sortColumn\":\"Name\",\"sortDescending\":true,\"domain\":\"orders\",\"aggregateId\":\"order-1\",\"queryType\":\"GetOrders\",\"entityId\":\"line-1\",\"projectionActorType\":\"OrdersProjectionActor\",\"eTags\":[\"\\u0022etag-1\\u0022\"],\"cacheDiscriminator\":\"orders-grid\",\"cachePayloadVersion\":2}");
        json.ShouldNotContain("\"criteria\"");

        QueryRequest roundTrip = JsonSerializer.Deserialize<QueryRequest>(json, WebJson).ShouldNotBeNull();
        JsonSerializer.Serialize(roundTrip, WebJson).ShouldBe(json);
        roundTrip.Criteria.ProjectionType.ShouldBe(request.Criteria.ProjectionType);
        roundTrip.Criteria.ColumnFilters.ShouldBe(request.Criteria.ColumnFilters);
        roundTrip.Criteria.StatusFilters.ShouldBe(request.Criteria.StatusFilters);
        roundTrip.ETags.ShouldBe(request.ETags);
    }

    [Fact]
    public void DirectJson_NullCollections_PreservesFlatDefaultsAndRoundTrips()
    {
        QueryRequest request = QueryRequest.Create(new ProjectionQuery("OrdersProjection"), null);

        string json = JsonSerializer.Serialize(request, WebJson);

        json.ShouldBe("{\"projectionType\":\"OrdersProjection\",\"tenantId\":null,\"filter\":null,\"skip\":null,\"take\":null,\"eTag\":null,\"columnFilters\":null,\"statusFilters\":null,\"searchQuery\":null,\"sortColumn\":null,\"sortDescending\":false,\"domain\":null,\"aggregateId\":null,\"queryType\":null,\"entityId\":null,\"projectionActorType\":null,\"eTags\":null,\"cacheDiscriminator\":null,\"cachePayloadVersion\":1}");
        JsonSerializer.Deserialize<QueryRequest>(json, WebJson).ShouldBe(request);
    }

    [Fact]
    public void SourceGeneratedJson_CanonicalRequest_RemainsWarningFreeAndFlat()
    {
        QueryRequest request = QueryRequest.Create(new ProjectionQuery("OrdersProjection", Take: 5), "acme");

        string json = JsonSerializer.Serialize(request, QueryRequestJsonContext.Default.QueryRequest);
        QueryRequest roundTrip = JsonSerializer.Deserialize(json, QueryRequestJsonContext.Default.QueryRequest).ShouldNotBeNull();

        json.ShouldNotContain("\"criteria\"");
        roundTrip.Criteria.ShouldBe(new ProjectionQuery("OrdersProjection", Take: 5));
        roundTrip.TenantId.ShouldBe("acme");
    }

    [Fact]
    public void WebJson_QuotedNumbers_AreAcceptedByReflectionAndSourceGeneratedPaths()
    {
        const string json = """{"projectionType":"Orders","tenantId":"acme","skip":"2","take":"25","cachePayloadVersion":"3"}""";

        QueryRequest reflection = JsonSerializer.Deserialize<QueryRequest>(json, WebJson).ShouldNotBeNull();
        QueryRequest generated = JsonSerializer.Deserialize(json, QueryRequestJsonContext.Default.QueryRequest).ShouldNotBeNull();

        reflection.Criteria.Skip.ShouldBe(2);
        reflection.Criteria.Take.ShouldBe(25);
        reflection.CachePayloadVersion.ShouldBe(3);
        generated.Criteria.ShouldBe(reflection.Criteria);
        generated.CachePayloadVersion.ShouldBe(3);
    }

    [Theory]
    [InlineData("sortDescending")]
    [InlineData("cachePayloadVersion")]
    public void DirectJson_ExplicitNullForNonNullableValue_ThrowsJsonException(string propertyName)
    {
        string json = $"{{\"projectionType\":\"Orders\",\"{propertyName}\":null}}";

        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<QueryRequest>(json, WebJson));
    }

    [Theory]
    [InlineData(JsonIgnoreCondition.WhenWritingNull, true)]
    [InlineData(JsonIgnoreCondition.WhenWritingDefault, false)]
    public void DirectJson_DefaultIgnoreCondition_MatchesPropertySerializerBehavior(
        JsonIgnoreCondition condition,
        bool includesFalseBoolean)
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = condition };
        QueryRequest request = QueryRequest.Create(new ProjectionQuery("Orders"), null);

        string json = JsonSerializer.Serialize(request, options);

        json.ShouldNotContain("\"tenantId\"");
        json.ShouldNotContain("\"skip\"");
        json.Contains("\"sortDescending\":false", StringComparison.Ordinal).ShouldBe(includesFalseBoolean);
        json.ShouldContain("\"cachePayloadVersion\":1");
    }

    [Fact]
    public void DirectJson_UnmappedMemberDisallow_RemainsFailClosed()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        _ = Should.Throw<JsonException>(
            () => JsonSerializer.Deserialize<QueryRequest>("""{"projectionType":"Orders","unexpected":true}""", options));
    }

    [Fact]
    public void V112SignatureFixture_AllLegacyMembersRemain()
    {
        string[] expectedNames =
        [
            "ProjectionType", "TenantId", "Filter", "Skip", "Take", "ETag", "ColumnFilters", "StatusFilters",
            "SearchQuery", "SortColumn", "SortDescending", "Domain", "AggregateId", "QueryType", "EntityId",
            "ProjectionActorType", "ETags", "CacheDiscriminator", "CachePayloadVersion",
        ];
        Type[] expectedTypes =
        [
            typeof(string), typeof(string), typeof(string), typeof(int?), typeof(int?), typeof(string),
            typeof(IReadOnlyDictionary<string, string>), typeof(IReadOnlyList<string>), typeof(string), typeof(string),
            typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string),
            typeof(IReadOnlyList<string>), typeof(string), typeof(int),
        ];
        ConstructorInfo legacyConstructor = typeof(QueryRequest).GetConstructors()
            .Single(constructor => constructor.GetParameters().Length == expectedNames.Length);
        ParameterInfo[] parameters = legacyConstructor.GetParameters();

        parameters.Select(parameter => parameter.Name).ShouldBe(expectedNames);
        parameters.Select(parameter => parameter.ParameterType).ShouldBe(expectedTypes);
        parameters.Take(2).All(parameter => !parameter.HasDefaultValue).ShouldBeTrue();
        parameters.Skip(2).Take(16).All(parameter => parameter.HasDefaultValue).ShouldBeTrue();
        parameters[^1].DefaultValue.ShouldBe(1);
        expectedNames.Select(name => typeof(QueryRequest).GetProperty(name)!.PropertyType).ShouldBe(expectedTypes);
        typeof(QueryRequest).GetMethod(nameof(QueryRequest.Deconstruct))!.GetParameters()
            .Select(parameter => parameter.Name)
            .ShouldBe(expectedNames);
    }

    [Fact]
    public void LegacyEntryPoints_AllCarryHfc0001Metadata()
    {
        string[] flattenedCriteriaProperties =
        [
            "ProjectionType", "Filter", "Skip", "Take", "ColumnFilters", "StatusFilters", "SearchQuery",
            "SortColumn", "SortDescending",
        ];
        List<MemberInfo> legacyMembers = flattenedCriteriaProperties
            .Select(name => (MemberInfo)typeof(QueryRequest).GetProperty(name)!)
            .ToList();
        legacyMembers.Add(typeof(QueryRequest).GetConstructors().Single(constructor => constructor.GetParameters().Length == 19));
        legacyMembers.Add(typeof(QueryRequest).GetMethod(nameof(QueryRequest.Deconstruct))!);

        foreach (MemberInfo member in legacyMembers)
        {
            ObsoleteAttribute obsolete = member.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
            obsolete.DiagnosticId.ShouldBe("HFC0001");
            obsolete.UrlFormat.ShouldBe("https://hexalith.github.io/FrontComposer/diagnostics/{0}");
            obsolete.Message.ShouldNotBeNull().ShouldContain("v3.0.0");
        }
    }

    [Fact]
    public void LegacyNullProjectionTypeConstruction_RemainsUnambiguous()
    {
#pragma warning disable HFC0001, CS8625 // Intentional v1.12 nullable-disabled consumer shape.
        QueryRequest request = new(null, "acme");
#pragma warning restore HFC0001, CS8625

#pragma warning disable HFC0001 // Intentional compatibility read.
        request.ProjectionType.ShouldBeNull();
#pragma warning restore HFC0001
    }

    [Fact]
    public void RecordStringRepresentation_RemainsFlattened()
    {
        QueryRequest request = QueryRequest.Create(new ProjectionQuery("Orders", Take: 5), "acme");

        string text = request.ToString();

        text.ShouldContain("ProjectionType = Orders");
        text.ShouldContain("Take = 5");
        text.ShouldNotContain("Criteria =");
    }
}
