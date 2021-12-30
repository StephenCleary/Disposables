using System;
using Nito.Disposables.Internals;

namespace Nito.Disposables.Advanced
{
    /// <summary>
    /// A helper class for disposables that need exactly-once semantics in a thread-safe way.
    /// </summary>
    /// <typeparam name="T">The type of "context" for the derived disposable. Since the context should not be modified, strongly consider making this an immutable type.</typeparam>
    /// <remarks>
    /// <para>If <see cref="Dispose()"/> is called multiple times, only the first call will execute the disposal code. Other calls to <see cref="Dispose()"/> will not wait for the disposal to complete.</para>
    /// </remarks>
    public sealed class SingleNonblockingDisposable<T> : IDisposable
    {
        /// <summary>
        /// The context. This is never <c>null</c>. This is empty if this instance has already been disposed (or is being disposed).
        /// </summary>
        private readonly BoundActionField<T> _context;

        /// <summary>
        /// Creates a disposable for the specified context.
        /// </summary>
        /// <param name="context">The context passed to <paramref name="dispose"/>. May be <c>null</c>.</param>
        /// <param name="dispose">The actual disposal method, called only once from <see cref="Dispose()"/>.</param>
        public SingleNonblockingDisposable(T context, Action<T> dispose)
        {
            _context = new BoundActionField<T>(dispose, context);
        }

        /// <summary>
        /// Whether this instance has been disposed (or is being disposed).
        /// </summary>
        public bool IsDisposed => _context.IsEmpty;

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="Dispose()"/> is called multiple times, only the first call will execute the disposal code. Other calls to <see cref="Dispose()"/> will not wait for the disposal to complete.</para>
        /// </remarks>
        public void Dispose() => _context.TryGetAndUnset()?.Invoke();

        /// <summary>
        /// Attempts to update the stored context. This method returns <c>false</c> if this instance has already been disposed (or is being disposed).
        /// </summary>
        /// <param name="contextUpdater">The function used to update an existing context. This may be called more than once if more than one thread attempts to simultaneously update the context.</param>
        public bool TryUpdateContext(Func<T, T> contextUpdater) => _context.TryUpdateContext(contextUpdater);
    }
}
