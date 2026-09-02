using System;
using UnityEngine;

namespace TimboJimbo.PropertyBindings
{
    /// <summary>
    /// Stable, typed metadata for constructing a BindableProperty without repeating serialized paths.
    /// Third-party packages can declare their own descriptors without registering with this package.
    /// </summary>
    public readonly struct PropertyDescriptor<TTarget, TValue> where TTarget : UnityEngine.Object
    {
        public string Id { get; }
        public string SerializedPath { get; }
        public ValueKind Kind { get; }
        public ComponentLayout Layout { get; }
        public string ComponentOnePath { get; }
        public string ComponentTwoPath { get; }
        public string ComponentThreePath { get; }
        public string ComponentFourPath { get; }

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

        public BindableProperty Create(TTarget target) => BindableProperty.CreateWithComponentLayout(
            target, SerializedPath, Kind, Layout, ComponentOnePath,
            ComponentTwoPath, ComponentThreePath, ComponentFourPath);
    }
}