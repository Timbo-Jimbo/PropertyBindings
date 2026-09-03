## [Unreleased]

## [0.8.2] - 03/09/2026

### Added

- `ValueContainer.From<T>(T)` now accepts every supported value type (previously enums only), so generic code can construct typed values without per-type overloads; unsupported types throw at construction

## [0.8.1] - 02/09/2026

### Fixed

- Direct and bulk writes now reject `ValueContainer` values whose kind does not match the target `BindableProperty`, preventing malformed typed writes from corrupting target values

## [0.8.0] - 02/09/2026

### Breaking changes

- `BindableProperty` is now explicitly descriptor-backed or ad-hoc; descriptor-backed identity is target plus stable descriptor ID
- Specialized binding selection is descriptor-driven and no longer structurally matches raw serialized paths
- Removed the legacy scalar/component factory family and structural descriptor fallback; use `BindableProperty.Create(...)` with a descriptor or `CreateAdHoc(...)`
- Existing serialized properties must be migrated to canonical descriptor IDs or intentionally retained as ad-hoc properties

### Added

- Added typed, extensible property descriptors and canonical descriptors for specialized Unity properties
- Added descriptor registry lookup, target/value contract validation, and descriptor-aware diagnostics
- Added typed `ValueContainer.From(...)` overloads
- Added structured binding resolution diagnostics with candidate match kinds and preserved construction failures
- Added Play Mode regression coverage for custom `Graphic.color` virtual setters, generated visual state, repeated bulk writes, and disposal

### Fixed

- Fixed `Graphic.color` bindings to honor the public virtual property contract for custom `Graphic` subclasses
- Generic bindings now reject incompatible component layouts and validate Unity's returned buffer counts during construction
- Malformed composite bindings now fail at bind time instead of falling through to atomic reflection writes
- Registry creation and type resolution now traverse candidates consistently after construction failures
- Binding construction now cleans up partial native and collection state after failures

## [0.7.0] - 06/07/2026

- Added a Drawer for BindableProperty
- Added Scale helper method to ValueContainer
- Improved filtering API for UserEditTracker

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