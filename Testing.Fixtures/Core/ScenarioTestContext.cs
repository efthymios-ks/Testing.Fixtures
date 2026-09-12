using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Testing.Fixtures.Core;

/// <summary>
/// What one test owns: its id, its logger, and whatever the configurations put on it — the
/// database it leased, the hosts it is driving.
/// </summary>
public sealed class ScenarioTestContext(ILogger logger)
{
    private readonly ConcurrentDictionary<string, object> _items = new(StringComparer.Ordinal);

    public string ScenarioId { get; } = Guid.NewGuid().ToString("N");

    public ILogger Logger { get; } = logger;

    public void Set<TItem>(TItem value)
        where TItem : notnull
        => Set(value, KeyOf<TItem>());

    public void Set<TItem>(TItem value, string key)
        where TItem : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _items[key] = value;
    }

    public TItem Get<TItem>()
        => Get<TItem>(KeyOf<TItem>());

    public TItem Get<TItem>(string key)
        => TryGet<TItem>(key, out var value)
            ? value
            : throw new InvalidOperationException(
                $"'{key}' is not on the test context. The configuration that publishes it either did not run or failed."
            );

    public bool TryGet<TItem>(out TItem value)
        => TryGet(KeyOf<TItem>(), out value);

    public bool TryGet<TItem>(string key, out TItem value)
    {
        if (_items.TryGetValue(key, out var item) && item is TItem typed)
        {
            value = typed;

            return true;
        }

        value = default!;

        return false;
    }

    private static string KeyOf<TItem>()
        => typeof(TItem).FullName ?? typeof(TItem).Name;
}
