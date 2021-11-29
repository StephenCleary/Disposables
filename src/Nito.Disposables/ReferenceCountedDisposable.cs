using System;
using System.Threading;

namespace Nito.Disposables
{
    /// <summary>
    /// Equivalent to <see cref="Disposable"/>.
    /// </summary>
    public sealed class ReferenceCountedDisposable : SingleDisposable<object>
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
            // Implementation note: IncrementCount always "succeds" in updating the context since it always returns the same instance.
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

            public ReferenceCount IncrementCount()
            {
                if (Interlocked.Increment(ref _count) == 1)
                    throw new InvalidOperationException($"Internal error during {nameof(IncrementCount)} in {nameof(ReferenceCountedDisposable)}");
                return this;
            }

            public void DecrementCount()
            {
                var result = Interlocked.Decrement(ref _count);
                if (result < 0)
                    throw new InvalidOperationException($"Internal error during {nameof(DecrementCount)} in {nameof(ReferenceCountedDisposable)}");
                if (result == 0)
                    _disposable?.Dispose();
            }
        }
    }
}
