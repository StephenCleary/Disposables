using System.Threading.Tasks;
using Nito.Disposables;
using System.Threading;
using Xunit;
using System.Collections.Generic;

namespace UnitTests;

public class DisposableUnitTests
{
    [Fact]
    public void Dispose_NullAction_DoesNotThrow()
    {
        var disposable = Disposable.Create(null);
        disposable.Dispose();
    }

    [Fact]
    public void Dispose_InvokesAction()
    {
        bool actionInvoked = false;
        var disposable = Disposable.Create(() => { actionInvoked = true; });
        disposable.Dispose();
        Assert.True(actionInvoked);
    }

    [Fact]
    public void Dispose_AfterAdd_InvokesBothActions()
    {
        bool action1Invoked = false;
        bool action2Invoked = false;
        var disposable = Disposable.Create(() => { action1Invoked = true; });
        disposable.Add(() => { action2Invoked = true; });
        disposable.Dispose();
        Assert.True(action1Invoked);
        Assert.True(action2Invoked);
    }

    [Fact]
    public void Dispose_AfterAdd_InvokesBothActionsInInverseOrder()
    {
        var results = new List<int>();
        var disposable = Disposable.Create(() => { results.Add(0); });
        disposable.Add(() => { results.Add(1); });
        disposable.Dispose();
        Assert.Equal(new[] { 1, 0 }, results);
    }

    [Fact]
    public void Dispose_AfterAddingNull_DoesNotThrow()
    {
        bool action1Invoked = false;
        var disposable = Disposable.Create(() => { action1Invoked = true; });
        disposable.Add(null);
        disposable.Dispose();
        Assert.True(action1Invoked);
    }

    [Fact]
    public async Task Add_AfterDisposeStarts_InvokesActionAfterDisposeCompletes()
    {
        bool action1Invoked = false;
        bool action2Invoked = false;
        var ready = new ManualResetEventSlim();
        var signal = new ManualResetEventSlim();
        var disposable = Disposable.Create(() =>
        {
            action1Invoked = true;
            ready.Set();
            signal.Wait();
        });
        var disposeTask = Task.Run(() => disposable.Dispose());
        ready.Wait();
        var addTask = Task.Run(() => disposable.Add(() => { action2Invoked = true; }));
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(addTask.Wait(100));
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Assert.True(action1Invoked);
        Assert.False(action2Invoked);
        signal.Set();
        await disposeTask;
        await addTask;
        Assert.True(action2Invoked);
    }

    [Fact]
    public void MultipleDispose_OnlyInvokesActionOnce()
    {
        var counter = 0;
        var disposable = Disposable.Create(() => { ++counter; });
        disposable.Dispose();
        disposable.Dispose();
        Assert.Equal(1, counter);
    }

    [Fact]
    public void Abandon_DoesNotInvokeAction()
    {
        bool actionInvoked = false;
        var disposable = Disposable.Create(() => { actionInvoked = true; });
        disposable.Abandon();
        disposable.Dispose();
        Assert.False(actionInvoked);
    }

    [Fact]
    public void AbandonWithConstruction_TransfersOwnership()
    {
        bool actionInvoked = false;
        var disposable = Disposable.Create(() => { actionInvoked = true; });
        var disposable2 = Disposable.Create(disposable.Abandon());
        disposable2.Dispose();
        Assert.True(actionInvoked);
    }

    [Fact]
    public void AbandonWithConstruction_AfterAdd_InvokesBothActionsInInverseOrder()
    {
        var results = new List<int>();
        var disposable = Disposable.Create(() => { results.Add(0); });
        disposable.Add(() => { results.Add(1); });
        var disposable2 = Disposable.Create(disposable.Abandon());
        disposable2.Dispose();
        Assert.Equal(new[] { 1, 0 }, results);
    }

    [Fact]
    public void Abandon_DoesNotDispose()
    {
        int counter = 0;
        var disposable = Disposable.Create(() => { --counter; });
        disposable.Abandon();
        Assert.False(disposable.IsDisposed);
        disposable.Add(() => { ++counter; });
        disposable.Dispose();
        Assert.Equal(1, counter);
    }
}
