using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cerevo.UB300_Win.Api {
    public static class TaskExtensionMethods {
        // http://blogs.msdn.com/b/pfxteam/archive/2012/10/05/how-do-i-cancel-non-cancelable-async-operations.aspx
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) {
                if (task != await Task.WhenAny(task, tcs.Task)) {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            return await task;
        }
    }
}
