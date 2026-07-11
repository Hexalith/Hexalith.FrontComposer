using System.Text;
using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Composes canonical projection criteria with tenant, EventStore routing, validator, and cache metadata.
/// </summary>
[JsonConverter(typeof(QueryRequestJsonConverter))]
public record QueryRequest
{
    private const string LegacyMessage = "Flattened QueryRequest projection criteria are deprecated. Use QueryRequest.Criteria with ProjectionQuery. Removed in v2.0.0. See HFC0001.";
    private const string DiagnosticId = "HFC0001";
    private const string HelpLinkFormat = "https://hexalith.github.io/FrontComposer/diagnostics/{0}";

    private ProjectionQuery _criteria;
    private string? _filter;

    internal string? LegacyFilter => _filter;

    /// <summary>
    /// Creates a query request from canonical projection criteria and transport/cache metadata.
    /// </summary>
    /// <param name="Criteria">The canonical projection criteria.</param>
    /// <param name="TenantId">Optional requested tenant context.</param>
    /// <param name="ETag">Optional ETag for cache validation.</param>
    /// <param name="Domain">Optional EventStore domain name.</param>
    /// <param name="AggregateId">Optional EventStore aggregate identifier.</param>
    /// <param name="QueryType">Optional EventStore query type discriminator.</param>
    /// <param name="EntityId">Optional EventStore entity identifier for nested projection queries.</param>
    /// <param name="ProjectionActorType">Optional EventStore projection actor type.</param>
    /// <param name="ETags">Optional explicit ETag validator set.</param>
    /// <param name="CacheDiscriminator">Optional framework-allowlisted ETag cache discriminator.</param>
    /// <param name="CachePayloadVersion">Projection payload contract version used by the cache.</param>
    /// <returns>The composed query request.</returns>
    public static QueryRequest Create(
        ProjectionQuery Criteria,
        string? TenantId,
        string? ETag = null,
        string? Domain = null,
        string? AggregateId = null,
        string? QueryType = null,
        string? EntityId = null,
        string? ProjectionActorType = null,
        System.Collections.Generic.IReadOnlyList<string>? ETags = null,
        string? CacheDiscriminator = null,
        int CachePayloadVersion = 1)
        => new(
            Criteria,
            TenantId,
            ETag,
            Domain,
            AggregateId,
            QueryType,
            EntityId,
            ProjectionActorType,
            ETags,
            CacheDiscriminator,
            CachePayloadVersion);

    private QueryRequest(
        ProjectionQuery Criteria,
        string? TenantId,
        string? ETag,
        string? Domain,
        string? AggregateId,
        string? QueryType,
        string? EntityId,
        string? ProjectionActorType,
        System.Collections.Generic.IReadOnlyList<string>? ETags,
        string? CacheDiscriminator,
        int CachePayloadVersion)
    {
        _criteria = Criteria ?? throw new ArgumentNullException(nameof(Criteria));
        this.TenantId = TenantId;
        this.ETag = ETag;
        this.Domain = Domain;
        this.AggregateId = AggregateId;
        this.QueryType = QueryType;
        this.EntityId = EntityId;
        this.ProjectionActorType = ProjectionActorType;
        this.ETags = ETags;
        this.CacheDiscriminator = CacheDiscriminator;
        this.CachePayloadVersion = CachePayloadVersion;
    }

