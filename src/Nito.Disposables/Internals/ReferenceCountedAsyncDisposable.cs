#if NETSTANDARD2_1
using Nito.Disposables.Advanced;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public sealed class ReferenceCountedAsyncDisposable<T> : IReferenceCountedAsyncDisposable<T>
        where T : class, IAsyncDisposable
    {
        private readonly SingleAsyncDisposable<IReferenceCounter> _singleDisposable;

        /// <summary>
        /// Initializes a reference counted disposable that refers to the specified reference count. The specified reference count must have already been incremented for this instance.
        /// </summary>
        public ReferenceCountedAsyncDisposable(IReferenceCounter referenceCounter)
        {
            _ = referenceCounter ?? throw new ArgumentNullException(nameof(referenceCounter));
            _singleDisposable = new(referenceCounter, context => (context.TryDecrementCount() as IAsyncDisposable)?.DisposeAsync() ?? new ValueTask());

            // Ensure we can cast from the stored IDisposable to T.
            _ = ((IReferenceCountedAsyncDisposable<T>) this).Target;
        }

        T? IReferenceCountedAsyncDisposable<T>.Target => (T?) ReferenceCounter.TryGetTarget();

        IReferenceCountedAsyncDisposable<T> IReferenceCountedAsyncDisposable<T>.AddReference()
        {
            var referenceCounter = ReferenceCounter;
            if (!referenceCounter.TryIncrementCount())
                throw new ObjectDisposedException(nameof(ReferenceCountedAsyncDisposable<T>)); // cannot actually happen
            return new ReferenceCountedAsyncDisposable<T>(referenceCounter);
        }

        IWeakReferenceCountedAsyncDisposable<T> IReferenceCountedAsyncDisposable<T>.AddWeakReference() => new WeakReferenceCountedAsyncDisposable<T>(ReferenceCounter);

        ValueTask IAsyncDisposable.DisposeAsync() => _singleDisposable.DisposeAsync();

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
                    throw new ObjectDisposedException(nameof(ReferenceCountedAsyncDisposable<T>));
                return referenceCounter;
            }
        }
    }
}
#endif