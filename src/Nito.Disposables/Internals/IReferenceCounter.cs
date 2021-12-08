using System;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// A reference count for an underlying disposable.
    /// </summary>
    public interface IReferenceCounter
    {
        /// <summary>
        /// Increments the reference count and returns <c>true</c>. If the reference count has already reached zero, returns <c>false</c>.
        /// </summary>
        bool TryIncrementCount();

        /// <summary>
        /// Decrements the reference count and returns <c>null</c>. If this call causes the reference count to reach zero, returns the underlying disposable.
        /// </summary>
        IDisposable? TryDecrementCount();

        /// <summary>
        /// Returns the underlying disposable. Returns <c>null</c> if the reference count has reached zero.
        /// </summary>
        IDisposable? TryGetTarget();
    }
}
