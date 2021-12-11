using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class ReferenceCountedDisposableUnitTests
    {
        [Fact]
        public void Create_NullDisposable_DoesNotThrow()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            disposable.Dispose();
        }

        [Fact]
        public void AdvancedCreate_NullDisposable_DoesNotThrow()
        {
            var disposable = ReferenceCountedDisposable.CreateWithNewReferenceCounter<IDisposable>(null);
            disposable.Dispose();
        }

        [Fact]
        public void Target_ReturnsTarget()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            Assert.Equal(target, disposable.Target);
        }

        [Fact]
        public void Target_WhenNull_ReturnsTarget()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            Assert.Null(disposable.Target);
        }

        [Fact]
        public void Target_AfterDispose_Throws()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.Target);
        }

        [Fact]
        public void Target_WhenNull_AfterDispose_Throws()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.Target);
        }

        [Fact]
        public void Dispose_DisposesTarget()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public void MultiDispose_DisposesTargetOnce()
        {
            var targetDisposeCount = 0;
            var target = new UnsafeDisposable(() => ++targetDisposeCount);
            var disposable = ReferenceCountedDisposable.Create(target);
            Assert.Equal(0, targetDisposeCount);
            disposable.Dispose();
            Assert.Equal(1, targetDisposeCount);
            disposable.Dispose();
            Assert.Equal(1, targetDisposeCount);
        }

        [Fact]
        public void AddReference_AfterDispose_Throws()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddReference());
        }

        [Fact]
        public void AddReference_AfterDispose_WhenAnotherReferenceExists_Throws()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var secondDisposable = disposable.AddReference();
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddReference());
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public void Dispose_WhenAnotherReferenceExists_DoesNotDisposeTarget_UntilOtherReferenceIsDisposed()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var secondDisposable = disposable.AddReference();
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
            secondDisposable.Dispose();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public void MultiDispose_OnlyDecrementsReferenceCountOnce()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            _ = disposable.AddReference();
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public void MultiCreate_SameTarget_SharesReferenceCount()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var secondDisposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public void MultiCreate_SameTarget_AfterDisposal_SharesReferenceCount()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            var secondDisposable = ReferenceCountedDisposable.Create(target);

            // TODO: desired semantics???
            _ = secondDisposable.Target;
        }

        private sealed class UnsafeDisposable : IDisposable
        {
            public UnsafeDisposable(Action action) => _action = action;

            public void Dispose() => _action();

            private readonly Action _action;
        }
    }
}
