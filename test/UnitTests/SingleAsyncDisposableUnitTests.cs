using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace UnitTests
{
    public class SingleAsyncDisposableUnitTests
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
        public async Task TryUpdateContext_AfterDisposeStarts_ReturnsFalse()
        {
            var originalContext = new object();
            var updatedContext = new object();
            object contextPassedToDispose = null;
            bool tryUpdateContextDelegateCalled = false;
            var ready = new TaskCompletionSource<object>();
            var signal = new TaskCompletionSource<object>();
            var disposable = new DelegateSingleDisposable<object>(originalContext, async context =>
            {
                ready.TrySetResult(null);
                await signal.Task;
                contextPassedToDispose = context;
            });
            var task = Task.Run(async () => await disposable.DisposeAsync());
            await ready.Task;
            Assert.False(disposable.TryUpdateContext(context => { tryUpdateContextDelegateCalled = true; return updatedContext; }));
            signal.TrySetResult(null);
            await task;
            Assert.False(tryUpdateContextDelegateCalled);
            Assert.Same(originalContext, contextPassedToDispose);
        }

        [Fact]
        public async Task TryUpdateContext_AfterDisposeCompletes_ReturnsFalse()
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
        public async Task DisposableWaitsForDisposeToComplete()
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

            var task2 = Task.Run(async () => await disposable.DisposeAsync());
            var timer = Task.Delay(500);
            Assert.Same(timer, await Task.WhenAny(task1, task2, timer));

            signal.TrySetResult(null);
            await task1;
            await task2;
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

            Assert.False(disposable.IsDisposing);
            Assert.False(disposable.IsDisposeStarted);
            Assert.False(disposable.IsDisposed);

            var task1 = Task.Run(async () => await disposable.DisposeAsync());
            await ready.Task;

            Assert.True(disposable.IsDisposing);
            Assert.True(disposable.IsDisposeStarted);
            Assert.False(disposable.IsDisposed);

            signal.TrySetResult(null);
            await task1;

            Assert.False(disposable.IsDisposing);
            Assert.True(disposable.IsDisposeStarted);
            Assert.True(disposable.IsDisposed);
        }

        private sealed class DelegateSingleDisposable<T> : SingleAsyncDisposable<T>
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
