using System;
using System.Threading;

namespace Nito.Disposables
{
    /// <summary>
    /// A base class for disposables that need exactly-once semantics in a threadsafe way.
    /// </summary>
    /// <typeparam name="T">The type of "context" for the derived disposable. Since the context should not be modified, strongly consider making this an immutable type.</typeparam>
    /// <remarks>
    /// <para>If <see cref="Dispose()"/> is called multiple times, only the first call will execute the disposal code. Other calls to <see cref="Dispose()"/> will not wait for the disposal to complete.</para>
    /// </remarks>
    public abstract class SingleNonblockingDisposable<T> : IDisposable
    {
        /// <summary>
        /// The context. This is <c>null</c> if this instance has already been disposed (or is being disposed).
        /// </summary>
        private ContextWrapper _context;

        /// <summary>
        /// Creates a disposable for the specified context.
        /// </summary>
        /// <param name="context">The context passed to <see cref="Dispose(T)"/>.</param>
        protected SingleNonblockingDisposable(T context)
        {
            _context = new ContextWrapper(context);
        }

        /// <summary>
        /// Whether this instance has been disposed (or is being disposed).
        /// </summary>
        public bool IsDisposed => _context == null;

        /// <summary>
        /// The actul disposal method, called only once from <see cref="Dispose()"/>.
        /// </summary>
        /// <param name="context">The context for the disposal operation.</param>
        protected abstract void Dispose(T context);

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="Dispose()"/> is called multiple times, only the first call will execute the disposal code. Other calls to <see cref="Dispose()"/> will not wait for the disposal to complete.</para>
        /// </remarks>
        public void Dispose()
        {
            var context = Interlocked.Exchange(ref _context, null);
            if (context != null)
                Dispose(context.Context);
        }

        /// <summary>
        /// Attempts to update the stored context. This method returns <c>false</c> if this instance has already been disposed (or is being disposed).
        /// </summary>
        /// <param name="contextUpdater">The function used to update an existing context. This may be called more than once if more than one thread attempts to simultanously update the context.</param>
        protected bool TryUpdateContext(Func<T, T> contextUpdater)
        {
            while (true)
            {
                var originalContext = Interlocked.CompareExchange(ref _context, _context, _context);
                if (originalContext == null)
                    return false;
                var updatedContext = new ContextWrapper(contextUpdater(originalContext.Context));
                var result = Interlocked.CompareExchange(ref _context, updatedContext, originalContext);
                if (ReferenceEquals(originalContext, result))
                    return true;
            }
        }

        private sealed class ContextWrapper
        {
            public ContextWrapper(T context)
            {
                Context = context;
            }

            public T Context { get; }
        }
    }
}
