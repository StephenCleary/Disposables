# Disposables [![Build status](https://ci.appveyor.com/api/projects/status/2uhdduj0hu1o9f28?svg=true)](https://ci.appveyor.com/project/StephenCleary/disposables) [![codecov](https://codecov.io/gh/StephenCleary/Disposables/branch/master/graph/badge.svg)](https://codecov.io/gh/StephenCleary/Disposables) [![NuGet version](https://badge.fury.io/nu/Nito.Disposables.svg)](https://www.nuget.org/packages/Nito.Disposables) [![API docs](https://img.shields.io/badge/API-dotnetapis-blue.svg)](http://dotnetapis.com/pkg/Nito.Disposables)
IDisposable helper types.

## AnonymousDisposable

The `AnonymousDisposable` type wraps an `Action`, and invokes that `Action` exactly once when it is disposed. The first thread to call `Dispose` is the one that invokes the `Action`; all other threads that call `Dispose` are blocked until the `Action` is completed. Once the `Action` is completed, it is never invoked again; future calls to `AnonymousDisposable.Dispose` are no-ops.

You can create an `AnonymousDisposable` by calling `AnonymousDisposable.Create(Action)` or `new AnonymousDisposable(Action)`.

### Advanced

You can append an `Action` to an `AnonymousDisposable` by calling its `Add` method with the `Action` to add. If the `AnonymousDisposable` is already disposed (or is in the process of being disposed by another thread), then the additional `Action` is invoked immediately.

## CollectionDisposable

`CollectionDisposable` contains a collection of `IDisposable` instances, and disposes them all exactly once when it is disposed. The first thread to call `Dispose` is the one that disposes all instances; all other threads that call `Dispose` are blocked until all instances have been disposed. Once disposed, future calls to `CollectionDisposable.Dispose` are no-ops.

You can create a `CollectionDisposable` by calling `CollectionDisposable.Create(...)` or `new CollectionDisposable(...)`, passing the collection of disposables.

You can also append a disposable to the `CollectionDisposable` by calling its `Add` method and passing it the disposable. If the `CollectionDisposable` is already disposed (or is in the process of being disposed by another thread), then the additional disposable is disposed immediately.

## NoopDisposable

An `IDisposable` that does nothing when disposed.

You can retrieve the singleton instance via `NoopDisposable.Instance`.

# Advanced Types

## SingleDisposable&lt;T&gt;

The `SingleDisposable<T>` type is a base type for disposables that desire exactly-once semantics, blocking other threads calling `Dispose` until the initial `Dispose` is complete. Both `AnonymousDisposable` and `CollectionDisposable` inherit from this type.

The type `T` is an immutable type that represents the contextual state of the instance. It is initialized in the constructor, optionally updated by calling `TryUpdateContext`, and finally retrieved and passed to `Dispose(T)` exactly once when `Dispose()` is called.

When the base type invokes `Dispose(T)`, your derived type should perform whatever disposing logic it needs to.

## SingleNonblockingDisposable&lt;T&gt;

The `SingleNonblockingDisposable<T>` type is a base type for disposables that desire exactly-once semantics *without* blocking other threads calling `Dispose`. It works exactly like `SingleDisposable<T>`, except that once disposal has started, other threads calling `Dispose` will return immediately, treating the additional `Dispose` calls as a no-op.
