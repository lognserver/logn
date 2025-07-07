// Copyright (c) 2025 Codefrog
// Business Source License 1.1 – see LICENSE.txt for details.
// Change Date: 2029-07-01   Change License: Apache-2.0

using System.Collections.Concurrent;

namespace Logn.Flow.Engine;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<object, CancellationToken, ValueTask>>> _subs = new();

    public ValueTask PublishAsync<T>(T @event, CancellationToken ct = default)
    {
        if (_subs.TryGetValue(typeof(T), out var handlers))
            return new(Task.WhenAll(handlers.Select(h => h(@event!, ct).AsTask())));
        return ValueTask.CompletedTask;
    }

    public ValueTask<IDisposable> SubscribeAsync<T>(
        Func<T, CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
    {
        var list = _subs.GetOrAdd(typeof(T), _ => new());
        var wrapper = new Func<object, CancellationToken, ValueTask>((e, c) => handler((T)e, c));
        list.Add(wrapper);

        return ValueTask.FromResult<IDisposable>(new Subscription(() => list.Remove(wrapper)));
    }

    private sealed record Subscription(Action DisposeAction) : IDisposable
    {
        public void Dispose() => DisposeAction();
    }
}