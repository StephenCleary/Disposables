using System;

namespace Nito.Disposables
{
    /// <summary>
    /// An instance that represents an uncounted weak reference. All members are threadsafe.
    /// </summary>
    public interface IWeakReferenceCountedDisposable<out T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Adds a reference to this reference counted disposable. Returns <c>null</c> if the underlying disposable has already been disposed or garbage collected.
        /// </summary>
        IReferenceCountedDisposable<T>? TryAddReference();

        /// <summary>
        /// Attempts to get the target object. Returns <c>null</c> if the underlying disposable has already been disposed or garbage collected.
        /// </summary>
        T? TryGetTarget();
    }
}
