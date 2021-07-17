// from: https://gist.github.com/cilliemalan/77cb177d9045244bfb9a939f8b96e9df#file-asyncawaiter-cs

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RadiantPi.Internal {

    public static class CancellationTokenEx {

        //--- Types ---

        /// <summary>
        /// The awaiter for cancellation tokens.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion {

            //--- Fields ---
            internal CancellationToken CancellationToken;

            //--- Constructors ---
            public CancellationTokenAwaiter(CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
            }

            //--- Methods ---
            public object GetResult() {

                // this is called by compiler generated methods when the
                // task has completed. Instead of returning a result, we
                // just throw an exception.
                if(IsCompleted) {
                    throw new OperationCanceledException();
                } else {
                    throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
                }
            }

            // called by compiler generated/.net internals to check
            // if the task has completed.
            public bool IsCompleted => CancellationToken.IsCancellationRequested;

            // The compiler will generate stuff that hooks in
            // here. We hook those methods directly into the
            // cancellation token.
            public void OnCompleted(Action continuation) => CancellationToken.Register(continuation);
            public void UnsafeOnCompleted(Action continuation) => CancellationToken.Register(continuation);
        }

        //--- Extension Methods ---

        /// <summary>
        /// Allows a cancellation token to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct) => new CancellationTokenAwaiter(ct);
    }
}