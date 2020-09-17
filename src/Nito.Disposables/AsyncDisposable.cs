#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nito.Disposables
{
    /// <summary>
    /// A disposable that executes a delegate when disposed.
    /// </summary>
    public sealed class AsyncDisposable : SingleAsyncDisposable<Func<ValueTask>>
    {
        private readonly AsyncDisposeFlags _flags;

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public AsyncDisposable(Func<ValueTask>? dispose)
            : this(dispose, AsyncDisposeFlags.ExecuteSerially)
        {
        }

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        /// <param name="flags">Flags that control how asynchronous disposal is handled.</param>
        public AsyncDisposable(Func<ValueTask>? dispose, AsyncDisposeFlags flags)
            : base(dispose!)
        {
            _flags = flags;
        }

        /// <inheritdoc />
        protected override ValueTask DisposeAsync(Func<ValueTask>? context)
        {
            if (context == null)
                return new ValueTask();

            var handlers = context.GetInvocationList();
            if (handlers.Length == 1)
                return context();

            return DoDisposeAsync(handlers);
        }

        private async ValueTask DoDisposeAsync(IReadOnlyList<Delegate> handlers)
        {
            if ((_flags & AsyncDisposeFlags.ExecuteConcurrently) != AsyncDisposeFlags.ExecuteConcurrently)
            {
                foreach (var handler in handlers)
                    await ((Func<ValueTask>) handler)().ConfigureAwait(false);
            }
            else
            {
                var tasks = handlers.Select(handler => ((Func<ValueTask>) handler)().AsTask()).ToList();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds a delegate to be executed when this instance is disposed. If this instance is already disposed or disposing, then <paramref name="dispose"/> is executed immediately.
        /// If this method is called multiple times concurrently at the same time this instance is disposed, then the different <paramref name="dispose"/> arguments may be disposed concurrently, even if <see cref="AsyncDisposeFlags.ExecuteSerially"/> was specified.
        /// </summary>
        /// <param name="dispose">The delegate to add. May be <c>null</c> to indicate no additional action.</param>
        public async ValueTask AddAsync(Func<ValueTask>? dispose)
        {
            if (dispose == null)
                return;
            if (TryUpdateContext(x => x + dispose))
                return;

            // If we are executing serially, wait for our disposal to complete; then call the additional delegate.
            if ((_flags & AsyncDisposeFlags.ExecuteConcurrently) != AsyncDisposeFlags.ExecuteConcurrently)
                await DisposeAsync().ConfigureAwait(false);
            await dispose().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static AsyncDisposable Create(Func<ValueTask>? dispose) => new AsyncDisposable(dispose);
    }
}
#endif