using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables
{
    /// <summary>
    /// Extension methods for <see cref="IAddReference"/>.
    /// </summary>
    public static class AddReferenceExtensions
    {
        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. Throws an exception if the underlying disposable has already been disposed.
        /// </summary>
        public static ReferenceCountedDisposable AddReference(this IAddReference addReference)
        {
            _ = addReference ?? throw new ArgumentNullException(nameof(addReference));
            return addReference.TryAddReference() ?? ThrowDisposedTargetException();
        }

        internal static ReferenceCountedDisposable ThrowDisposedTargetException() => throw new InvalidOperationException("AddReference called for a disposed target.");
    }
}
