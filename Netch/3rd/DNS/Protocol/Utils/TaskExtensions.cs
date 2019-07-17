using System;
using System.Threading;
using System.Threading.Tasks;

namespace DNS.Protocol.Utils
{
    public static class TaskExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration registration = token.Register(src =>
            {
                ((TaskCompletionSource<bool>)src).TrySetResult(true);
            }, tcs);

            using (registration)
            {
                if (await Task.WhenAny(task, tcs.Task) != task)
                {
                    throw new OperationCanceledException(token);
                }
            }

            return await task;
        }

        public static async Task<T> WithCancellationTimeout<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout))
            using (CancellationTokenSource linkSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken))
            {
                return await task.WithCancellation(linkSource.Token);
            }
        }
    }
}
