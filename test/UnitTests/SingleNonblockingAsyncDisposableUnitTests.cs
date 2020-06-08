using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace UnitTests
{
    public class SingleNonblockingAsyncDisposableUnitTests
    {
        [Fact]
        public async Task Dispose_ConstructedWithContext_ReceivesThatContext()
        {
            var providedContext = new object();
            object seenContext = null;
            var disposable = new DelegateSingleDisposable<object>(providedContext, async context => { seenContext = context; });
            await disposable.DisposeAsync();
            Assert.Same(providedContext, seenContext);
        }

        [Fact]
        public async Task Dispose_UpdatedContext_ReceivesUpdatedContext()
        {
            var originalContext = new object();
            var updatedContext = new object();
            object contextPassedToDispose = null;
            object contextPassedToTryUpdateContextDelegate = null;
            var disposable = new DelegateSingleDisposable<object>(originalContext, async context => { contextPassedToDispose = context; });
            Assert.True(disposable.TryUpdateContext(context => { contextPassedToTryUpdateContextDelegate = context; return updatedContext; }));
            await disposable.DisposeAsync();
            Assert.Same(originalContext, contextPassedToTryUpdateContextDelegate);
            Assert.Same(updatedContext, contextPassedToDispose);
        }

        [Fact]
        public async Task TryUpdateContext_AfterDispose_ReturnsFalse()
        {
            var originalContext = new object();
            var updatedContext = new object();
            object contextPassedToDispose = null;
            bool tryUpdateContextDelegateCalled = false;
            var disposable = new DelegateSingleDisposable<object>(originalContext, async context => { contextPassedToDispose = context; });
            await disposable.DisposeAsync();
            Assert.False(disposable.TryUpdateContext(context => { tryUpdateContextDelegateCalled = true; return updatedContext; }));
            Assert.False(tryUpdateContextDelegateCalled);
            Assert.Same(originalContext, contextPassedToDispose);
        }

        [Fact]
        public async Task DisposeOnlyCalledOnce()
        {
            var counter = 0;
            var disposable = new DelegateSingleDisposable<object>(new object(), async _ => { ++counter; });
            await disposable.DisposeAsync();
            await disposable.DisposeAsync();
            Assert.Equal(1, counter);
        }

        [Fact]
        public async Task DisposeIsNonblocking()
        {
            var ready = new TaskCompletionSource<object>();
            var signal = new TaskCompletionSource<object>();
            var disposable = new DelegateSingleDisposable<object>(new object(), async _ =>
            {
                ready.TrySetResult(null);
                await signal.Task;
            });

            var task1 = Task.Run(async () => await disposable.DisposeAsync());
            await ready.Task;

            await Task.Run(async () => await disposable.DisposeAsync());

            signal.TrySetResult(null);
            await task1;
        }

        [Fact]
        public async Task LifetimeProperties_HaveAppropriateValues()
        {
            var ready = new TaskCompletionSource<object>();
            var signal = new TaskCompletionSource<object>();
            var disposable = new DelegateSingleDisposable<object>(new object(), async _ =>
            {
                ready.TrySetResult(null);
                await signal.Task;
            });

            Assert.False(disposable.IsDisposed);

            var task1 = Task.Run(async () => await disposable.DisposeAsync());
            await ready.Task;

            // Note: IsDisposed is true once disposal starts.
            Assert.True(disposable.IsDisposed);

            signal.TrySetResult(null);
            await task1;

            Assert.True(disposable.IsDisposed);
        }

        private sealed class DelegateSingleDisposable<T> : SingleNonblockingAsyncDisposable<T>
            where T : class
        {
            private readonly Func<T, Task> _callback;

            public DelegateSingleDisposable(T context, Func<T, Task> callback)
                : base(context)
            {
                _callback = callback;
            }

            public new bool TryUpdateContext(Func<T, T> updater)
            {
                return base.TryUpdateContext(updater);
            }

            protected override async ValueTask DisposeAsync(T context)
            {
                await _callback(context);
            }
        }
    }
}
