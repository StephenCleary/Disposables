using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables
{
    /// <summary>
    /// An object that can add a reference count.
    /// </summary>
    public interface IAddReference
    {
        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public ReferenceCountedDisposable? TryAddReference();
    }
}
