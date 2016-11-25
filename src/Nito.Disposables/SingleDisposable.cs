using System;
using System.Threading;

namespace Nito.Disposables
{
    /// <summary>
    /// A base class for disposables that need exactly-once semantics in a threadsafe way.
    /// </summary>
    /// <typeparam name="T">The type of "context" for the derived disposable.</typeparam>
    public abstract class SingleDisposable<T> : IDisposable
        where T : class
    {
        /// <summary>
        /// The context. This may be <c>null</c>.
        /// </summary>
        private T _context;

        /// <summary>
        /// Creates a disposable for the specified context.
        /// </summary>
        /// <param name="context">The context passed to <see cref="Dispose(T)"/>. If this is <c>null</c>, then <see cref="Dispose(T)"/> will never be called.</param>
        protected SingleDisposable(T context)
        {
            _context = context;
        }

        /// <summary>
        /// The actul disposal method, called only once from <see cref="Dispose()"/>. If the context passed to the constructor of this instance is <c>null</c>, then this method is never called.
        /// </summary>
        /// <param name="context">The context for the disposal operation. This is never <c>null</c>.</param>
        protected abstract void Dispose(T context);

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            var context = Interlocked.Exchange(ref _context, null);
            if (context != null)
                Dispose(context);
        }

        /// <summary>
        /// Attempts to update the stored context. This method returns <c>false</c> if this instance has already been disposed.
        /// </summary>
        /// <param name="contextUpdater">The function used to update an existing context. This may be called more than once if more than one thread attempts to simultanously update the context.</param>
        protected bool TryUpdateContext(Func<T, T> contextUpdater)
        {
            while (true)
            {
                var originalContext = Interlocked.CompareExchange(ref _context, _context, _context);
                if (originalContext == null)
                    return false;
                var updatedContext = contextUpdater(originalContext);
                var result = Interlocked.CompareExchange(ref _context, updatedContext, originalContext);
                if (ReferenceEquals(originalContext, result))
                    return true;
            }
        }
    }
}
