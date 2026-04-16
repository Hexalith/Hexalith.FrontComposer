namespace Hexalith.FrontComposer.Contracts.Lifecycle;

/// <summary>
/// ULID generator seam. Default Shell implementation (<c>UlidFactory</c>) wraps <c>NUlid.Ulid.NewUlid()</c>.
/// Tests inject a deterministic implementation via constructor substitution (Decision D3).
/// </summary>
public interface IUlidFactory {
    /// <summary>
    /// Returns a new ULID as a 26-character Crockford Base32 string (ULID spec, lexicographically time-sortable).
    /// </summary>
    /// <returns>A new ULID string.</returns>
    string NewUlid();
}
