using System;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// A reference count for an underlying target.
    /// </summary>
    public interface IReferenceCounter<out T>
    {
        /// <summary>
        /// Increments the reference count and returns <c>true</c>. If the reference count has already reached zero, returns <c>false</c>.
        /// </summary>
        bool TryIncrementCount();

        /// <summary>
        /// Decrements the reference count and returns <c>null</c>. If this call causes the reference count to reach zero, returns the underlying target.
        /// </summary>
        T? TryDecrementCount();

        /// <summary>
        /// Returns the underlying target. Returns <c>null</c> if the reference count has already reached zero.
        /// </summary>
        T? TryGetTarget();
    }
}
