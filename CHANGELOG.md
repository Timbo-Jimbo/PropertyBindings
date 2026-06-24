## [0.6.2] - 24/06/2026

- Removed OnAfterDeserialize - was causing crashed when accessing .boxedValue on SerializedProperty which is apparently a known issue - and we don't really need it!

## [0.6.1] - 21/06/2026

- Removed some leftover debug logs

## [0.6.0] - 21/06/2026

- `PropertyBindingCollection.Bindings` (dictionary) replaced with `Properties` (read-only list)
- `StartBulkWriteScope()` renamed to `BulkWriteScope()`
- `TryWrite()` now automatically delegates to bulk or direct write depending on whether a bulk scope is active
- Added `TryDirectWrite()` for writes that must happen immediately (throws if bulk scope is active)
- Added `TryGetBindingType()` to query the binding type for a property
- `ReflectionPropertyBinding`: Complete rewrite of property path resolution to support nested paths (dot notation) and array/list index access
- `GenericPropertyBinding`: Fixed to exclude array element properties, which Unity's Generic Binding system does not support

## [0.5.0] - 04/06/2026

- Moved yet more shared code to Core package

## [0.4.0] - 04/06/2026

- Moved some shared code to Core package

## [0.3.1] - 04/06/2026

- Updated Readme.

## [0.3.0] - 01/06/2026

- Added unclamped lerp variants to `ValueContainer` and `HSV/Oklab/Oklch`

## [0.2.0] - 29/05/2026

- Added `CHANGELOG.md`
- Fixed `UserEditTracker` throwing error when handing reference value kinds
- Improved inspector for `ValueContainer`
- Moved editor code from `TimboJimbo.PropertyBindings.Editor` namespace to `TimboJimboEditor.PropertyBindings`
- Moved editor code from `TimboJimboTests.PropertyBindings` namespace to `TimboJimboTests.PropertyBindings`