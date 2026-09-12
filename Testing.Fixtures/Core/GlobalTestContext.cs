using System.Collections.Concurrent;

namespace Testing.Fixtures.Core;

/// <summary>What lives for the whole run — containers, pools, anything started once.</summary>
public sealed class GlobalTestContext
{
    private readonly ConcurrentDictionary<Type, object> _items = new();

    public void Set<TItem>(TItem value)
        where TItem : notnull
        => _items[typeof(TItem)] = value;

    public TItem Get<TItem>()
        => TryGet<TItem>(out var value)
            ? value
            : throw new InvalidOperationException(
                $"'{typeof(TItem).Name}' is not on the global context. The configuration that publishes it either did not run or failed."
            );

    public bool TryGet<TItem>(out TItem value)
    {
        if (_items.TryGetValue(typeof(TItem), out var item))
        {
            value = (TItem)item;

            return true;
        }

        value = default!;

        return false;
    }
}
