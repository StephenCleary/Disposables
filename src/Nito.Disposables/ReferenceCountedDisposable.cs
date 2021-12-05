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
            : this(new ReferenceCount(disposable))
        {
        }

        private ReferenceCountedDisposable(ReferenceCount referenceCount)
            : base(referenceCount)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(object context)
        {
            var referenceCount = (ReferenceCount)context;
            referenceCount.TryDecrementCount()?.Dispose();
        }

        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public ReferenceCountedDisposable? TryAddReference()
        {
            ReferenceCount referenceCount = null!;
            // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
            // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
            if (!TryUpdateContext(x => referenceCount = ((ReferenceCount)x).TryIncrementCount() ? (ReferenceCount)x : null!))
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
            IReferenceCount? TryFindAndIncrementReferenceCount(IDisposable disposable);
        }

        private sealed class NewReferenceCountStrategy : IReferenceCountStrategy
        {
            public IReferenceCount? TryFindAndIncrementReferenceCount(IDisposable disposable) => new ReferenceCount(disposable);
        }

        private sealed class SelfReferenceCountStrategy : IReferenceCountStrategy
        {
            public IReferenceCount? TryFindAndIncrementReferenceCount(IDisposable disposable) => disposable as IReferenceCount;
        }

        //private sealed class AttachedReferenceCountStrategy : IReferenceCountStrategy
        //{
        //    public IReferenceCount? TryFindAndIncrementReferenceCount(IDisposable disposable) => TODO;
        //}

        /// <summary>
        /// A reference count for an underlying disposable.
        /// </summary>
        private interface IReferenceCount
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

        private sealed class ReferenceCount : IReferenceCount
        {
            private IDisposable? _disposable;
            private int _count;

            public ReferenceCount(IDisposable? disposable)
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
            private readonly WeakReference<ReferenceCount> _weakReference;

            private WeakReference(ReferenceCount referenceCount)
            {
                _weakReference = new(referenceCount);
            }

            public static WeakReference? TryCreate(ReferenceCountedDisposable referenceCountedDisposable)
            {
                _ = referenceCountedDisposable ?? throw new ArgumentNullException(nameof(referenceCountedDisposable));
                ReferenceCount referenceCount = null!;
                // Implementation note: TryUpdateContext always "succeeds" in updating the context since the lambda always returns the same instance.
                // The only way this isn't the case is if the reference counted disposable has been disposed.
                if (!referenceCountedDisposable.TryUpdateContext(x => referenceCount = (ReferenceCount)x))
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
