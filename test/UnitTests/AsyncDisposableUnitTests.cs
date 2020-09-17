using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace UnitTests
{
    public class AsyncDisposableUnitTests
    {
        [Fact]
        public async Task Dispose_NullAction_DoesNotThrow()
        {
            var disposable = AsyncDisposable.Create(null);
            await disposable.DisposeAsync();
        }

        [Fact]
        public async Task Dispose_InvokesAction()
        {
            bool actionInvoked = false;
            var disposable = AsyncDisposable.Create(async () => { actionInvoked = true; });
            await disposable.DisposeAsync();
            Assert.True(actionInvoked);
        }

        [Fact]
        public async Task Dispose_AfterAdd_InvokesBothActions()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = AsyncDisposable.Create(async () => { action1Invoked = true; });
            await disposable.AddAsync(async () => { action2Invoked = true; });
            await disposable.DisposeAsync();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }

        [Fact]
        public async Task Dispose_AfterAddingNull_DoesNotThrow()
        {
            bool action1Invoked = false;
            var disposable = AsyncDisposable.Create(async () => { action1Invoked = true; });
            await disposable.AddAsync(null);
            await disposable.DisposeAsync();
            Assert.True(action1Invoked);
        }

        [Fact]
        public async Task Add_AfterDisposeStarts_ExecutingConcurrently_InvokesActionImmediately()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var ready = new TaskCompletionSource<object>();
            var signal = new TaskCompletionSource<object>();
            var disposable = new AsyncDisposable(async () =>
            {
                ready.TrySetResult(null);
                await signal.Task;
                action1Invoked = true;
            }, AsyncDisposeFlags.ExecuteConcurrently);
            var task = Task.Run(async () => await disposable.DisposeAsync());
            await ready.Task;
            await disposable.AddAsync(async () => { action2Invoked = true; });
            Assert.False(action1Invoked);
            Assert.True(action2Invoked);
            signal.TrySetResult(null);
            await task;
            Assert.True(action1Invoked);
        }

        [Fact]
        public async Task Add_AfterDisposeStarts_ExecutingInSerial_InvokesActionAfterDisposeCompletes()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var ready = new TaskCompletionSource<object>();
            var signal = new TaskCompletionSource<object>();
            var disposable = AsyncDisposable.Create(async () =>
            {
                action1Invoked = true;
                ready.TrySetResult(null);
                await signal.Task;
            });
            var disposeTask = Task.Run(async () => await disposable.DisposeAsync());
            await ready.Task;
            var addTask = Task.Run(async () => await disposable.AddAsync(async () => { action2Invoked = true; }));
            Assert.NotEqual(addTask, await Task.WhenAny(addTask, Task.Delay(100)));
            Assert.True(action1Invoked);
            Assert.False(action2Invoked);
            signal.TrySetResult(null);
            await disposeTask;
            await addTask;
            Assert.True(action2Invoked);
        }

        [Fact]
        public async Task Actions_ExecutingInSerial_ExecuteInSerial()
        {
            bool running = false;
            var disposable = new AsyncDisposable(null);
            for (int i = 0; i != 10; ++i)
            {
                await disposable.AddAsync(async () =>
                {
                    Assert.False(running);
                    running = true;
                    await Task.Delay(10);
                    running = false;
                });
            }

            await disposable.DisposeAsync();
        }

        [Fact]
        public async Task MultipleDispose_OnlyInvokesActionOnce()
        {
            var counter = 0;
            var disposable = AsyncDisposable.Create(async () => { ++counter; });
            await disposable.DisposeAsync();
            await disposable.DisposeAsync();
            Assert.Equal(1, counter);
        }
    }
}
