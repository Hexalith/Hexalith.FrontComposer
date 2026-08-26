using Counter.Domain;

namespace Counter.Web;

/// <summary>
/// Carries immutable sample-command snapshots from accepted dispatch into the current Counter
/// circuit's projection catch-up bridge.
/// </summary>
internal sealed class CounterCommandProjectionCatchUpChannel
{
    private long _capturedCount;
    private long _publishedCount;

    /// <summary>Gets the number of exact-key command snapshots captured by this sample process.</summary>
    public long CapturedCount => Interlocked.Read(ref _capturedCount);

    /// <summary>Gets the number of confirmed exact-key snapshots published by this sample process.</summary>
    public long PublishedCount => Interlocked.Read(ref _publishedCount);

    /// <summary>Occurs after an exact-key create reaches the confirmed lifecycle state.</summary>
    public event Action<string, string, string, CreateCounterCommand>? CreateConfirmed;

    /// <summary>Occurs after an exact-key update reaches the confirmed lifecycle state.</summary>
    public event Action<string, string, string, UpdateCounterCommand>? UpdateConfirmed;

    /// <summary>Captures a typed command before dispatch for later confirmed publication.</summary>
    public Action<string?>? Capture<TCommand>(TCommand command, string tenantId, string userId)
        where TCommand : class
        => command switch
        {
            CreateCounterCommand create => CaptureCreate(create, tenantId, userId),
            UpdateCounterCommand update => CaptureUpdate(update, tenantId, userId),
            _ => null,
        };

    private Action<string?> CaptureCreate(CreateCounterCommand command, string tenantId, string userId)
    {
        int published = 0;
        CreateCounterCommand snapshot = new()
        {
            MessageId = command.MessageId,
            TenantId = command.TenantId,
            CounterId = command.CounterId,
            InitialValue = command.InitialValue,
        };
        _ = Interlocked.Increment(ref _capturedCount);
        return messageId =>
        {
            if (Interlocked.Exchange(ref published, 1) != 0)
            {
                return;
            }

            _ = Interlocked.Increment(ref _publishedCount);
            CreateConfirmed?.Invoke(tenantId, userId, messageId ?? snapshot.MessageId, snapshot);
        };
    }

    private Action<string?> CaptureUpdate(UpdateCounterCommand command, string tenantId, string userId)
    {
        int published = 0;
        UpdateCounterCommand snapshot = new()
        {
            MessageId = command.MessageId,
            TenantId = command.TenantId,
            CounterId = command.CounterId,
            Amount = command.Amount,
        };
        _ = Interlocked.Increment(ref _capturedCount);
        return messageId =>
        {
            if (Interlocked.Exchange(ref published, 1) != 0)
            {
                return;
            }

            _ = Interlocked.Increment(ref _publishedCount);
            UpdateConfirmed?.Invoke(tenantId, userId, messageId ?? snapshot.MessageId, snapshot);
        };
    }
}
