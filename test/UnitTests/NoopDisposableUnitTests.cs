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
    }
}
