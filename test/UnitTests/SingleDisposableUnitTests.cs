using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class SingleDisposableUnitTests
    {
        [Fact]
        public void Dispose_ConstructedWithContext_ReceivesThatContext()
        {
            var providedContext = new object();
            object seenContext = null;
            var disposable = new DelegateSingleDisposable<object>(providedContext, context => { seenContext = context; });
            disposable.Dispose();
            Assert.Same(providedContext, seenContext);
        }

        [Fact]
        public void Dispose_UpdatedContext_ReceivesUpdatedContext()
        {
            var originalContext = new object();
            var updatedContext = new object();
            object contextPassedToDispose = null;
            object contextPassedToTryUpdateContextDelegate = null;
            var disposable = new DelegateSingleDisposable<object>(originalContext, context => { contextPassedToDispose = context; });
            Assert.True(disposable.TryUpdateContext(context => { contextPassedToTryUpdateContextDelegate = context; return updatedContext; }));
            disposable.Dispose();
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
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = new DelegateSingleDisposable<object>(originalContext, context =>
            {
                ready.Set();
                signal.Wait();
                contextPassedToDispose = context;
            });
            var task = Task.Run(() => disposable.Dispose());
            ready.Wait();
            Assert.False(disposable.TryUpdateContext(context => { tryUpdateContextDelegateCalled = true; return updatedContext; }));
            signal.Set();
            await task;
            Assert.False(tryUpdateContextDelegateCalled);
            Assert.Same(originalContext, contextPassedToDispose);
        }

        [Fact]
        public void TryUpdateContext_AfterDisposeCompletes_ReturnsFalse()
        {
            var originalContext = new object();
            var updatedContext = new object();
            object contextPassedToDispose = null;
            bool tryUpdateContextDelegateCalled = false;
            var disposable = new DelegateSingleDisposable<object>(originalContext, context => { contextPassedToDispose = context; });
            disposable.Dispose();
            Assert.False(disposable.TryUpdateContext(context => { tryUpdateContextDelegateCalled = true; return updatedContext; }));
            Assert.False(tryUpdateContextDelegateCalled);
            Assert.Same(originalContext, contextPassedToDispose);
        }

        [Fact]
        public void DisposeOnlyCalledOnce()
        {
            var counter = 0;
            var disposable = new DelegateSingleDisposable<object>(new object(), _ => { ++counter; });
            disposable.Dispose();
            disposable.Dispose();
            Assert.Equal(1, counter);
        }

        [Fact]
        public async Task DisposableWaitsForDisposeToComplete()
        {
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = new DelegateSingleDisposable<object>(new object(), _ =>
            {
                ready.Set();
                signal.Wait();
            });

            var task1 = Task.Run(() => disposable.Dispose());
            ready.Wait();

            var task2 = Task.Run(() => disposable.Dispose());
            var timer = Task.Delay(500);
            Assert.Same(timer, await Task.WhenAny(task1, task2, timer));

            signal.Set();
            await task1;
            await task2;
        }

        [Fact]
        public async Task LifetimeProperties_HaveAppropriateValues()
        {
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();

            var disposable = new DelegateSingleDisposable<object>(new object(), _ =>
            {
                ready.Set();
                signal.Wait();
            });

            Assert.False(disposable.IsDisposing);
            Assert.False(disposable.IsDisposeStarted);
            Assert.False(disposable.IsDisposed);

            var task1 = Task.Run(() => disposable.Dispose());
            ready.Wait();

            Assert.True(disposable.IsDisposing);
            Assert.True(disposable.IsDisposeStarted);
            Assert.False(disposable.IsDisposed);

            signal.Set();
            await task1;

            Assert.False(disposable.IsDisposing);
            Assert.True(disposable.IsDisposeStarted);
            Assert.True(disposable.IsDisposed);
        }

        private sealed class DelegateSingleDisposable<T> : SingleDisposable<T>
            where T : class
        {
            private readonly Action<T> _callback;

            public DelegateSingleDisposable(T context, Action<T> callback)
                : base(context)
            {
                _callback = callback;
            }

            public new bool TryUpdateContext(Func<T, T> updater)
            {
                return base.TryUpdateContext(updater);
            }

            protected override void Dispose(T context)
            {
                _callback(context);
            }
        }
    }
}
