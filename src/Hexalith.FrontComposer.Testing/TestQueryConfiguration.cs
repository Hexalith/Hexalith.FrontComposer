using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Testing;

internal sealed record TestQueryConfiguration<T>(
    QueryResult<T>? Result,
    Func<QueryRequest, QueryResult<T>>? Callback) {
    public static TestQueryConfiguration<T> FromResult(QueryResult<T> result) => new(result, null);

    public static TestQueryConfiguration<T> FromCallback(Func<QueryRequest, QueryResult<T>> callback) => new(null, callback);
}
