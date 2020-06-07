using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, and entries may not be <c>null</c>.</param>
        public CollectionDisposable(params IDisposable[] disposables)
            : this((IEnumerable<IDisposable>)disposables)
        {
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, and entries may not be <c>null</c>.</param>
        public CollectionDisposable(IEnumerable<IDisposable> disposables)
            : base(ImmutableQueue.CreateRange(disposables))
        {
        }

        /// <inheritdoc />
        protected override void Dispose(ImmutableQueue<IDisposable> context)
        {
            foreach (var disposable in context)
                disposable.Dispose();
        }

        /// <summary>
        /// Adds a disposable to the collection of disposables. If this instance is already disposed or disposing, then <paramref name="disposable"/> is disposed immediately.
        /// </summary>
        /// <param name="disposable">The disposable to add to our collection. May not be <c>null</c>.</param>
        public void Add(IDisposable disposable)
        {
            _ = disposable ?? throw new ArgumentNullException(nameof(disposable));
            if (TryUpdateContext(x => x.Enqueue(disposable)))
                return;

            // Wait for our disposal to complete; then dispose the additional item.
            Dispose();
            disposable.Dispose(); // TODO: ensure these are serial as well.
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, and entries may not be <c>null</c>.</param>
        public static CollectionDisposable Create(params IDisposable[] disposables) => new CollectionDisposable(disposables);

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, and entries may not be <c>null</c>.</param>
        public static CollectionDisposable Create(IEnumerable<IDisposable> disposables) => new CollectionDisposable(disposables);
    }
}
