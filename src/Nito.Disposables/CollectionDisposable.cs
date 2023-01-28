using Nito.Disposables.Advanced;
using Nito.Disposables.Internals;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Nito.Disposables
{
    /// <summary>
    /// Disposes a collection of disposables.
    /// </summary>
    public sealed class CollectionDisposable : IDisposable, IDisposableProperties
    {
        private readonly SingleDisposable<ImmutableQueue<IDisposable>> _singleDisposable;

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
        {
            _singleDisposable = new(ImmutableQueue.CreateRange(disposables.WhereNotNull()), Dispose);

            static void Dispose(ImmutableQueue<IDisposable> context)
            {
                foreach (var disposable in context)
                    disposable?.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool IsDisposeStarted => _singleDisposable.IsDisposeStarted;

        /// <inheritdoc/>
        public bool IsDisposed => _singleDisposable.IsDisposed;

        /// <inheritdoc/>
        public bool IsDisposing => _singleDisposable.IsDisposing;

        /// <inheritdoc/>
        public void Dispose() => _singleDisposable.Dispose();

        /// <summary>
        /// Adds a disposable to the collection of disposables. If this instance is already disposed or disposing, then <paramref name="disposable"/> is disposed immediately.
        /// If this method is called multiple times concurrently at the same time this instance is disposed, then the different <paramref name="disposable"/> arguments may be disposed concurrently.
        /// </summary>
        /// <param name="disposable">The disposable to add to our collection. May be <c>null</c>.</param>
        public void Add(IDisposable? disposable)
        {
            if (disposable == null)
                return;
            if (_singleDisposable.TryUpdateContext(x => x.Enqueue(disposable)))
                return;

            // Wait for our disposal to complete; then dispose the additional item.
            _singleDisposable.Dispose();
            disposable.Dispose();
        }

        /// <summary>
        /// Makes this disposable do nothing when it is disposed. Returns the actions this disposable *would* have taken; these can be passed to a new instance to transfer ownership.
        /// </summary>
        public IEnumerable<IDisposable> Abandon()
        {
            var result = ImmutableQueue<IDisposable>.Empty;
            var updated = _singleDisposable.TryUpdateContext(x =>
            {
                result = x;
                return ImmutableQueue<IDisposable>.Empty;
            });
            if (!updated)
                result = ImmutableQueue<IDisposable>.Empty;
            return result;
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public static CollectionDisposable Create(params IDisposable?[] disposables) => new(disposables);

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public static CollectionDisposable Create(IEnumerable<IDisposable?> disposables) => new(disposables);
    }
}
