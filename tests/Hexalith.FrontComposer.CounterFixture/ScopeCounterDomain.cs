using System.Collections.Concurrent;
using System.Text.Json;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;

namespace Hexalith.FrontComposer.CounterFixture;

/// <summary>A local-only command that appends one distinguishable projection row.</summary>
public sealed record AddScopeRow(string RowId, string AggregateId) : ICommandContract {
    public static string Domain => "counter";

    public static string CommandType => "add-scope-row";
}

/// <summary>The persisted event for one fixture row.</summary>
public sealed record ScopeRowAdded(string RowId) : IEventPayload;

/// <summary>Aggregate state rebuilt by EventStore before each command.</summary>
public sealed class ScopeCounterState {
    public List<string> RowIds { get; set; } = [];

    public void Apply(ScopeRowAdded added) {
        ArgumentNullException.ThrowIfNull(added);
        RowIds.Add(added.RowId);
    }
}

/// <summary>EventStore command processor for the fixture's counter domain.</summary>
[EventStoreDomain("counter")]
public sealed class ScopeCounterAggregate : EventStoreAggregate<ScopeCounterState> {
    public static DomainResult Handle(AddScopeRow command, ScopeCounterState? state) {
        ArgumentNullException.ThrowIfNull(command);
        return DomainResult.Success([new ScopeRowAdded(command.RowId)]);
    }
}

/// <summary>Ephemeral query state rebuilt from EventStore's full event replay.</summary>
public sealed class FixtureProjectionRows {
    private readonly ConcurrentDictionary<string, string[]> _rows = new(StringComparer.Ordinal);

    public void Replace(string tenant, string[] rows) => _rows[tenant] = rows;

    public string[] Get(string tenant) => _rows.TryGetValue(tenant, out string[]? rows) ? rows : [];
}

/// <summary>Projects genuine EventStore events and publishes the Shell projection identity.</summary>
[EventStoreDomain("counter")]
public sealed class ScopeCounterProjection(FixtureProjectionRows rows) : IDomainProjectionHandler {
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => "counter";

    public ProjectionResponse Project(ProjectionRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        List<string> markers = [];
        foreach (ProjectionEventDto evt in request.Events) {
            if (evt.EventTypeName.EndsWith(nameof(ScopeRowAdded), StringComparison.Ordinal)) {
                ScopeRowAdded? added = JsonSerializer.Deserialize<ScopeRowAdded>(
                    evt.Payload, _jsonOptions);
                if (added is not null) {
                    markers.Add(added.RowId);
                }
            }
        }

        string[] snapshot = [.. markers];
        rows.Replace(request.TenantId, snapshot);
        return new ProjectionResponse(
            "counter-projection",
            JsonSerializer.SerializeToElement(snapshot.Select(static marker => new { id = marker })));
    }
}

/// <summary>Serves the exact query type sent by the production count reader.</summary>
[EventStoreDomain("counter")]
public sealed class ScopeCounterQuery(FixtureProjectionRows rows) : IDomainQueryHandler {
    public string Domain => "counter";

    public string QueryType => "Counter.Domain.CounterProjection";

    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        string[] snapshot = rows.Get(query.TenantId);
        return Task.FromResult(QueryResult.FromPayload(
            JsonSerializer.SerializeToElement(snapshot.Select(static marker => new { id = marker })),
            "counter-projection"));
    }
}
