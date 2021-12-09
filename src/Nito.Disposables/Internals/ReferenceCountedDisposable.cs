using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public sealed class ReferenceCountedDisposable<T> : SingleDisposable<IReferenceCounter<IDisposable>>, IReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Initializes a reference counted disposable that refers to the specified reference count. The specified reference count must have already been incremented for this instance.
        /// </summary>
        public ReferenceCountedDisposable(IReferenceCounter<IDisposable> referenceCounter)
            : base(referenceCounter)
        {
            _ = referenceCounter ?? throw new ArgumentNullException(nameof(referenceCounter));

            // Ensure we can cast from the stored IDisposable to T.
            _ = (T) referenceCounter.TryGetTarget()!;
        }

        /// <inheritdoc/>
        protected override void Dispose(IReferenceCounter<IDisposable> referenceCounter) => referenceCounter.TryDecrementCount()?.Dispose();

        T IReferenceCountedDisposable<T>.Target => (T) (ReferenceCounter.TryGetTarget() ?? throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>)));

        IReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddReference()
        {
            var referenceCounter = ReferenceCounter;
            if (!referenceCounter.TryIncrementCount())
                throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
            return new ReferenceCountedDisposable<T>(referenceCounter);
        }

        IWeakReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddWeakReference() => new WeakReferenceCountedDisposable<T>(ReferenceCounter);

        private IReferenceCounter<IDisposable> ReferenceCounter
        {
            get
            {
                IReferenceCounter<IDisposable> referenceCounter = null!;
                // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
                // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
                if (!TryUpdateContext(x => referenceCounter = x))
                    throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
                return referenceCounter;
            }
        }
    }
}
