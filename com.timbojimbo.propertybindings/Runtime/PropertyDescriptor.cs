using System;
using UnityEngine;

namespace TimboJimbo.PropertyBindings
{
    public interface IPropertyDescriptor
    {
        string Id { get; }
        Type TargetType { get; }
        Type ValueType { get; }
        string SerializedPath { get; }
        ValueKind Kind { get; }
        ComponentLayout Layout { get; }
        string ComponentOnePath { get; }
        string ComponentTwoPath { get; }
        string ComponentThreePath { get; }
        string ComponentFourPath { get; }
        bool SupportsTarget(UnityEngine.Object target);
        bool Matches(BindableProperty property);
        BindableProperty Create(UnityEngine.Object target);
    }

    /// <summary>
    /// Stable, typed metadata for constructing a BindableProperty without repeating serialized paths.
    /// Register third-party descriptors with PropertyDescriptorRegistry, or register them together
    /// with a specialized binding through PropertyBindingRegistry.
    /// </summary>
    public readonly struct PropertyDescriptor<TTarget, TValue> : IPropertyDescriptor where TTarget : UnityEngine.Object
    {
        public string Id { get; }
        public string SerializedPath { get; }
        public ValueKind Kind { get; }
        public ComponentLayout Layout { get; }
        public string ComponentOnePath { get; }
        public string ComponentTwoPath { get; }
        public string ComponentThreePath { get; }
        public string ComponentFourPath { get; }
        public Type TargetType => typeof(TTarget);
        public Type ValueType => typeof(TValue);

        public PropertyDescriptor(
            string id,
            string serializedPath,
            ValueKind kind,
            ComponentLayout layout,
            string componentOnePath,
            string componentTwoPath = null,
            string componentThreePath = null,
            string componentFourPath = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Descriptor ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(serializedPath)) throw new ArgumentException("Serialized path is required.", nameof(serializedPath));
            var inferredKind = InferValueKind(typeof(TValue));
            if (kind != inferredKind)
                throw new ArgumentException(
                    $"Descriptor value type {typeof(TValue).FullName} requires {inferredKind}, not {kind}.",
                    nameof(kind));
            if (string.IsNullOrWhiteSpace(componentOnePath) ||
                (layout >= ComponentLayout.Two && string.IsNullOrWhiteSpace(componentTwoPath)) ||
                (layout >= ComponentLayout.Three && string.IsNullOrWhiteSpace(componentThreePath)) ||
                (layout >= ComponentLayout.Four && string.IsNullOrWhiteSpace(componentFourPath)))
                throw new ArgumentException("Every component required by the layout must have a path.", nameof(componentOnePath));
            Id = id;
            SerializedPath = serializedPath;
            Kind = kind;
            Layout = layout;
            ComponentOnePath = componentOnePath;
            ComponentTwoPath = componentTwoPath;
            ComponentThreePath = componentThreePath;
            ComponentFourPath = componentFourPath;
        }

        private static ValueKind InferValueKind(Type valueType)
        {
            if (valueType == typeof(int)) return ValueKind.Int;
            if (valueType == typeof(float)) return ValueKind.Float;
            if (valueType == typeof(Vector2)) return ValueKind.Vector2;
            if (valueType == typeof(Vector3)) return ValueKind.Vector3;
            if (valueType == typeof(Vector4)) return ValueKind.Vector4;
            if (valueType == typeof(Color)) return ValueKind.Color;
            if (valueType == typeof(Quaternion)) return ValueKind.Quaternion;
            if (valueType == typeof(bool)) return ValueKind.Bool;
            if (valueType.IsEnum) return ValueKind.Enum;
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType)) return ValueKind.Reference;
            if (valueType == typeof(string)) return ValueKind.String;
            throw new NotSupportedException($"Property descriptor value type {valueType.FullName} is not supported.");
        }

        public BindableProperty Create(TTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            PropertyDescriptorRegistry.Register(this);
            return BindableProperty.CreateFromDescriptor(target, Id);
        }

        public bool SupportsTarget(UnityEngine.Object target) => target is TTarget;

        public bool Matches(BindableProperty property)
        {
            return property.Target is TTarget &&
                   string.Equals(property.DescriptorId, Id, StringComparison.Ordinal);
        }

        BindableProperty IPropertyDescriptor.Create(UnityEngine.Object target)
        {
            if (target is not TTarget typedTarget)
                throw new ArgumentException(
                    $"Descriptor '{Id}' requires {typeof(TTarget).FullName}, not {target?.GetType().FullName ?? "null"}.",
                    nameof(target));
            return Create(typedTarget);
        }
    }
}