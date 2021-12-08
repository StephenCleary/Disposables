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
        public static IReferenceCountedDisposable<T> AddReference<T>(this IReferenceCounterReference<T> addReference)
            where T : class, IDisposable
        {
            _ = addReference ?? throw new ArgumentNullException(nameof(addReference));
            return addReference.TryAddReference() ?? ThrowDisposedTargetException<IReferenceCountedDisposable<T>>();
        }

        internal static T ThrowDisposedTargetException<T>() => throw new InvalidOperationException("AddReference called for a disposed target.");
    }
}
