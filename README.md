![Logo](src/icon.png)

# Disposables [![Build status](https://github.com/StephenCleary/Disposables/workflows/Build/badge.svg)](https://github.com/StephenCleary/Disposables/actions?query=workflow%3ABuild) [![codecov](https://codecov.io/gh/StephenCleary/Disposables/branch/main/graph/badge.svg)](https://codecov.io/gh/StephenCleary/Disposables) [![NuGet version](https://badge.fury.io/nu/Nito.Disposables.svg)](https://www.nuget.org/packages/Nito.Disposables) [![API docs](https://img.shields.io/badge/API-FuGet-blue.svg)](https://www.fuget.org/packages/Nito.Disposables)
IDisposable helper types.

# Main Types

- `Disposable`/`AsyncDisposable` - When disposed, invokes an `Action`/`Func<ValueTask>`.
- `CollectionDisposable`/`AsyncCollectionDisposable` - When disposed, disposes a collection of other disposables.
- `IReferenceCountedDisposable<T>` - Maintains a reference count for a disposable and disposes it when the reference count reaches zero.
- `NoopDisposable` - When disposed, does nothing.

## Disposable and AsyncDisposable

The `Disposable` type wraps an `Action`, and invokes that `Action` exactly once when it is disposed. The first thread to call `Dispose` is the one that invokes the `Action`; all other threads that call `Dispose` are blocked until the `Action` is completed. Once the `Action` is completed, it is never invoked again; future calls to `Disposable.Dispose` are no-ops.

You can create a `Disposable` by calling `Disposable.Create(Action)` or `new Disposable(Action)`.

`AsyncDisposable` is exactly the same as `Disposable` except it wraps a `Func<ValueTask>`.

You can call `Abandon` to have the `Disposable`/`AsyncDisposable` abandon its disposal work and do nothing when it is disposed. `Abandon` returns the `Action` (or `Func<ValueTask>`) that it would have taken on disposal; this can be passed to `Create` to transfer ownership of the disposal actions.

If the `Action` (or `Func<Task>`) throws an exception, only the first caller of `Dispose` (or `DisposeAsync`) will observe the exception. All other calls to `Dispose` / `DisposeAsync` will wait for the delegate to complete, but they will not observe the exception.

### Advanced

You can append an `Action` to a `Disposable` by calling its `Add` method with the `Action` to add. When the `Disposable` is disposed, it will call its actions in reverse order. When `Add` is called, if the `Disposable` is already disposed (or is in the process of being disposed by another thread), then the additional `Action` is invoked immediately by the current thread after the disposal completes, and the other thread is not blocked waiting for the `Action` to complete.

`AsyncDisposable` may also have multiple delegates. By default, they are all invoked serially in reverse order, but you can change this to concurrent by creating the instance with the `AsyncDisposeFlags.ExecuteConcurrently` flag.

## CollectionDisposable

`CollectionDisposable` contains a collection of `IDisposable` instances, and disposes them all exactly once when it is disposed. The first thread to call `Dispose` is the one that disposes all instances; all other threads that call `Dispose` are blocked until all instances have been disposed. Once disposed, future calls to `CollectionDisposable.Dispose` are no-ops.

You can create a `CollectionDisposable` by calling `CollectionDisposable.Create(...)` or `new CollectionDisposable(...)`, passing the collection of disposables.

You can also append a disposable to the `CollectionDisposable` by calling its `Add` method and passing it the disposable. If the `CollectionDisposable` is already disposed (or is in the process of being disposed by another thread), then the additional disposable is disposed immediately by the current thread after the disposal completes, and the other thread is not blocked waiting for the additional disposable to dispose.

`AsyncCollectionDisposable` is exactly the same as `CollectionDisposable` except it is a collection of `IAsyncDisposable` instances. You can also create a mixed collection (containing both `IDisposable` and `IAsyncDisposable` instances) by calling `ToAsyncDisposable` on your `IDisposable` instances.

You can call `Abandon` to have the `CollectionDisposable`/`AsyncCollectionDisposable` abandon its disposal work and do nothing when it is disposed. `Abandon` returns the `IEnumerable<IDisposable>` (or `IEnumerable<IAsyncDisposable>`) that it would have disposed; this can be passed to `Create` to transfer ownership of the disposal actions.

