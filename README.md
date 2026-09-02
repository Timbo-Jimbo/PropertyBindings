# Timbo Jimbo - Property Bindings

Lets you find and drive bindable properties on GameObjects

> [!WARNING]
> This package is new - use at your own risk! :)

# Installation

This package is available on [OpenUPM](https://openupm.com/packages/com.timbojimbo.propertybindings)

1. Add the Scoped Registry:
	- Open **Edit > Project Settings > Package Manager**
	- Add a new Scoped Registry (or append the missing scope if you already have one):
		- Name: `OpenUPM`
		- URL: `https://package.openupm.com/`
		- Scope(s): `com.timbojimbo`
2. Install the package
	- Open **Window > Package Manager**
	- Click Add and select **Add package by name...**
	- Paste name: `com.timbojimbo.propertybindings`

Done!


> [!WARNING]
> This package is new - use at your own risk! :)

<details>
<summary>Install from GitHub instead (Not Recommended)</summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- Open **Window > Package Manager**
- Click Add and select **Add package from git URL...**
- Paste `https://github.com/Timbo-Jimbo/PropertyBindings.git?path=Packages/com.timbojimbo.propertybindings`
</details>


## BindableProperty

Bindable properties are discovered via [editor utility](https://github.com/Timbo-Jimbo/PropertyBindings/blob/main/com.timbojimbo.propertybindings/Editor/Utility/BindablePropertyUtility.cs#L15). They hold within them all the information needed to bind to a specific property on a specific instance of an object. 

It is serializable. You can use the editor tooling to find some `BindableProperty`'s that you want to drive, store them in a `List`, and at runtime you can bind to them and drive them. 

### Property descriptors

Built-in properties are defined once as typed `PropertyDescriptor<TTarget, TValue>` values. Prefer constructing them with descriptors instead of repeating Unity serialized paths:

```csharp
var property = BindableProperty.Create(transform, TransformProperties.LocalPosition);
var value = ValueContainer.From(Vector3.zero);
```

Descriptors provide a stable ID, target/value types, serialized path, value kind, and component layout. Creating a property registers its descriptor and serializes only the target and descriptor ID as meaningful state. Editor discovery, binding selection, diagnostics, and downstream packages therefore share one property contract.

`BindableProperty` has two explicit forms:

- **Descriptor-backed:** use `BindableProperty.Create(target, descriptor)` for finite, registered contracts and specialized bindings. Equality is `target + descriptor ID`.
- **Ad-hoc:** use `BindableProperty.CreateAdHoc(...)` for arbitrary serialized or reflected fields. Equality uses the target and explicit path/kind/layout metadata.

An ad-hoc property does not acquire specialized behavior merely because its path resembles a registered descriptor. This keeps property identity deliberate and removes structural fallback or migration ambiguity.

## IPropertyBinding

Bound properties are driven by various `IPropertyBinding` implementations:
- `TransformPropertyBinding`
- `CanvasGroupPropertyBinding`
- `ImagePropertyBinding`
- etc

These implementations drive the fields on the values directly.

For example: when driving the `position` of a `Transform`, the `TransformPropertyBinding` will read and write to `target.position`. This is efficient and also crucial for certain types of Components that want a chance to set some dirty flags when values are written to (Mainly UGUI Components) - setting an `Image`'s color property will mark it's verticies dirty and so on. If you try to drive an `Image`'s color via reflection then your changes won't be visible until _something else_ causes that `Image` to mark its verticies as dirty. `IPropertyBinding` implementations solve all of that.

`GenericPropertyBinding` is used for properties lacking a specific implementation. It is powered by [the same Unity API's that power built in Animations.](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Animations.GenericBinding.html). `ReflectionPropertyBinding` acts as a final catch all for properties that can't be driven any other way. It supports simple property names, nested paths via dot notation (e.g. `material.color`), and array or list element access via index notation (e.g. `materials.Array.data[0]` or `items[2]`).

When you write to a property using one of those two fallbacks, we invoke `OnDidApplyAnimationProperties` on all effected targets so that they have a chance to regenerate/etc. This is a bit of a catch-all though, and isn't as efficient writing through the custom `IPropertyBinding` implementation. (It tells the target that _something_ changed, not _what_, so the target will have no choice but to regenerate everything)

You can also write your own `IPropertyBinding`s to drive your own components or any components that are missing. More on that bellow.

## Bringing the two together
### PropertyBindingCollection
You provide the `PropertyBindingCollection` a list of `BindableProperty`'s, and through it you can read and write to any of  those properties.

> [!WARNING]
> Remember to `Dispose` of the `PropertyBindingCollection` once you are done.

### Manual API
`PropertyBindingCollection` is a bit of a convenience wrapper around some API's that you can use yourself. Once you have a `BindableProperty`, you can create a `IPropertyBinding` to drive it using `PropertyBindingRegistry.Create`. Simply pass in your binding and it'll give you the best matching `IPropertyBinding` implementation to drive your `BindableProperty`. 

> [!WARNING]
> Remember to `Dispose` of your `IPropertyBinding` once you are done.

## PropertyBindingRegistry
The `PropertyBindingRegistry` is where `IPropertyBinding`s live. Specialized bindings register the descriptors they support plus a factory. Descriptor registration also publishes those descriptors through `PropertyDescriptorRegistry`, making them available to editor discovery and legacy migration.

```csharp
PropertyBindingRegistry.Register<MyPropertyBinding>(
	new IPropertyDescriptor[] { MyProperties.Amount },
	(root, property) => new MyPropertyBinding(root, property));
```

Open-ended fallback bindings can still use predicate registration. The built-in Generic and Reflection bindings use this form because arbitrary user-authored properties cannot be represented by a closed descriptor catalog.

Use `PropertyBindingRegistry.Diagnose` to inspect candidate priority, whether each candidate matched by descriptor or predicate, descriptor IDs, construction failures, and the selected binding type.

## Extra
### UserEditTracker
You can use `UserEditTracker` to track changes that a user makes in the editor between calling `StartDetecting` and `StopDetecting`. This tool is useful when you want to write editor tooling that wants to detect the users edits and perform actions as a result of that. Unitys built in Animation window works a bit like this - in Record mode, you make a change and that change generates a keyframe in the Animation timeline. You could use this tool to detect those sorts of changes and build a similar Animation tool yourself. 

Under the hood it uses the Undo/Redo api's built into Unity to surface changes to `BindableProperty`s that are _direct result_ of a users edits. Each change to a `BindableProperty` generates a `BindablePropertyValueEdit`. This contains a reference to the `BindableProperty` as well as its `InitialValue` and `CurrentValue`. The `BindablePropertyValueEdit` is reused/updated for the session (that is, between the `StartDetecting` and `StopDetecting` calls) so you can store a reference to it. 

Put another way: if the user makes 10 seperate edits across 3 distinct `BindableProperty`s, then you will have had 10 change events raised and would have observed 3 unique `BindablePropertyValueEdit` - one for each `BindableProperty`, each containing their respective `BindableProperty`s initial value (the value _before_ the first of the 10 edits that touched that `BindableProperty`) and its current value (the value _after_ all 10 edits).
