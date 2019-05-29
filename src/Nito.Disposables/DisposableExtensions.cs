#if NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nito.Disposables
{
    /// <summary>
    /// Extension methods for disposables.
    /// </summary>
    public static class DisposableExtensions
    {
        /// <summary>
        /// Treats the synchronous disposable as an asynchronous disposable. The asynchronous disposal will actually run synchronously.
        /// </summary>
        /// <param name="this">The synchronous disposable.</param>
        public static IAsyncDisposable ToAsyncDisposable(this IDisposable @this) => new AsyncDisposableWrapper(@this);

        private sealed class AsyncDisposableWrapper : IAsyncDisposable
        {
            private readonly IDisposable _disposable;

            public AsyncDisposableWrapper(IDisposable disposable) => _disposable = disposable;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            public async ValueTask DisposeAsync() => _disposable.Dispose();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }
    }
}
#endif