By default, all `IAsyncDisposable` instances are disposed serially in reverse order, but you can change this to concurrent by creating the `AsyncCollectionDisposable` instance with the `AsyncDisposeFlags.ExecuteConcurrently` flag.

### Fixing Other Disposables

`CollectionDisposable` can be used as a wrapper to enforce only-dispose-once semantics on another disposable. If a type `IncorrectDisposable` has a `Dispose` method that breaks if it is called more than once, then `CollectionDisposable.Create(incorrectDisposable)` returns an `IDisposable` that will only invoke `IncorrectDisposable.Dispose` a single time, regardless of how many times you call `CollectionDisposable.Dispose`.

## Reference Counted Disposables

You can create a reference-counted disposable wrapping a target disposable by passing the target disposable to `ReferenceCountedDisposable.Create`. The reference-counted disposable represents an increment of the reference count, and decrements that reference count when disposed. When the reference count reaches zero, the target disposable is disposed.

You can increment the reference count by calling `IReferenceCountedDisposable<T>.AddReference`, which returns an independent reference-counted disposable representing its own increment of the reference count.

A reference-counted disposable can access its underlying target disposable via `IReferenceCountedDisposable<T>.Target`.

### Advanced: Weak Reference Counted Disposables

You can create a weak-reference-counted disposable by calling `IReferenceCountedDisposable<T>.AddWeakReference`. Weak-reference-counted disposables weakly reference the target disposable and the reference count. They do not represent an increment of the reference count.

You can attempt to increment the reference count for a weak-reference-counted disposable by calling `IWeakReferenceCountedDisposable<T>.TryAddReference`. If successful, this method returns a (strong) reference-counted disposable.

You can also attempt to access the underlying target disposable via `IWeakReferenceCountedDisposable<T>.TryGetTarget`.

### Advanced: Custom Reference Counting

Reference-counted disposables by default use an ephemeron for the reference count, so calling `ReferenceCountedDisposable.Create` multiple times on the same target disposable instance will share the underlying reference count. If the reference count is already be zero, this method will throw `ObjectDisposedException`; to avoid this exception, you can call `ReferenceCountedDisposable.TryCreate`.

If you want to use a *new* reference count and not use the ephemeron, you can call `ReferenceCountedDisposable.CreateWithNewReferenceCounter`. This usage avoids ephemerons, which put pressure on the garbage collector.

## NoopDisposable

A type implementing both `IDisposable` and `IAsyncDisposable` that does nothing when disposed.

You can retrieve the singleton instance via `NoopDisposable.Instance`.

# Advanced Types

## SingleDisposable&lt;T&gt;

The `SingleDisposable<T>` type is a base type for disposables that desire exactly-once semantics, blocking other threads calling `Dispose` until the initial `Dispose` is complete. Both `Disposable` and `CollectionDisposable` inherit from this type.

The type `T` is an immutable type that represents the contextual state of the instance. It is initialized in the constructor, optionally updated by calling `TryUpdateContext`, and finally retrieved and passed to `Dispose(T)` exactly once when `Dispose()` is called.

When the base type invokes `Dispose(T)`, your derived type should perform whatever disposing logic it needs to.

`AsyncSingleDisposable<T>` is exactly the same as `SingleDisposable<T>` except that it implements `IAsyncDisposable` instead of `IDisposable`.

If `Dispose(T)` (or `DisposeAsync(T)`) throws an exception, only the first caller of `Dispose` (or `DisposeAsync`) will observe the exception. All other calls to `Dispose` / `DisposeAsync` will wait for the delegate to complete, but they will not observe the exception.

## SingleNonblockingDisposable&lt;T&gt;

The `SingleNonblockingDisposable<T>` type is a base type for disposables that desire exactly-once semantics *without* blocking other threads calling `Dispose`. It works exactly like `SingleDisposable<T>`, except that once disposal has started, other threads calling `Dispose` will return immediately, treating the additional `Dispose` calls as a no-op.

`AsyncSingleNonblockingDisposable<T>` is exactly the same as `SingleNonblockingDisposable<T>` except that it implements `IAsyncDisposable` instead of `IDisposable`.
