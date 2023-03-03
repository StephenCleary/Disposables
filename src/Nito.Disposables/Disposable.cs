using System;
using System.Linq;

namespace Nito.Disposables
{
    /// <summary>
    /// A disposable that executes a delegate when disposed.
    /// </summary>
    public sealed class Disposable : SingleDisposable<Action?>
    {
        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public Disposable(Action? dispose)
            : base(dispose)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(Action? context)
        {
            if (context == null)
                return;
            foreach (var handler in context.GetInvocationList().Reverse().Cast<Action>())
                handler();
        }

        /// <summary>
        /// Adds a delegate to be executed when this instance is disposed. If this instance is already disposed or disposing, then <paramref name="dispose"/> is executed immediately.
        /// If this method is called multiple times concurrently at the same time this instance is disposed, then the different <paramref name="dispose"/> arguments may be disposed concurrently.
        /// </summary>
        /// <param name="dispose">The delegate to add. May be <c>null</c> to indicate no additional action.</param>
        public void Add(Action? dispose)
        {
            if (dispose == null)
                return;
            if (TryUpdateContext(x => x + dispose))
                return;

            // Wait for our disposal to complete; then call the additional delegate.
            Dispose();
            dispose();
        }

        /// <summary>
        /// Makes this disposable do nothing when it is disposed. Returns the actions this disposable *would* have taken; these can be passed to a new instance to transfer ownership.
        /// </summary>
        public Action? Abandon()
        {
            Action? result = null;
            var updated = TryUpdateContext(x =>
            {
                result = x;
                return null;
            });
            if (!updated)
                result = null;
            return result;
        }

        /// <summary>
        /// Creates a new disposable that executes <paramref name="dispose"/> when disposed.
        /// </summary>
        /// <param name="dispose">The delegate to execute when disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static Disposable Create(Action? dispose) => new Disposable(dispose);
    }
}
