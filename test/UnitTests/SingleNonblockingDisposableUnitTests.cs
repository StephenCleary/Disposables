using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class SingleNonblockingDisposableUnitTests
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
        public void TryUpdateContext_AfterDispose_ReturnsFalse()
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
        public async Task DisposeIsNonblocking()
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

            await Task.Run(() => disposable.Dispose());

            signal.Set();
            await task1;
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

            Assert.False(disposable.IsDisposed);

            var task1 = Task.Run(() => disposable.Dispose());
            ready.Wait();

            // Note: IsDisposed is true once disposal starts.
            Assert.True(disposable.IsDisposed);

            signal.Set();
            await task1;

            Assert.True(disposable.IsDisposed);
        }

        private sealed class DelegateSingleDisposable<T> : SingleNonblockingDisposable<T>
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
