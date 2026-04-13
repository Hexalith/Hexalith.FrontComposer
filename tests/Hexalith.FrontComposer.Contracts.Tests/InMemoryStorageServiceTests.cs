namespace Hexalith.FrontComposer.Contracts.Tests;

using Hexalith.FrontComposer.Contracts.Storage;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="InMemoryStorageService"/>.
/// </summary>
public class InMemoryStorageServiceTests
{
    [Fact]
    public async Task FlushAsync_CompletesSuccessfully()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();
        await sut.SetAsync("key1", "value1", ct);
        await sut.SetAsync("key2", "value2", ct);

        // Act & Assert — should not throw
        await sut.FlushAsync(ct);

        // Verify in-memory flush is a no-op because there are no pending writes
        (await sut.GetAsync<string>("key1", ct)).ShouldBe("value1");
        (await sut.GetAsync<string>("key2", ct)).ShouldBe("value2");
    }

    [Fact]
    public async Task GetAsync_KeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();

        // Act
        string? result = await sut.GetAsync<string>("nonexistent", ct);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetKeysAsync_NoMatchingKeys_ReturnsEmptyList()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();
        await sut.SetAsync("alpha:one", 1, ct);
        await sut.SetAsync("alpha:two", 2, ct);

        // Act
        IReadOnlyList<string> keys = await sut.GetKeysAsync("beta:", ct);

        // Assert
        keys.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetKeysAsync_WithPrefix_ReturnsOnlyMatchingKeys()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();
        await sut.SetAsync("tenant1:user1:feature:a", "v1", ct);
        await sut.SetAsync("tenant1:user1:feature:b", "v2", ct);
        await sut.SetAsync("tenant2:user1:feature:c", "v3", ct);

        // Act
        IReadOnlyList<string> keys = await sut.GetKeysAsync("tenant1:", ct);

        // Assert
        keys.Count.ShouldBe(2);
        keys.ShouldContain("tenant1:user1:feature:a");
        keys.ShouldContain("tenant1:user1:feature:b");
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesIt()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();
        await sut.SetAsync("key", "value", ct);

        // Act
        await sut.RemoveAsync("key", ct);

        // Assert
        (await sut.GetAsync<string>("key", ct)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_MissingKey_DoesNotThrow()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();

        // Act & Assert — should not throw
        await sut.RemoveAsync("nonexistent", ct);
    }

    [Fact]
    public async Task SetAndGetAsync_ConcurrentAccess_NoDataCorruption()
    {
        // Arrange
        var sut = new InMemoryStorageService();
        const int operationCount = 100;

        // Act — 100 parallel writes
        Task[] writeTasks = Enumerable.Range(0, operationCount)
            .Select(i => sut.SetAsync($"key{i}", $"value{i}", CancellationToken.None))
            .ToArray();
        await Task.WhenAll(writeTasks);

        // 100 parallel reads — collect tasks, then await all
        Task<string?>[] readTasks = new Task<string?>[operationCount];
        for (int i = 0; i < operationCount; i++)
        {
            readTasks[i] = sut.GetAsync<string>($"key{i}", CancellationToken.None);
        }

        string?[] results = await Task.WhenAll(readTasks);

        // Assert — no lost updates
        for (int i = 0; i < operationCount; i++)
        {
            results[i].ShouldBe($"value{i}");
        }
    }

    [Fact]
    public async Task SetAsync_SameKeyTwice_OverwritesValue()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();
        await sut.SetAsync("key", "first", ct);

        // Act
        await sut.SetAsync("key", "second", ct);

        // Assert
        (await sut.GetAsync<string>("key", ct)).ShouldBe("second");
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        // Arrange
        CancellationToken ct = TestContext.Current.CancellationToken;
        var sut = new InMemoryStorageService();

        // Act
        await sut.SetAsync("myKey", 42, ct);
        int? result = await sut.GetAsync<int>("myKey", ct);

        // Assert
        result.ShouldBe(42);
    }
}
