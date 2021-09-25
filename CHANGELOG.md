# Changelog
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.2.1] - 2021-09-25
- [Fix] Bumped `System.Collections.Immutable` dependency version from `1.4.0` to `1.7.1`. This fixes the shim dlls issue on .NET Framework targets.

## [2.2.0] - 2020-10-02
- [Feature] Added support for `null` disposables and delegates (which are ignored).
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
- [Fix] Added explicit `net461` target.

## [2.0.0] - 2018-06-02
- [Breaking] Fixed typo: `SingleDisposable<T>.IsDispoed` is now `SingleDisposable<T>.IsDisposed`.
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
