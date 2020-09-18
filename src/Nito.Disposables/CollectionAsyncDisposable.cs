#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// Disposes a collection of disposables.
    /// </summary>
    public sealed class CollectionAsyncDisposable : SingleAsyncDisposable<ImmutableQueue<IAsyncDisposable>>
    {
        private readonly AsyncDisposeFlags _flags;

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public CollectionAsyncDisposable(params IAsyncDisposable?[] disposables)
            : this(disposables, AsyncDisposeFlags.ExecuteSerially)
        {
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public CollectionAsyncDisposable(IEnumerable<IAsyncDisposable?> disposables)
            : this(disposables, AsyncDisposeFlags.ExecuteSerially)
        {
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        /// <param name="flags">Flags that control how asynchronous disposal is handled.</param>
        public CollectionAsyncDisposable(IEnumerable<IAsyncDisposable?> disposables, AsyncDisposeFlags flags)
            : base(ImmutableQueue.CreateRange(disposables.WhereNotNull()))
        {
            _flags = flags;
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsync(ImmutableQueue<IAsyncDisposable> context)
        {
            if ((_flags & AsyncDisposeFlags.ExecuteConcurrently) != AsyncDisposeFlags.ExecuteConcurrently)
            {
                foreach (var disposable in context)
                    await disposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                var tasks = context.Select(disposable => disposable.DisposeAsync().AsTask()).ToList();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds a disposable to the collection of disposables. If this instance is already disposed or disposing, then <paramref name="disposable"/> is disposed immediately.
        /// If this method is called multiple times concurrently at the same time this instance is disposed, then the different <paramref name="disposable"/> arguments may be disposed concurrently, even if <see cref="AsyncDisposeFlags.ExecuteSerially"/> was specified.
        /// </summary>
        /// <param name="disposable">The disposable to add to our collection. May be <c>null</c>.</param>
        public async ValueTask AddAsync(IAsyncDisposable? disposable)
        {
            if (disposable == null)
                return;
            if (TryUpdateContext(x => x.Enqueue(disposable)))
                return;

            // If we are executing serially, wait for our disposal to complete; then dispose the additional item.
            if ((_flags & AsyncDisposeFlags.ExecuteConcurrently) != AsyncDisposeFlags.ExecuteConcurrently)
                await DisposeAsync().ConfigureAwait(false);
            await disposable.DisposeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public static CollectionAsyncDisposable Create(params IAsyncDisposable?[] disposables) => new CollectionAsyncDisposable(disposables);

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose. May not be <c>null</c>, but entries may be <c>null</c>.</param>
        public static CollectionAsyncDisposable Create(IEnumerable<IAsyncDisposable?> disposables) => new CollectionAsyncDisposable(disposables);
    }
}
#endif