    /// <summary>
    /// Initializes a query request through the flattened v1.12 compatibility surface.
    /// </summary>
    /// <param name="ProjectionType">The projection type name to query.</param>
    /// <param name="TenantId">Optional requested tenant context.</param>
    /// <param name="Filter">Legacy filter expression.</param>
    /// <param name="Skip">The number of items to skip for pagination.</param>
    /// <param name="Take">The number of items to take for pagination.</param>
    /// <param name="ETag">Optional ETag for cache validation.</param>
    /// <param name="ColumnFilters">Per-column filter values keyed by declared property name.</param>
    /// <param name="StatusFilters">Enum-member names selected through status filters.</param>
    /// <param name="SearchQuery">The global search query.</param>
    /// <param name="SortColumn">The declared property name used for ordering.</param>
    /// <param name="SortDescending">Whether the ordering is descending.</param>
    /// <param name="Domain">Optional EventStore domain name.</param>
    /// <param name="AggregateId">Optional EventStore aggregate identifier.</param>
    /// <param name="QueryType">Optional EventStore query type discriminator.</param>
    /// <param name="EntityId">Optional EventStore entity identifier for nested projection queries.</param>
    /// <param name="ProjectionActorType">Optional EventStore projection actor type.</param>
    /// <param name="ETags">Optional explicit ETag validator set.</param>
    /// <param name="CacheDiscriminator">Optional framework-allowlisted ETag cache discriminator.</param>
    /// <param name="CachePayloadVersion">Projection payload contract version used by the cache.</param>
    [JsonConstructor]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public QueryRequest(
        string ProjectionType,
        string? TenantId,
        string? Filter = null,
        int? Skip = null,
        int? Take = null,
        string? ETag = null,
        System.Collections.Generic.IReadOnlyDictionary<string, string>? ColumnFilters = null,
        System.Collections.Generic.IReadOnlyList<string>? StatusFilters = null,
        string? SearchQuery = null,
        string? SortColumn = null,
        bool SortDescending = false,
        string? Domain = null,
        string? AggregateId = null,
        string? QueryType = null,
        string? EntityId = null,
        string? ProjectionActorType = null,
        System.Collections.Generic.IReadOnlyList<string>? ETags = null,
        string? CacheDiscriminator = null,
        int CachePayloadVersion = 1)
        : this(
            new ProjectionQuery(
                ProjectionType,
                Skip,
                Take,
                ColumnFilters,
                StatusFilters,
                SearchQuery,
                SortColumn,
                SortDescending),
            TenantId,
            ETag,
            Domain,
            AggregateId,
            QueryType,
            EntityId,
            ProjectionActorType,
            ETags,
            CacheDiscriminator,
            CachePayloadVersion)
    {
        _filter = Filter;
    }

