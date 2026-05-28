# Timbo Jimbo - Property Bindings

Lets you find and drive bindable properties on GameObject

> [!WARNING]
> This package is new - use at your own risk! :)

## BindableProperty

Bindable properties are discovered via [editor utility](https://github.com/Timbo-Jimbo/PropertyBindings/blob/main/com.timbojimbo.propertybindings/Editor/Utility/BindablePropertyUtility.cs#L15). They hold within them all the information needed to bind to a specific property on a specific instance of an object. 

It is serializable. You can use the editor tooling to find some `BindableProperty`'s that you want to drive, store them in a `List`, and at runtime you can bind to them and drive them. 

## IPropertyBinding

Bound properties are driven by various `IPropertyBinding` implementations:
- `TransformPropertyBinding`
- `CanvasGroupPropertyBinding`
- `ImagePropertyBinding`
- etc

These implementations drive the fields on the values directly.

For example: when driving the `position` of a `Transform`, the `TransformPropertyBinding` will read and write to `target.position`. This is efficient and also crucial for certain types of Components that want a chance to set some dirty flags when values are written to (Mainly UGUI Components) - setting an `Image`'s color property will mark it's verticies dirty and so on. If you try to drive an `Image`'s color via reflection then your changes won't be visible until _something else_ causes that `Image` to mark its verticies as dirty. `IPropertyBinding` implementations solve all of that.

`GenericPropertyBinding` is used for properties lacking a specific implementation. It is powered by [the same Unity API's that power built in Animations.](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Animations.GenericBinding.html). `ReflectionPropertyBinding` acts as a final catch all for properties that can't be driven any other way. 

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
The `PropertyBindingRegistry` is where `IPropertyBinding`s live. You register your custom bindings via `PropertyBindingRegistry.Register`. You pass in a method that checks if your `IPropertyBinding` can drive a `BindableProperty`, and a factory method to create a new instance of your `IPropertyBinding`. 

It is up to you how and where you want to go about this. The built in `IPropertyBinding`s register via a static ctor on `PropertyBindingRegistry`, and they all follow a similar pattern of exposing a static `CanBind` method as well as an private `TryGetBindingInfo` method that is used to by both `CanBind` and by the `IPropertyBinding` itself to tell it  _what_ it is driving and _how_ it is driving it. 

Again, this is just a pattern and it isn't enforced by the framework - but it should equip you well enough to read some of the `IPropertyBinding` implementation source code and get a good feel for how it all works.

## Extra
### UserEditTracker
You can use `UserEditTracker` to track changes that a user makes in the editor between calling `StartDetecting` and `StopDetecting`. This tool is useful when you want to write editor tooling that wants to detect the users edits and perform actions as a result of that. Unitys built in Animation window works a bit like this - in Record mode, you make a change and that change generates a keyframe in the Animation timeline. You could use this tool to detect those sorts of changes and build a similar Animation tool yourself. 

Under the hood it uses the Undo/Redo api's built into Unity to surface changes to `BindableProperty`s that are _direct result_ of a users edits. Each change to a `BindableProperty` generates a `BindablePropertyValueEdit`. This contains a reference to the `BindableProperty` as well as its `InitialValue` and `CurrentValue`. The `BindablePropertyValueEdit` is reused/updated for the session (that is, between the `StartDetecting` and `StopDetecting` calls) so you can store a reference to it. 

Put another way: if the user makes 10 seperate edits across 3 distinct `BindableProperty`s, then you will have had 10 change events raised and would have observed 3 unique `BindablePropertyValueEdit` - one for each `BindableProperty`, each containing their respective `BindableProperty`s initial value (the value _before_ the first of the 10 edits that touched that `BindableProperty`) and its current value (the value _after_ all 10 edits).
