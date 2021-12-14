using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// An instance that represents an uncounted weak reference.
    /// </summary>
    public sealed class WeakReferenceCountedDisposable<T> : IWeakReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        private readonly WeakReference<IReferenceCounter> _weakReference;

        /// <summary>
        /// Creates an instance that weakly references the specified reference counter. The specified reference counter should not be incremented.
        /// </summary>
        public WeakReferenceCountedDisposable(IReferenceCounter referenceCounter)
        {
            _ = referenceCounter ?? throw new ArgumentNullException(nameof(referenceCounter));

            _weakReference = new(referenceCounter);

            // Ensure we can cast from the stored disposable to T.
            _ = (T?) referenceCounter.TryGetTarget()!;
        }

        IReferenceCountedDisposable<T>? IWeakReferenceCountedDisposable<T>.TryAddReference()
        {
            if (!_weakReference.TryGetTarget(out var referenceCounter))
                return null;
            if (!referenceCounter.TryIncrementCount())
                return null;
            return new ReferenceCountedDisposable<T>(referenceCounter);
        }

        T? IWeakReferenceCountedDisposable<T>.TryGetTarget()
        {
            if (!_weakReference.TryGetTarget(out var referenceCounter))
                return null;
            return (T?) referenceCounter.TryGetTarget();
        }
    }
}
