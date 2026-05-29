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
    public struct BindableProperty : IEquatable<BindableProperty>, ISerializationCallbackReceiver
    {
        public Object Target => _target;
        public string Path => _path;
        public ValueKind Kind => _kind;
        public ComponentLayout ComponentLayout => _componentLayout;
        public string ComponentOnePath => _componentOnePath;
        public string ComponentTwoPath => _componentTwoPath;
        public string ComponentThreePath => _componentThreePath;
        public string ComponentFourPath => _componentFourPath;

        [SerializeField] private Object _target;
        [SerializeField] private string _path;
        [SerializeField] private ValueKind _kind;
        [SerializeField] private ComponentLayout _componentLayout;
        [SerializeField] private string _componentOnePath;
        [SerializeField] private string _componentTwoPath;
        [SerializeField] private string _componentThreePath;
        [SerializeField] private string _componentFourPath;
        [NonSerialized] private long _identityHash;
        [NonSerialized] private bool _identityHashComputed;


        public static BindableProperty Invalid => default;

        public static BindableProperty CreateScalar(Object target, string path, ValueKind kind)
        {
            PropBindingsAssert.IsNotNull(target);
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(path));
            
            var result = new BindableProperty
            {
                _target = target,
                _path = path,
                _kind = kind,
                _componentLayout = ComponentLayout.One,
                _componentOnePath = path,
                _componentTwoPath = string.Empty,
                _componentThreePath = string.Empty,
                _componentFourPath = string.Empty,
            };
            result.ComputeAndCacheIdentityHash();
            return result;
        }

        public static BindableProperty CreateTwoComponent(Object target, string path, ValueKind kind, string componentOnePath, string componentTwoPath)
        {
            PropBindingsAssert.IsNotNull(target);
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(path));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentOnePath));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentTwoPath));

            var result = new BindableProperty
            {
                _target = target,
                _path = path,
                _kind = kind,
                _componentLayout = ComponentLayout.Two,
                _componentOnePath = componentOnePath,
                _componentTwoPath = componentTwoPath,
                _componentThreePath = string.Empty,
                _componentFourPath = string.Empty,
            };
            result.ComputeAndCacheIdentityHash();
            return result;
        }

        public static BindableProperty CreateThreeComponent(Object target, string path, ValueKind kind, string componentOnePath, string componentTwoPath, string componentThreePath)
        {
            PropBindingsAssert.IsNotNull(target);
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(path));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentOnePath));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentTwoPath));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentThreePath));

            var result = new BindableProperty
            {
                _target = target,
                _path = path,
                _kind = kind,
                _componentLayout = ComponentLayout.Three,
                _componentOnePath = componentOnePath,
                _componentTwoPath = componentTwoPath,
                _componentThreePath = componentThreePath,
                _componentFourPath = string.Empty,
            };
            result.ComputeAndCacheIdentityHash();
            return result;
        }

        public static BindableProperty CreateFourComponent(Object target, string path, ValueKind kind, string componentOnePath, string componentTwoPath, string componentThreePath, string componentFourPath)
        {
            PropBindingsAssert.IsNotNull(target);
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(path));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentOnePath));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentTwoPath));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentThreePath));
            PropBindingsAssert.IsFalse(string.IsNullOrEmpty(componentFourPath));

            var result = new BindableProperty
            {
                _target = target,
                _path = path,
                _kind = kind,
                _componentLayout = ComponentLayout.Four,
                _componentOnePath = componentOnePath,
                _componentTwoPath = componentTwoPath,
                _componentThreePath = componentThreePath,
                _componentFourPath = componentFourPath
            };
            result.ComputeAndCacheIdentityHash();
            return result;
        }

        public static BindableProperty CreateWithComponentLayout(Object target, string path, ValueKind kind, ComponentLayout componentLayout, string componentOnePath, string componentTwoPath = null, string componentThreePath = null, string componentFourPath = null)
        {
            switch (componentLayout)
            {
                case ComponentLayout.One:
                    return CreateScalar(target, path, kind);
                case ComponentLayout.Two:
                    return CreateTwoComponent(target, path, kind, componentOnePath, componentTwoPath);
                case ComponentLayout.Three:
                    return CreateThreeComponent(target, path, kind, componentOnePath, componentTwoPath, componentThreePath);
                case ComponentLayout.Four:
                    return CreateFourComponent(target, path, kind, componentOnePath, componentTwoPath, componentThreePath, componentFourPath);
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentLayout), componentLayout, null);
            }
        }

        private void ComputeAndCacheIdentityHash()
        {
            unchecked
            {
                long hash = _target != null ? _target.GetHashCode() : 0L;
                hash = hash * 6364136223846793005L + (_path != null ? _path.GetHashCode() : 0L);
                hash = hash * 6364136223846793005L + _kind.GetHashCode();
                hash = hash * 6364136223846793005L + _componentLayout.GetHashCode();
                hash = hash * 6364136223846793005L + (_componentOnePath?.GetHashCode() ?? 0);
                hash = hash * 6364136223846793005L + (_componentTwoPath?.GetHashCode() ?? 0);
                hash = hash * 6364136223846793005L + (_componentThreePath?.GetHashCode() ?? 0);
                hash = hash * 6364136223846793005L + (_componentFourPath?.GetHashCode() ?? 0);
                _identityHash = hash;
            }
            _identityHashComputed = true;
        }

        public bool Equals(BindableProperty other)
        {
            if (!_identityHashComputed) ComputeAndCacheIdentityHash();
            if (!other._identityHashComputed) other.ComputeAndCacheIdentityHash();
            return _identityHash == other._identityHash;
        }

        public override bool Equals(object obj)
        {
            return obj is BindableProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (!_identityHashComputed)
                ComputeAndCacheIdentityHash();

            return (int)(_identityHash ^ (_identityHash >> 32));
        }

        public static bool operator ==(BindableProperty left, BindableProperty right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BindableProperty left, BindableProperty right)
        {
            return !left.Equals(right);
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            ComputeAndCacheIdentityHash();
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