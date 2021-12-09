using System;

namespace Nito.Disposables
{
    /// <summary>
    /// An instance that represents an uncounted weak reference.
    /// </summary>
    public interface IWeakReferenceCountedDisposable<out T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Adds a reference to this reference counted disposable. If the underlying disposable has already been disposed or garbage collected, returns <c>null</c>.
        /// </summary>
        IReferenceCountedDisposable<T>? TryAddReference();

        /// <summary>
        /// Attempts to get the target object. If the underlying disposable has already been disposed or garbage collected, returns <c>null</c>.
        /// </summary>
        T? TryGetTarget();
    }
}
