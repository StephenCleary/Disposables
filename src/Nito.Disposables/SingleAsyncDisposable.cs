#if NETSTANDARD2_1
using System;
using System.Threading.Tasks;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// A base class for disposables that need exactly-once semantics in a thread-safe way. All disposals of this instance block until the disposal is complete.
    /// </summary>
    /// <typeparam name="T">The type of "context" for the derived disposable. Since the context should not be modified, strongly consider making this an immutable type.</typeparam>
    /// <remarks>
    /// <para>If <see cref="DisposeAsync()"/> is called multiple times, only the first call will execute the disposal code. Other calls to <see cref="DisposeAsync()"/> will wait for the disposal to complete.</para>
    /// </remarks>
    public abstract class SingleAsyncDisposable<T> : IAsyncDisposable
    {
        /// <summary>
        /// The context. This is never <c>null</c>. This is empty if this instance has already been disposed (or is being disposed).
        /// </summary>
        private readonly BoundAsyncActionField<T> _context;

        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        /// <summary>
        /// Creates a disposable for the specified context.
        /// </summary>
        /// <param name="context">The context passed to <see cref="DisposeAsync(T)"/>.</param>
        protected SingleAsyncDisposable(T context)
        {
            _context = new BoundAsyncActionField<T>(DisposeAsync, context);
        }

        /// <summary>
        /// Whether this instance is currently disposing or has been disposed.
        /// </summary>
        public bool IsDisposeStarted => _context.IsEmpty;

        /// <summary>
        /// Whether this instance is disposed (finished disposing).
        /// </summary>
        public bool IsDisposed => _tcs.Task.IsCompleted;

        /// <summary>
        /// Whether this instance is currently disposing, but not finished yet.
        /// </summary>
        public bool IsDisposing => IsDisposeStarted && !IsDisposed;

        /// <summary>
        /// The actul disposal method, called only once from <see cref="DisposeAsync()"/>.
        /// </summary>
        /// <param name="context">The context for the disposal operation.</param>
        protected abstract ValueTask DisposeAsync(T context);

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="DisposeAsync()"/> is called multiple times, only the first call will execute the disposal code. Other calls to <see cref="DisposeAsync()"/> will wait for the disposal to complete.</para>
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            var context = _context.TryGetAndUnset();
            if (context == null)
            {
                await _tcs.Task.ConfigureAwait(false);
                return;
            }
            try
            {
                await context.InvokeAsync().ConfigureAwait(false);
            }
            finally
            {
                _tcs.TrySetResult(null!);
            }
        }

        /// <summary>
        /// Attempts to update the stored context. This method returns <c>false</c> if this instance has already been disposed (or is being disposed).
        /// </summary>
        /// <param name="contextUpdater">The function used to update an existing context. This may be called more than once if more than one thread attempts to simultaneously update the context.</param>
        protected bool TryUpdateContext(Func<T, T> contextUpdater) => _context.TryUpdateContext(contextUpdater);
    }
}
#endif