using Nito.Disposables.Advanced;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public sealed class ReferenceCountedDisposable<T> : IReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        private readonly SingleDisposable<IReferenceCounter> _singleDisposable;

        /// <summary>
        /// Initializes a reference counted disposable that refers to the specified reference count. The specified reference count must have already been incremented for this instance.
        /// </summary>
        public ReferenceCountedDisposable(IReferenceCounter referenceCounter)
        {
            _ = referenceCounter ?? throw new ArgumentNullException(nameof(referenceCounter));
            _singleDisposable = new(referenceCounter, context => (context.TryDecrementCount() as IDisposable)?.Dispose());

            // Ensure we can cast from the stored IDisposable to T.
            _ = ((IReferenceCountedDisposable<T>) this).Target;
        }

        T? IReferenceCountedDisposable<T>.Target => (T?) ReferenceCounter.TryGetTarget();

        IReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddReference()
        {
            var referenceCounter = ReferenceCounter;
            if (!referenceCounter.TryIncrementCount())
                throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>)); // cannot actually happen
            return new ReferenceCountedDisposable<T>(referenceCounter);
        }

        IWeakReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddWeakReference() => new WeakReferenceCountedDisposable<T>(ReferenceCounter);

        void IDisposable.Dispose() => _singleDisposable.Dispose();

        bool IDisposableProperties.IsDisposeStarted => _singleDisposable.IsDisposeStarted;

        bool IDisposableProperties.IsDisposed => _singleDisposable.IsDisposed;

        bool IDisposableProperties.IsDisposing => _singleDisposable.IsDisposing;

        private IReferenceCounter ReferenceCounter
        {
            get
            {
                IReferenceCounter referenceCounter = null!;
                // Implementation note: this always "succeeds" in updating the context since it always returns the same instance.
                // So, we know that this will be called at most once. It may also be called zero times if this instance is disposed.
                if (!_singleDisposable.TryUpdateContext(x => referenceCounter = x))
                    throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
                return referenceCounter;
            }
        }
    }
}
