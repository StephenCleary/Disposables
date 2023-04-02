# Changelog
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.5.0] - TBD
- [Feature] Added `TryAdd` methods to collection/delegate disposables to synchronously attempt to add.

## [2.4.0] - 2023-03-03
- [Feature] Added `Abandon` methods to ignore disposal or transfer ownership to another disposable. [#17](https://github.com/StephenCleary/Disposables/issues/17)
- [Fix] Reduced version of `System.Collections.Immutable` dependency to reduce version conflicts. [#16](https://github.com/StephenCleary/Disposables/issues/16)
- [Fix] Specified order of delegates/children disposal: inverse order of the collection. Note that this is a change in behavior; it was unspecified before but would always execute in *forward* order, meaning the first delegate/child added would be the first one disposed.

## [2.3.0] - 2021-12-30

- [Feature] Added `ReferenceCountedDisposable`, `ReferenceCountedAsyncDisposable`, and associated types. [#12](https://github.com/StephenCleary/Disposables/issues/12)
- [Fix] Fixed race condition bug (never observed). [#14](https://github.com/StephenCleary/Disposables/issues/14)

## [2.2.1] - 2021-09-25
- [Fix] Bumped `System.Collections.Immutable` dependency version from `1.4.0` to `1.7.1`. This fixes the shim dlls issue on .NET Framework targets.

## [2.2.0] - 2020-10-02
- [Feature] Added support for `null` disposables and delegates (which are ignored). [#13](https://github.com/StephenCleary/Disposables/issues/13)
- [Feature] Added `Disposable` and `AsyncDisposable`.

## [2.1.0] - 2020-06-08
- [Feature] Added `AnonymousAsyncDisposable`.
- [Feature] Added `CollectionAsyncDisposable`.
- [Feature] Added `AsyncDisposeFlags`.
- [Feature] Added `IDisposable.ToAsyncDisposable()`.
- [Feature] Added `SingleAsyncDisposable<T>`.
- [Feature] Added `SingleNonblockingAsyncDisposable<T>`.
- [Feature] Added `netstandard2.1` target.

## [2.0.1] - 2019-07-20
- [Fix] Published NuGet symbol packages.
- [Fix] Added explicit `net461` target. [#4](https://github.com/StephenCleary/Disposables/issues/4)

## [2.0.0] - 2018-06-02
- [Breaking] Fixed typo: `SingleDisposable<T>.IsDispoed` is now `SingleDisposable<T>.IsDisposed`. [#3](https://github.com/StephenCleary/Disposables/issues/3)
- [Feature] Added source linking.

## [1.2.3] - 2017-09-09
- [Fix] Removed public signing.

## [1.2.2] - 2017-08-26
- [Fix] Added explicit `netstandard2.0` target.

## [1.2.1] - 2017-08-26
- [Fix] Added public signing.

## [1.2.0] - 2017-08-23
- [Feature] Added `NoopDisposable`.

## [1.1.0] - 2017-02-28
- [Feature] Added `AnonymousDisposable`.
- [Feature] Added `CollectionDisposable`.
- [Feature] Added `SingleNonblockingDisposable<T>`.

## [1.0.0] - 2016-08-16
- [Feature] Added `SingleDisposable<T>`.
