using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public sealed class ReferenceCountedDisposable<T> : SingleDisposable<IReferenceCounter<T>>, IReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Initializes a reference counted disposable that refers to the specified reference count. The specified reference count must have already been incremented for this instance.
        /// </summary>
        public ReferenceCountedDisposable(IReferenceCounter<T> referenceCounter)
            : base(referenceCounter)
        {
        }

        /// <inheritdoc/>
        protected override void Dispose(IReferenceCounter<T> referenceCounter)
        {
            referenceCounter.TryDecrementCount()?.Dispose();
        }

        T IReferenceCountedDisposable<T>.Target => ReferenceCounter.TryGetTarget() ?? throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));

        IReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddReference()
        {
            var referenceCounter = ReferenceCounter;
            if (!referenceCounter.TryIncrementCount())
                throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
            return new ReferenceCountedDisposable<T>(referenceCounter);
        }

        IWeakReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddWeakReference() => new WeakReferenceCountedDisposable<T>(ReferenceCounter);

        private IReferenceCounter<T> ReferenceCounter
        {
            get
            {
                IReferenceCounter<T> referenceCounter = null!;
                // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
                // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
                if (!TryUpdateContext(x => referenceCounter = x))
                    throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
                return referenceCounter;
            }
        }
    }
}
