using System;
using System.Threading;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// Equivalent to <see cref="Disposable"/>.
    /// </summary>
    public sealed class ReferenceCountedDisposable : SingleDisposable<object>, IAddReference
    {
        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public ReferenceCountedDisposable(IDisposable? disposable)
            : this(new ReferenceCounter<IDisposable>(disposable))
        {
        }

        private ReferenceCountedDisposable(IReferenceCounter<IDisposable> referenceCount)
            : base(referenceCount)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(object context)
        {
            var referenceCount = (IReferenceCounter<IDisposable>)context;
            referenceCount.TryDecrementCount()?.Dispose();
        }

        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public ReferenceCountedDisposable? TryAddReference()
        {
            IReferenceCounter<IDisposable> referenceCount = null!;
            // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
            // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
            if (!TryUpdateContext(x => referenceCount = ((IReferenceCounter<IDisposable>)x).TryIncrementCount() ? (IReferenceCounter<IDisposable>)x : null!))
                return null;
            return new ReferenceCountedDisposable(referenceCount);
        }

        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static ReferenceCountedDisposable Create(IDisposable? disposable) => new (disposable);

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. If this <see cref="ReferenceCountedDisposable"/> has already been disposed, returns <c>null</c>.
        /// </summary>
        public IAddReference? TryAddWeakReference() => WeakReference.TryCreate(this);

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. Throws an exception if this <see cref="ReferenceCountedDisposable"/> has already been disposed.
        /// </summary>
        public IAddReference AddWeakReference() => TryAddWeakReference() ?? AddReferenceExtensions.ThrowDisposedTargetException();

        private sealed class WeakReference : IAddReference
        {
            private readonly WeakReference<IReferenceCounter<IDisposable>> _weakReference;

            private WeakReference(IReferenceCounter<IDisposable> referenceCount)
            {
                _weakReference = new(referenceCount);
            }

            public static WeakReference? TryCreate(ReferenceCountedDisposable referenceCountedDisposable)
            {
                _ = referenceCountedDisposable ?? throw new ArgumentNullException(nameof(referenceCountedDisposable));
                IReferenceCounter<IDisposable> referenceCount = null!;
                // Implementation note: TryUpdateContext always "succeeds" in updating the context since the lambda always returns the same instance.
                // The only way this isn't the case is if the reference counted disposable has been disposed.
                if (!referenceCountedDisposable.TryUpdateContext(x => referenceCount = (IReferenceCounter<IDisposable>)x))
                    return null;
                return new(referenceCount);
            }

            public ReferenceCountedDisposable? TryAddReference()
            {
                if (!_weakReference.TryGetTarget(out var referenceCount))
                    return null;
                if (!referenceCount.TryIncrementCount())
                    return null;
                return new(referenceCount);
            }
        }
    }
}
