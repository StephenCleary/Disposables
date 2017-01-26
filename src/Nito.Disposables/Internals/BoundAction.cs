using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// An action delegate bound with its context.
    /// </summary>
    /// <typeparam name="T">The type of context.</typeparam>
    public sealed class BoundAction<T>
    {
        private readonly Action<T> _action;
        private readonly T _context;

        /// <summary>
        /// Creates a bound action.
        /// </summary>
        /// <param name="action">The action delegate.</param>
        /// <param name="context">The context.</param>
        public BoundAction(Action<T> action, T context)
        {
            _action = action;
            _context = context;
        }

        /// <summary>
        /// Executes the action. This should only be done after the bound action is retrieved from a field by <see cref="TryGetAndUnset"/>.
        /// </summary>
        public void Invoke() => _action?.Invoke(_context);

        /// <summary>
        /// Atomically retrieves the bound action from the field and sets the field to <c>null</c>. May return <c>null</c>.
        /// </summary>
        /// <param name="field">The location of the bound action.</param>
        public static BoundAction<T> TryGetAndUnset(ref BoundAction<T> field)
        {
            return Interlocked.Exchange(ref field, null);
        }

        /// <summary>
        /// Attempts to update the context of the bound action stored in <paramref name="field"/>. Returns <c>false</c> if the field is <c>null</c>.
        /// </summary>
        /// <param name="field">The location of the bound action.</param>
        /// <param name="contextUpdater">The function used to update an existing context. This may be called more than once if more than one thread attempts to simultanously update the context.</param>
        public static bool TryUpdateContext(ref BoundAction<T> field, Func<T, T> contextUpdater)
        {
            while (true)
            {
                var original = Interlocked.CompareExchange(ref field, field, field);
                if (original == null)
                    return false;
                var updatedContext = new BoundAction<T>(original._action, contextUpdater(original._context));
                var result = Interlocked.CompareExchange(ref field, updatedContext, original);
                if (ReferenceEquals(original, result))
                    return true;
            }
        }
    }
}
