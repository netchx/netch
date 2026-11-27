#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Controllers;
using Netch.Models.GitHubRelease;
using Netch.Utils;

namespace Tests;

[TestClass]
public class AsyncPrimitivesTests
{
    [TestMethod]
    public async Task AsyncLockEnforcesMutualExclusionAndSameFlowReentrancy()
    {
        using var asyncLock = new AsyncLock();
        Assert.IsFalse(asyncLock.IsLockHeld);

        var enteredFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAcquiredBeforeFirstReleased = false;
        var secondSawHeldWhileWaiting = false;

        var first = Task.Run(async () =>
        {
            await using var _ = await asyncLock.EnterAsync();
            Assert.IsTrue(asyncLock.IsLockHeld);

            // Re-entrancy check on the same async flow across an await boundary.
            await Task.Yield();
            Assert.IsTrue(asyncLock.IsLockHeld);

            enteredFirst.SetResult();
            await releaseFirst.Task;
        });

        await enteredFirst.Task;

        var waitingSecondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = Task.Run(async () =>
        {
            secondSawHeldWhileWaiting = asyncLock.IsLockHeld;
            var enterTask = asyncLock.EnterAsync().AsTask();
            secondSawHeldWhileWaiting |= asyncLock.IsLockHeld;
            waitingSecondStarted.SetResult();

            await using var _ = await enterTask;
            secondAcquiredBeforeFirstReleased = !releaseFirst.Task.IsCompleted;
            Assert.IsTrue(asyncLock.IsLockHeld);
        });

        await waitingSecondStarted.Task;
        await Task.Delay(30);
        Assert.IsFalse(second.IsCompleted);
        Assert.IsFalse(secondSawHeldWhileWaiting);

        releaseFirst.SetResult();
        await Task.WhenAll(first, second);

        Assert.IsFalse(secondAcquiredBeforeFirstReleased);
        Assert.IsFalse(asyncLock.IsLockHeld);
    }

    [TestMethod]
    public async Task AsyncLockCancellationWhileWaitingDoesNotCorruptStateOrLeakSemaphore()
    {
        using var asyncLock = new AsyncLock();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holder = Task.Run(async () =>
        {
            await using (await asyncLock.EnterAsync())
            {
                Assert.IsTrue(asyncLock.IsLockHeld);
                entered.SetResult();
                await release.Task;
            }
        });

        await entered.Task;
        Assert.IsFalse(asyncLock.IsLockHeld);

        using (var cts = new CancellationTokenSource(20))
        {
            var threw = false;
            try
            {
                await using var _ = await asyncLock.EnterAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                threw = true;
            }

            Assert.IsTrue(threw);
            Assert.IsFalse(asyncLock.IsLockHeld);
        }

        release.SetResult();
        await holder;
        Assert.IsFalse(asyncLock.IsLockHeld);

        // Lock must remain acquirable after waiter cancellation.
        await using (await asyncLock.EnterAsync())
        {
            Assert.IsTrue(asyncLock.IsLockHeld);
        }

        Assert.IsFalse(asyncLock.IsLockHeld);
    }

    [TestMethod]
    public async Task AsyncSemaphoreSerializesAndDoubleDisposeDoesNotOverRelease()
    {
        using var semaphore = new AsyncSemaphore(1);

        var releaser1 = await semaphore.EnterAsync();
        var waiterTask = Task.Run(async () =>
        {
            using var _ = await semaphore.EnterAsync();
        });

        await Task.Delay(30);
        Assert.IsFalse(waiterTask.IsCompleted);

        releaser1.Dispose();
        releaser1.Dispose();
        await waiterTask;

        // If double-dispose over-released, two simultaneous acquisitions would both succeed before release.
        using var holder = await semaphore.EnterAsync();
        using var cts = new CancellationTokenSource(30);
        var threw = false;
        try
        {
            using var _ = await semaphore.EnterAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            threw = true;
        }

        Assert.IsTrue(threw);
    }

    [TestMethod]
    public async Task AsyncLazyInvokesFactoryOnceAcrossConcurrentWaiters()
    {
        var invocations = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lazy = new AsyncLazy<int>(async () =>
        {
            Interlocked.Increment(ref invocations);
            await gate.Task;
            return 42;
        });

        var t1 = Task.Run(lazy.GetValueAsync);
        var t2 = Task.Run(lazy.GetValueAsync);
        var t3 = Task.Run(lazy.GetValueAsync);

        gate.SetResult();
        var results = await Task.WhenAll(t1, t2, t3);

        Assert.AreEqual(1, Volatile.Read(ref invocations));
        CollectionAssert.AreEqual(new[] { 42, 42, 42 }, results);
    }

    [TestMethod]
    public async Task TaskForgetHandlesNullAndFaultedTasksWithoutThrowing()
    {
        Task? nullTask = null;
        nullTask.Forget();

        var alreadyFaulted = Task.FromException(new InvalidOperationException("boom"));
        alreadyFaulted.Forget();

        var tcs = new TaskCompletionSource();
        tcs.Task.Forget();
        tcs.SetException(new InvalidOperationException("async boom"));
        await Task.Delay(20);
    }

    [TestMethod]
    public void UpdateCheckerSelectsZipOver7zAndParsesFourRowSha256Table()
    {
        var assets = new[]
        {
            new Asset { name = "Netch-2.0.1-winui-x64-self-contained.7z" },
            new Asset { name = "Netch-2.0.1-winui-x64-self-contained.zip" },
            new Asset { name = "Netch-2.0.1-winui-x64-framework-dependent.7z" },
            new Asset { name = "Netch-2.0.1-winui-x64-framework-dependent.zip" }
        };

        var sc = UpdateChecker.SelectReleaseAsset(assets, selfContained: true);
        var fdd = UpdateChecker.SelectReleaseAsset(assets, selfContained: false);

        Assert.AreEqual("Netch-2.0.1-winui-x64-self-contained.zip", sc?.name);
        Assert.AreEqual("Netch-2.0.1-winui-x64-framework-dependent.zip", fdd?.name);

        var scZipSha = new string('a', 64);
        var fddZipSha = new string('b', 64);
        var sc7zSha = new string('c', 64);
        var fdd7zSha = new string('d', 64);

        var previous = UpdateChecker.LatestRelease;
        try
        {
            UpdateChecker.LatestRelease = new Release
            {
                body = $$"""
                    ## SHA256

                    | 文件 / File | SHA256 |
                    | --- | --- |
                    | `Netch-2.0.1-winui-x64-self-contained.zip` | `{{scZipSha}}` |
                    | `Netch-2.0.1-winui-x64-framework-dependent.zip` | `{{fddZipSha}}` |
                    | `Netch-2.0.1-winui-x64-self-contained.7z` | `{{sc7zSha}}` |
                    | `Netch-2.0.1-winui-x64-framework-dependent.7z` | `{{fdd7zSha}}` |
                    """
            };

            Assert.AreEqual(("Netch-2.0.1-winui-x64-self-contained.zip", scZipSha), UpdateChecker.GetLatestUpdateFileNameAndHash());
            Assert.AreEqual(("Netch-2.0.1-winui-x64-framework-dependent.zip", fddZipSha), UpdateChecker.GetLatestUpdateFileNameAndHash("framework-dependent"));
        }
        finally
        {
            UpdateChecker.LatestRelease = previous;
        }
    }
}
