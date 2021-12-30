using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables.Advanced
{
    /// <summary>
    /// Useful properties for disposable types.
    /// </summary>
    public interface IDisposableProperties
    {
        /// <summary>
        /// Whether this instance is currently disposing or has been disposed.
        /// </summary>
        public bool IsDisposeStarted { get; }

        /// <summary>
        /// Whether this instance is disposed (finished disposing).
        /// </summary>
        public bool IsDisposed { get; }

        /// <summary>
        /// Whether this instance is currently disposing, but not finished yet.
        /// </summary>
        public bool IsDisposing { get; }
    }
}
