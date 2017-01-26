using System;

namespace Nito.Disposables
{
    /// <summary>
    /// A disposable that executes a delegate when disposed.
    /// </summary>
    public sealed class AnonymousDisposable : SingleDisposable<Action>
    {
        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public AnonymousDisposable(Action dispose)
            : base(dispose)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(Action context) => context?.Invoke();

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. May not be <c>null</c>.</param>
        public static AnonymousDisposable Create(Action dispose) => new AnonymousDisposable(dispose);
    }
}
