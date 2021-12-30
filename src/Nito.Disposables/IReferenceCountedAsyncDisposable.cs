#if NETSTANDARD2_1
using System;

namespace Nito.Disposables
{
    /// <summary>
    /// An instance that represents a reference count. All members are threadsafe.
    /// </summary>
    public interface IReferenceCountedAsyncDisposable<out T> : IAsyncDisposable
        where T : class, IAsyncDisposable
    {
        /// <summary>
        /// Adds a weak reference to this reference counted disposable. Throws <see cref="ObjectDisposedException"/> if this instance is disposed.
        /// </summary>
        IWeakReferenceCountedAsyncDisposable<T> AddWeakReference();

        /// <summary>
        /// Returns a new reference to this reference counted disposable, incrementing the reference counter. Throws <see cref="ObjectDisposedException"/> if this instance is disposed.
        /// </summary>
        IReferenceCountedAsyncDisposable<T> AddReference();

        /// <summary>
        /// Gets the target object. Throws <see cref="ObjectDisposedException"/> if this instance is disposed.
        /// </summary>
        T? Target { get; }

        /// <summary>
        /// Whether this instance is currently disposing or has been disposed.
        /// </summary>
        public bool IsDisposeStarted { get; }

        /// <summary>
        /// Whether this instance is disposed (finished disposing).
        /// </summary>
        public bool IsDisposed { get; }

        /// <summary>
        /// Whether this instance is currently disposing, but not finished yet.
        /// </summary>
        public bool IsDisposing { get; }
    }
}
#endif