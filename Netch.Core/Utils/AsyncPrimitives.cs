using System.Diagnostics;

namespace Netch.Utils;

public static class TaskExtensions
{
    public static void Forget(this Task? task)
    {
        if (task == null)
            return;

        if (task.IsCompleted)
        {
            if (task.IsFaulted)
                Log.Debug(task.Exception, "Unhandled exception in fire-and-forget task");
            return;
        }

        _ = task.ContinueWith(
            static t => Log.Debug(t.Exception, "Unhandled exception in fire-and-forget task"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}

public sealed class AsyncSemaphore(int initialCount) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(initialCount);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}

public sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly AsyncLocal<HoldNode?> _currentHold = new();

    public bool IsLockHeld
    {
        get
        {
            for (var node = _currentHold.Value; node != null; node = node.Parent)
            {
                if (node.Held)
                    return true;
            }

            return false;
        }
    }

    public ValueTask<Releaser> EnterAsync(CancellationToken cancellationToken = default)
    {
        var parent = _currentHold.Value;
        while (parent != null && parent.Done)
            parent = parent.Parent;

        var node = new HoldNode(parent);
        _currentHold.Value = node;

        Task waitTask;
        try
        {
            waitTask = _semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            node.Done = true;
            _currentHold.Value = parent;
            throw;
        }
        if (waitTask.IsCompleted)
        {
            if (waitTask.IsCompletedSuccessfully)
            {
                node.Held = true;
                return ValueTask.FromResult(new Releaser(this, node));
            }

            node.Done = true;
            _currentHold.Value = parent;
            return CompleteFailedAsync(waitTask);
        }

        return CompleteEnterAsync(waitTask, node);
    }

    public void Dispose() => _semaphore.Dispose();

    private async ValueTask<Releaser> CompleteEnterAsync(Task waitTask, HoldNode node)
    {
        try
        {
            await waitTask.ConfigureAwait(false);
        }
        catch
        {
            node.Done = true;
            throw;
        }

        node.Held = true;
        return new Releaser(this, node);
    }

    private static async ValueTask<Releaser> CompleteFailedAsync(Task waitTask)
    {
        await waitTask.ConfigureAwait(false);
        throw new UnreachableException();
    }

    internal sealed class HoldNode(HoldNode? parent)
    {
        public HoldNode? Parent { get; } = parent;
        public volatile bool Held;
        public volatile bool Done;
    }

    public sealed class Releaser : IDisposable, IAsyncDisposable
    {
        private AsyncLock? _owner;
        private readonly HoldNode _node;

        internal Releaser(AsyncLock owner, HoldNode node)
        {
            _owner = owner;
            _node = node;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            if (owner == null)
                return;

            _node.Held = false;
            _node.Done = true;
            owner._currentHold.Value = _node.Parent;
            owner._semaphore.Release();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

public sealed class AsyncLazy<T>(Func<Task<T>> valueFactory)
{
    private readonly Lazy<Task<T>> _lazy = new(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<T> GetValueAsync() => _lazy.Value;
}