    /// <summary>Gets or initializes the canonical projection criteria.</summary>
    [JsonIgnore]
    public ProjectionQuery Criteria
    {
        get => _criteria;
        init
        {
            _criteria = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>Gets or initializes the projection type through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(0)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public string ProjectionType
    {
        get => _criteria.ProjectionType;
        init => _criteria = _criteria with { ProjectionType = value };
    }

    /// <summary>Gets or initializes the requested tenant context.</summary>
    [JsonPropertyOrder(1)]
    public string? TenantId { get; init; }

    /// <summary>Gets or initializes the legacy filter expression.</summary>
    [JsonPropertyOrder(2)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public string? Filter
    {
        get => _filter;
        init => _filter = value;
    }

    /// <summary>Gets or initializes the pagination offset through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(3)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public int? Skip
    {
        get => _criteria.Skip;
        init => _criteria = _criteria with { Skip = value };
    }

    /// <summary>Gets or initializes the page size through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(4)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public int? Take
    {
        get => _criteria.Take;
        init => _criteria = _criteria with { Take = value };
    }

    /// <summary>Gets or initializes the optional ETag validator.</summary>
    [JsonPropertyOrder(5)]
    public string? ETag { get; init; }

    /// <summary>Gets or initializes column filters through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(6)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public System.Collections.Generic.IReadOnlyDictionary<string, string>? ColumnFilters
    {
        get => _criteria.ColumnFilters;
        init => _criteria = _criteria with { ColumnFilters = value };
    }

    /// <summary>Gets or initializes status filters through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(7)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public System.Collections.Generic.IReadOnlyList<string>? StatusFilters
    {
        get => _criteria.StatusFilters;
        init => _criteria = _criteria with { StatusFilters = value };
    }

    /// <summary>Gets or initializes the search query through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(8)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public string? SearchQuery
    {
        get => _criteria.SearchQuery;
        init => _criteria = _criteria with { SearchQuery = value };
    }

    /// <summary>Gets or initializes the sort column through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(9)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public string? SortColumn
    {
        get => _criteria.SortColumn;
        init => _criteria = _criteria with { SortColumn = value };
    }

    /// <summary>Gets or initializes the ordering direction through the flattened compatibility surface.</summary>
    [JsonPropertyOrder(10)]
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public bool SortDescending
    {
        get => _criteria.SortDescending;
        init => _criteria = _criteria with { SortDescending = value };
    }

    /// <summary>Gets or initializes the EventStore domain name.</summary>
    [JsonPropertyOrder(11)]
    public string? Domain { get; init; }

    /// <summary>Gets or initializes the EventStore aggregate identifier.</summary>
    [JsonPropertyOrder(12)]
    public string? AggregateId { get; init; }

    /// <summary>Gets or initializes the EventStore query type discriminator.</summary>
    [JsonPropertyOrder(13)]
    public string? QueryType { get; init; }

    /// <summary>Gets or initializes the EventStore entity identifier.</summary>
    [JsonPropertyOrder(14)]
    public string? EntityId { get; init; }

    /// <summary>Gets or initializes the EventStore projection actor type.</summary>
    [JsonPropertyOrder(15)]
    public string? ProjectionActorType { get; init; }

    /// <summary>Gets or initializes the explicit ETag validator set.</summary>
    [JsonPropertyOrder(16)]
    public System.Collections.Generic.IReadOnlyList<string>? ETags { get; init; }

    /// <summary>Gets or initializes the framework-allowlisted cache discriminator.</summary>
    [JsonPropertyOrder(17)]
    public string? CacheDiscriminator { get; init; }

    /// <summary>Gets or initializes the projection payload contract version used by the cache.</summary>
    [JsonPropertyOrder(18)]
    public int CachePayloadVersion { get; init; }

    /// <summary>
    /// Deconstructs a query request through the flattened v1.12 compatibility surface.
    /// </summary>
#if NET10_0_OR_GREATER
    [Obsolete(LegacyMessage, error: false, DiagnosticId = DiagnosticId, UrlFormat = HelpLinkFormat)]
#else
    [Obsolete(LegacyMessage, error: false)]
#endif
    public void Deconstruct(
        out string ProjectionType,
        out string? TenantId,
        out string? Filter,
        out int? Skip,
        out int? Take,
        out string? ETag,
        out System.Collections.Generic.IReadOnlyDictionary<string, string>? ColumnFilters,
        out System.Collections.Generic.IReadOnlyList<string>? StatusFilters,
        out string? SearchQuery,
        out string? SortColumn,
        out bool SortDescending,
        out string? Domain,
        out string? AggregateId,
        out string? QueryType,
        out string? EntityId,
        out string? ProjectionActorType,
        out System.Collections.Generic.IReadOnlyList<string>? ETags,
        out string? CacheDiscriminator,
        out int CachePayloadVersion)
    {
        ProjectionType = _criteria.ProjectionType;
        TenantId = this.TenantId;
        Filter = _filter;
        Skip = _criteria.Skip;
        Take = _criteria.Take;
        ETag = this.ETag;
        ColumnFilters = _criteria.ColumnFilters;
        StatusFilters = _criteria.StatusFilters;
        SearchQuery = _criteria.SearchQuery;
        SortColumn = _criteria.SortColumn;
        SortDescending = _criteria.SortDescending;
        Domain = this.Domain;
        AggregateId = this.AggregateId;
        QueryType = this.QueryType;
        EntityId = this.EntityId;
        ProjectionActorType = this.ProjectionActorType;
        ETags = this.ETags;
        CacheDiscriminator = this.CacheDiscriminator;
        CachePayloadVersion = this.CachePayloadVersion;
    }

    /// <summary>Appends the v1.12 flattened record members.</summary>
    /// <param name="builder">The target string builder.</param>
    /// <returns><see langword="true"/> because the record has printable members.</returns>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        builder.Append("ProjectionType = ");
        builder.Append(_criteria.ProjectionType);
        builder.Append(", TenantId = ");
        builder.Append(TenantId);
        builder.Append(", Filter = ");
        builder.Append(_filter);
        builder.Append(", Skip = ");
        builder.Append(_criteria.Skip);
        builder.Append(", Take = ");
        builder.Append(_criteria.Take);
        builder.Append(", ETag = ");
        builder.Append(ETag);
        builder.Append(", ColumnFilters = ");
        builder.Append(_criteria.ColumnFilters);
        builder.Append(", StatusFilters = ");
        builder.Append(_criteria.StatusFilters);
        builder.Append(", SearchQuery = ");
        builder.Append(_criteria.SearchQuery);
        builder.Append(", SortColumn = ");
        builder.Append(_criteria.SortColumn);
        builder.Append(", SortDescending = ");
        builder.Append(_criteria.SortDescending);
        builder.Append(", Domain = ");
        builder.Append(Domain);
        builder.Append(", AggregateId = ");
        builder.Append(AggregateId);
        builder.Append(", QueryType = ");
        builder.Append(QueryType);
        builder.Append(", EntityId = ");
        builder.Append(EntityId);
        builder.Append(", ProjectionActorType = ");
        builder.Append(ProjectionActorType);
        builder.Append(", ETags = ");
        builder.Append(ETags);
        builder.Append(", CacheDiscriminator = ");
        builder.Append(CacheDiscriminator);
        builder.Append(", CachePayloadVersion = ");
        builder.Append(CachePayloadVersion);
        return true;
    }
}
