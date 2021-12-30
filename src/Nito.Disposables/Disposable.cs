using Nito.Disposables.Advanced;
using System;

namespace Nito.Disposables
{
    /// <summary>
    /// A disposable that executes a delegate when disposed.
    /// </summary>
    public sealed class Disposable : IDisposable, IDisposableProperties
    {
        private readonly SingleDisposable<Action?> _singleDisposable;

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public Disposable(Action? dispose)
        {
            _singleDisposable = new(dispose, context => context?.Invoke());
        }

        /// <inheritdoc/>
        public bool IsDisposeStarted => _singleDisposable.IsDisposeStarted;

        /// <inheritdoc/>
        public bool IsDisposed => _singleDisposable.IsDisposed;

        /// <inheritdoc/>
        public bool IsDisposing => _singleDisposable.IsDisposing;

        /// <inheritdoc/>
        public void Dispose() => _singleDisposable.Dispose();

        /// <summary>
        /// Adds a delegate to be executed when this instance is disposed. If this instance is already disposed or disposing, then <paramref name="dispose"/> is executed immediately.
        /// If this method is called multiple times concurrently at the same time this instance is disposed, then the different <paramref name="dispose"/> arguments may be disposed concurrently.
        /// </summary>
        /// <param name="dispose">The delegate to add. May be <c>null</c> to indicate no additional action.</param>
        public void Add(Action? dispose)
        {
            if (dispose == null)
                return;
            if (_singleDisposable.TryUpdateContext(x => x + dispose))
                return;

            // Wait for our disposal to complete; then call the additional delegate.
            _singleDisposable.Dispose();
            dispose();
        }

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static Disposable Create(Action? dispose) => new(dispose);
    }
}
