using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.PropertyBindings
{
    public enum ComponentLayout
    {
        One,
        Two,
        Three,
        Four
    }

    [Serializable]
    public struct BindableProperty : IEquatable<BindableProperty>
    {
        public Object Target => _target;
        public string DescriptorId => _descriptorId;
        public bool HasDescriptor => !string.IsNullOrEmpty(_descriptorId);
        public bool IsAdHoc => !HasDescriptor;
        public string Path => TryGetDescriptor(out var descriptor) ? descriptor.SerializedPath : _path;
        public ValueKind Kind => TryGetDescriptor(out var descriptor) ? descriptor.Kind : _kind;
        public ComponentLayout ComponentLayout => TryGetDescriptor(out var descriptor) ? descriptor.Layout : _componentLayout;
        public string ComponentOnePath => TryGetDescriptor(out var descriptor) ? descriptor.ComponentOnePath : _componentOnePath;
        public string ComponentTwoPath => TryGetDescriptor(out var descriptor) ? descriptor.ComponentTwoPath : _componentTwoPath;
        public string ComponentThreePath => TryGetDescriptor(out var descriptor) ? descriptor.ComponentThreePath : _componentThreePath;
        public string ComponentFourPath => TryGetDescriptor(out var descriptor) ? descriptor.ComponentFourPath : _componentFourPath;

        [SerializeField] private Object _target;
        [SerializeField] private string _descriptorId;
        [SerializeField] private string _path;
        [SerializeField] private ValueKind _kind;
        [SerializeField] private ComponentLayout _componentLayout;
        [SerializeField] private string _componentOnePath;
        [SerializeField] private string _componentTwoPath;
        [SerializeField] private string _componentThreePath;
        [SerializeField] private string _componentFourPath;

        public bool IsValid => _target != null &&
                               (HasDescriptor
                                   ? TryGetDescriptor(out _)
                                   : !string.IsNullOrEmpty(_path) && _kind != ValueKind.Invalid);


        public static BindableProperty Invalid => default;

        public static ComponentLayout ExpectedGenericComponentLayout(ValueKind kind)
        {
            return kind switch
            {
                ValueKind.Vector2 => ComponentLayout.Two,
                ValueKind.Vector3 => ComponentLayout.Three,
                ValueKind.Vector4 => ComponentLayout.Four,
                ValueKind.Color => ComponentLayout.Four,
                ValueKind.Quaternion => ComponentLayout.Four,
                _ => ComponentLayout.One
            };
        }

        public static bool IsGenericLayoutCompatible(ValueKind kind, ComponentLayout layout) =>
            kind != ValueKind.Invalid && ExpectedGenericComponentLayout(kind) == layout;

        public static BindableProperty Create<TTarget, TValue>(
            TTarget target,
            PropertyDescriptor<TTarget, TValue> descriptor)
            where TTarget : Object => descriptor.Create(target);

        internal static BindableProperty CreateFromDescriptor(
            Object target,
            string descriptorId)
        {
            PropBindingsAssert.IsNotNull(target);
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(descriptorId));
            return new BindableProperty { _target = target, _descriptorId = descriptorId };
        }

        public bool TryGetDescriptor(out IPropertyDescriptor descriptor) =>
            PropertyDescriptorRegistry.TryGetForTarget(_descriptorId, _target, out descriptor);

        public static BindableProperty CreateUnresolvedTarget(Object target)
        {
            PropBindingsAssert.IsNotNull(target);

            return new BindableProperty
            {
                _target = target,
                _descriptorId = string.Empty,
                _path = string.Empty,
                _kind = ValueKind.Invalid,
                _componentLayout = ComponentLayout.One,
                _componentOnePath = string.Empty,
                _componentTwoPath = string.Empty,
                _componentThreePath = string.Empty,
                _componentFourPath = string.Empty,
            };
        }

        public static BindableProperty CreateAdHoc(
            Object target,
            string path,
            ValueKind kind,
            ComponentLayout componentLayout = ComponentLayout.One,
            string componentOnePath = null,
            string componentTwoPath = null,
            string componentThreePath = null,
            string componentFourPath = null)
        {
            PropBindingsAssert.IsNotNull(target);
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(path));
            PropBindingsAssert.IsTrue(kind != ValueKind.Invalid);
            PropBindingsAssert.IsTrue(componentLayout is >= ComponentLayout.One and <= ComponentLayout.Four);

            componentOnePath ??= path;
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentOnePath));
            if (componentLayout >= ComponentLayout.Two) PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentTwoPath));
            if (componentLayout >= ComponentLayout.Three) PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentThreePath));
            if (componentLayout >= ComponentLayout.Four) PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentFourPath));

            return new BindableProperty
            {
                _target = target,
                _descriptorId = string.Empty,
                _path = path,
                _kind = kind,
                _componentLayout = componentLayout,
                _componentOnePath = componentOnePath,
                _componentTwoPath = componentTwoPath ?? string.Empty,
                _componentThreePath = componentThreePath ?? string.Empty,
                _componentFourPath = componentFourPath ?? string.Empty
            };
        }

        private long ComputeIdentityHash()
        {
            unchecked
            {
                long hash = _target != null ? _target.GetHashCode() : 0L;
                if (HasDescriptor)
                    return hash * 6364136223846793005L + StringComparer.Ordinal.GetHashCode(_descriptorId);
                hash = hash * 6364136223846793005L + (_path?.GetHashCode() ?? 0L);
                hash = hash * 6364136223846793005L + _kind.GetHashCode();
                hash = hash * 6364136223846793005L + _componentLayout.GetHashCode();
                hash = hash * 6364136223846793005L + (_componentOnePath?.GetHashCode() ?? 0);
                hash = hash * 6364136223846793005L + (_componentTwoPath ?? string.Empty).GetHashCode();
                hash = hash * 6364136223846793005L + (_componentThreePath ?? string.Empty).GetHashCode();
                hash = hash * 6364136223846793005L + (_componentFourPath ?? string.Empty).GetHashCode();
                return hash;
            }
        }

        public bool Equals(BindableProperty other)
        {
            if (_target != other._target || HasDescriptor != other.HasDescriptor) return false;
            if (HasDescriptor)
                return string.Equals(_descriptorId, other._descriptorId, StringComparison.Ordinal);

            return ComputeIdentityHash() == other.ComputeIdentityHash() &&
                   _path == other._path &&
                   _kind == other._kind &&
                   _componentLayout == other._componentLayout &&
                   _componentOnePath == other._componentOnePath &&
                   (_componentTwoPath ?? string.Empty) == (other._componentTwoPath ?? string.Empty) &&
                   (_componentThreePath ?? string.Empty) == (other._componentThreePath ?? string.Empty) &&
                   (_componentFourPath ?? string.Empty) == (other._componentFourPath ?? string.Empty);
        }

        public override bool Equals(object obj)
        {
            return obj is BindableProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            long identityHash = ComputeIdentityHash();
            return (int)(identityHash ^ (identityHash >> 32));
        }

        public static bool operator ==(BindableProperty left, BindableProperty right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BindableProperty left, BindableProperty right)
        {
            return !left.Equals(right);
        }
    }
    
    public class BindablePropertyEqualityComparer : IEqualityComparer<BindableProperty>
    {
        public static readonly BindablePropertyEqualityComparer Instance = new BindablePropertyEqualityComparer();

        public bool Equals(BindableProperty x, BindableProperty y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(BindableProperty obj)
        {
            return obj.GetHashCode();
        }
    }
}