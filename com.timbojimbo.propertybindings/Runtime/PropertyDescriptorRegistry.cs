using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimboJimbo.PropertyBindings
{
    /// <summary>Catalog of stable property descriptors used by binding selection and editor discovery.</summary>
    public static class PropertyDescriptorRegistry
    {
        private static readonly List<IPropertyDescriptor> _descriptors = new();
        private static readonly Dictionary<string, IPropertyDescriptor> _byId = new(StringComparer.Ordinal);

        static PropertyDescriptorRegistry()
        {
            UnityPropertyDescriptors.RegisterAll(Register);
        }

        public static IReadOnlyList<IPropertyDescriptor> Descriptors => _descriptors;

        public static void Register(IPropertyDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (_byId.TryGetValue(descriptor.Id, out var existing))
            {
                if (HaveSameContract(existing, descriptor)) return;
                throw new InvalidOperationException($"A property descriptor with ID '{descriptor.Id}' is already registered.");
            }

            _byId.Add(descriptor.Id, descriptor);
            _descriptors.Add(descriptor);
        }

        public static bool TryGet(string descriptorId, out IPropertyDescriptor descriptor)
        {
            if (!string.IsNullOrEmpty(descriptorId)) return _byId.TryGetValue(descriptorId, out descriptor);
            descriptor = null;
            return false;
        }

        public static bool TryGetForTarget(
            string descriptorId,
            UnityEngine.Object target,
            out IPropertyDescriptor descriptor)
        {
            if (TryGet(descriptorId, out descriptor) && descriptor.SupportsTarget(target)) return true;
            descriptor = null;
            return false;
        }

        public static void GetForTarget(UnityEngine.Object target, List<IPropertyDescriptor> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            for (int i = 0; i < _descriptors.Count; i++)
                if (_descriptors[i].SupportsTarget(target)) results.Add(_descriptors[i]);
        }

        private static bool HaveSameContract(IPropertyDescriptor a, IPropertyDescriptor b) =>
            a.Id == b.Id && a.TargetType == b.TargetType && a.ValueType == b.ValueType &&
            a.SerializedPath == b.SerializedPath && a.Kind == b.Kind && a.Layout == b.Layout &&
            a.ComponentOnePath == b.ComponentOnePath && a.ComponentTwoPath == b.ComponentTwoPath &&
            a.ComponentThreePath == b.ComponentThreePath && a.ComponentFourPath == b.ComponentFourPath;
    }
}