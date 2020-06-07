#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

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
        /// <param name="disposables">The disposables to dispose.</param>
        public CollectionAsyncDisposable(params IAsyncDisposable[] disposables)
            : this(disposables, AsyncDisposeFlags.ExecuteConcurrently)
        {
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public CollectionAsyncDisposable(IEnumerable<IAsyncDisposable> disposables)
            : this(disposables, AsyncDisposeFlags.ExecuteConcurrently)
        {
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        /// <param name="flags">Flags that control how asynchronous disposal is handled.</param>
        public CollectionAsyncDisposable(IEnumerable<IAsyncDisposable> disposables, AsyncDisposeFlags flags)
            : base(ImmutableQueue.CreateRange(disposables))
        {
            _flags = flags;
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsync(ImmutableQueue<IAsyncDisposable> context)
        {
            if ((_flags & AsyncDisposeFlags.ExecuteSerially) == AsyncDisposeFlags.ExecuteSerially)
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
        /// </summary>
        /// <param name="disposable">The disposable to add to our collection.</param>
        public ValueTask AddAsync(IAsyncDisposable disposable)
        {
            _ = disposable ?? throw new ArgumentNullException(nameof(disposable));
            if (TryUpdateContext(x => x.Enqueue(disposable)))
                return new ValueTask();
            return disposable.DisposeAsync();
        }

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public static CollectionDisposable Create(params IDisposable[] disposables) => new CollectionDisposable(disposables);

        /// <summary>
        /// Creates a disposable that disposes a collection of disposables.
        /// </summary>
        /// <param name="disposables">The disposables to dispose.</param>
        public static CollectionDisposable Create(IEnumerable<IDisposable> disposables) => new CollectionDisposable(disposables);
    }
}
#endif