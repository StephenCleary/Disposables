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
            referenceCount.DecrementCount();
        }

        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public ReferenceCountedDisposable? TryAddReference()
        {
            ReferenceCount referenceCount = null!;
            // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
            // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
            if (!TryUpdateContext(x => referenceCount = ((ReferenceCount)x).IncrementCount()))
                return null;
            return new ReferenceCountedDisposable(referenceCount);
        }

        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static ReferenceCountedDisposable Create(IDisposable? disposable) => new (disposable);

        /// <summary>
        /// Adds an uncounted reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public IAddReference? TryAddUncountedReference() => UncountedReference.TryCreate(this);

        /// <summary>
        /// Adds an uncounted reference to this reference counted disposable. Throws an exception if the underlying disposable has already been disposed.
        /// </summary>
        public IAddReference AddUncountedReference() => TryAddUncountedReference() ?? AddReferenceExtensions.ThrowDisposedTargetException();

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public IAddReference? TryAddWeakReference() => WeakReference.TryCreate(this);

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. Throws an exception if the underlying disposable has already been disposed.
        /// </summary>
        public IAddReference AddWeakReference() => TryAddWeakReference() ?? AddReferenceExtensions.ThrowDisposedTargetException();

        private sealed class ReferenceCount
        {
            private readonly IDisposable? _disposable;
            private int _count;

            public ReferenceCount(IDisposable? disposable)
                : this(disposable, 1)
            {
            }

            private ReferenceCount(IDisposable? disposable, int count)
            {
                _disposable = disposable;
                _count = count;
            }

            public ReferenceCount IncrementCount() =>
                TryIncrementCount() ?? throw new InvalidOperationException($"Internal error during {nameof(IncrementCount)} in {nameof(ReferenceCountedDisposable)}");

            public ReferenceCount? TryIncrementCount() => TryUpdate(x => x == 0 ? null : x + 1) == null ? null : this;

            public int? TryDecrementCount() => TryUpdate(x => x == 0 ? null : x - 1);

            public void DecrementCount()
            {
                var result = TryDecrementCount();
                if (result == null)
                    throw new InvalidOperationException($"Internal error during {nameof(DecrementCount)} in {nameof(ReferenceCountedDisposable)}");
                if (result == 0)
                    _disposable?.Dispose();
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

        private sealed class UncountedReference : IAddReference
        {
            private readonly ReferenceCount _referenceCount;

            private UncountedReference(ReferenceCount referenceCount)
            {
                _referenceCount = referenceCount;
            }

            public static UncountedReference? TryCreate(ReferenceCountedDisposable referenceCountedDisposable)
            {
                _ = referenceCountedDisposable ?? throw new ArgumentNullException(nameof(referenceCountedDisposable));
                ReferenceCount referenceCount = null!;
                // Implementation note: TryUpdateContext always "succeeds" in updating the context since the lambda always returns the same instance.
                // The only way this isn't the case is if the reference counted disposable has been disposed.
                if (!referenceCountedDisposable.TryUpdateContext(x => referenceCount = (ReferenceCount)x))
                    return null;
                return new(referenceCount);
            }

            // TODO: we want to allow incrementing this to change an uncounted reference into a reference counted disposable,
            //  but that can't be safely done using Interlocked. Perhaps if we used `lock`. Or perhaps our BoundAction needs another primitive operation.
            // Or perhaps the ReferenceCount itself should be a SingleDisposable<int>?

            public ReferenceCountedDisposable? TryAddReference()
            {
                if (_referenceCount.TryIncrementCount() == null)
                    return null;
                return new(_referenceCount);
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
                if (referenceCount.TryIncrementCount() == null)
                    return null;
                return new(referenceCount);
            }
        }
    }
}
