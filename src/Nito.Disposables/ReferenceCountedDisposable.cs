using System;
using System.Threading;

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
            : this(new ReferenceCounter(disposable))
        {
        }

        private ReferenceCountedDisposable(ReferenceCounter referenceCount)
            : base(referenceCount)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(object context)
        {
            var referenceCount = (ReferenceCounter)context;
            referenceCount.TryDecrementCount()?.Dispose();
        }

        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public ReferenceCountedDisposable? TryAddReference()
        {
            ReferenceCounter referenceCount = null!;
            // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
            // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
            if (!TryUpdateContext(x => referenceCount = ((ReferenceCounter)x).TryIncrementCount() ? (ReferenceCounter)x : null!))
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

        private interface IReferenceCountStrategy
        {
            IReferenceCounter? TryFindAndIncrementReferenceCount(IDisposable disposable);
        }

        private sealed class NewReferenceCountStrategy : IReferenceCountStrategy
        {
            public IReferenceCounter? TryFindAndIncrementReferenceCount(IDisposable disposable) => new ReferenceCounter(disposable);
        }

        private sealed class SelfReferenceCountStrategy : IReferenceCountStrategy
        {
            public IReferenceCounter? TryFindAndIncrementReferenceCount(IDisposable disposable) => disposable as IReferenceCounter;
        }

        //private sealed class AttachedReferenceCountStrategy : IReferenceCountStrategy
        //{
        //    public IReferenceCount? TryFindAndIncrementReferenceCount(IDisposable disposable) => TODO;
        //}

        /// <summary>
        /// A reference count for an underlying disposable.
        /// </summary>
        private interface IReferenceCounter
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

        private sealed class ReferenceCounter : IReferenceCounter
        {
            private IDisposable? _disposable;
            private int _count;

            public ReferenceCounter(IDisposable? disposable)
            {
                _disposable = disposable;
                _count = 1;
            }

            public bool TryIncrementCount() => TryUpdate(x => x == 0 ? null : x + 1) != null;

            public IDisposable? TryDecrementCount()
            {
                var updateResult = TryUpdate(x => x == 0 ? null : x - 1);
                if (updateResult != 0)
                    return null;
                return Interlocked.Exchange(ref _disposable, null);
            }

            public IDisposable? TryGetTarget()
            {
                var result = Interlocked.CompareExchange(ref _disposable, null, null);
                var count = Interlocked.CompareExchange(ref _count, 0, 0);
                if (count == 0)
                    return null;
                return result;
            }

            private int? TryUpdate(Func<int, int?> func)
            {
                while (true)
                {
                    var original = Interlocked.CompareExchange(ref _count, 0, 0);
                    if (original == 0)
                        return null;
                    var updatedCount = func(original);
                    if (updatedCount == null)
                        return null;
                    var result = Interlocked.CompareExchange(ref _count, updatedCount.Value, original);
                    if (original == result)
                        return updatedCount.Value;
                }
            }
        }

        private sealed class WeakReference : IAddReference
        {
            private readonly WeakReference<ReferenceCounter> _weakReference;

            private WeakReference(ReferenceCounter referenceCount)
            {
                _weakReference = new(referenceCount);
            }

            public static WeakReference? TryCreate(ReferenceCountedDisposable referenceCountedDisposable)
            {
                _ = referenceCountedDisposable ?? throw new ArgumentNullException(nameof(referenceCountedDisposable));
                ReferenceCounter referenceCount = null!;
                // Implementation note: TryUpdateContext always "succeeds" in updating the context since the lambda always returns the same instance.
                // The only way this isn't the case is if the reference counted disposable has been disposed.
                if (!referenceCountedDisposable.TryUpdateContext(x => referenceCount = (ReferenceCounter)x))
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
