using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// A reference-counted one-time signal.
    /// </summary>
    public sealed class RefCountedSignal
    {
        private int _count;
        private readonly ManualResetEventSlim _mre = new ManualResetEventSlim();

        private void Release()
        {
            lock (_mre)
            {
                --_count;
                if (_count == 0)
                    _mre.Dispose();
            }
        }

        private bool IsDone => Count == 0 && _mre.IsSet;

        // IsDone == count has reached zero *and* MRE has been set.
        // AddRef:
        // 1) If IsDone, return noop MRER.
        // 2) Increment count; return real MRER.

        // when set, replace inner MRE with a noop "MRE", and enable disposing when count reaches zero.

        private void Set()
        {

        }

        /// <summary>
        /// A reference to a signal. When this reference is disposed, its reference count is decremented.
        /// </summary>
        public interface ISignalReference : IDisposable
        {
            /// <summary>
            /// Waits for the signal to be set.
            /// </summary>
            void Wait();

            /// <summary>
            /// Sets the signal.
            /// </summary>
            void Set();
        }

        private sealed class NoopSignalReference : ISignalReference
        {
            private NoopSignalReference() { }

            void ISignalReference.Wait() { }
            void ISignalReference.Set() { }
            void IDisposable.Dispose() { }

            public static NoopSignalReference Instance { get; } = new NoopSignalReference();
        }
    }
}
