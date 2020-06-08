using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using Xunit;

namespace UnitTests
{
    public class NoopDisposableUnitTests
    {
        [Fact]
        public void Instance_IsSingleton()
        {
            Assert.Same(NoopDisposable.Instance, NoopDisposable.Instance);
        }

        [Fact]
        public void Dispose_MultipleTimes_DoesNothing()
        {
            NoopDisposable.Instance.Dispose();
            NoopDisposable.Instance.Dispose();
        }

        [Fact]
        public async Task DisposeAsync_MultipleTimes_DoesNothing()
        {
            await NoopDisposable.Instance.DisposeAsync();
            await NoopDisposable.Instance.DisposeAsync();
        }
    }
}
