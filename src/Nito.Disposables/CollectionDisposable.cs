using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// Disposes a collection of disposables.
    /// </summary>
    public sealed class CollectionDisposable : SingleDisposable<ImmutableQueue<IDisposable>>
    {
        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public CollectionDisposable(params IDisposable?[] disposables)
            : this((IEnumerable<IDisposable?>)disposables)
        {
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public CollectionDisposable(IEnumerable<IDisposable?> disposables)
            : base(ImmutableQueue.CreateRange(disposables.WhereNotNull()))
        {
        }

        /// <inheritdoc />
        protected override void Dispose(ImmutableQueue<IDisposable> context)
        {
            foreach (var disposable in context)
                disposable?.Dispose();
        }

        /// <summary>
        /// Adds a disposable to the collection of disposables. If this instance is already disposed or disposing, then <paramref name="disposable"/> is disposed immediately.
        /// If this method is called multiple times concurrently at the same time this instance is disposed, then the different <paramref name="disposable"/> arguments may be disposed concurrently.
        /// </summary>
        /// <param name="disposable">The disposable to add to our collection. May be <c>null</c>.</param>
        public void Add(IDisposable? disposable)
        {
            if (disposable == null)
                return;
            if (TryUpdateContext(x => x.Enqueue(disposable)))
                return;

            // Wait for our disposal to complete; then dispose the additional item.
            Dispose();
            disposable.Dispose();
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public static CollectionDisposable Create(params IDisposable?[] disposables) => new CollectionDisposable(disposables);

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public static CollectionDisposable Create(IEnumerable<IDisposable?> disposables) => new CollectionDisposable(disposables);
    }
}
