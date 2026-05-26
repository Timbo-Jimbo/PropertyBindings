using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class ReflectionPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private readonly BindableProperty _property;

        public static bool CanBind(BindableProperty property) => ReflectionReadWrite.CanAccess(property);

        public ReflectionPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            _property = property;
        }

        public override void Dispose()
        {
        }

        protected override bool TargetMustBeNotifiedOnWrite() => true;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer) =>
            ReflectionReadWrite.TryRead(_property, out valueContainer);

        protected override bool TryWriteToTarget(ValueContainer valueContainer) =>
            ReflectionReadWrite.TryWrite(_property, valueContainer);


        private static class ReflectionReadWrite
        {
            public static bool CanAccess(BindableProperty property)
            {
                if (property.Target == null || string.IsNullOrEmpty(property.Path))
                    return false;

                var accessor = PropertyAccessorCache.GetFor(property);
                return accessor.IsValid();
            }

            public static bool TryRead(BindableProperty property, out ValueContainer valueContainer)
            {
                valueContainer = default;
                
                if (property.Target == null || string.IsNullOrEmpty(property.Path))
                    return false;

                //read via reflection
                var propertyAccessor = PropertyAccessorCache.GetFor(property);
                var value = propertyAccessor.GetValue(property.Target);

                switch (property.Kind)
                {
                    case ValueKind.Int:
                        valueContainer = ValueContainer.FromInt((int)value);
                        break;
                    case ValueKind.Float:
                        valueContainer = ValueContainer.FromFloat((float)value);
                        break;
                    case ValueKind.Vector2:
                        valueContainer = ValueContainer.FromVector2((Vector2)value);
                        break;
                    case ValueKind.Vector3:
                        valueContainer = ValueContainer.FromVector3((Vector3)value);
                        break;
                    case ValueKind.Vector4:
                        valueContainer = ValueContainer.FromVector4((Vector4)value);
                        break;
                    case ValueKind.Color:
                        valueContainer = ValueContainer.FromColor((Color)value);
                        break;
                    case ValueKind.Quaternion:
                        valueContainer = ValueContainer.FromQuaternion((Quaternion)value);
                        break;
                    case ValueKind.Bool:
                        valueContainer = ValueContainer.FromBool((bool)value);
                        break;
                    case ValueKind.Enum:
                        valueContainer = ValueContainer.FromEnum(System.Convert.ToInt32(value));
                        break;
                    case ValueKind.Reference:
                        valueContainer = ValueContainer.FromReference((Object)value);
                        break;
                    case ValueKind.String:
                        valueContainer = ValueContainer.FromString((string)value);
                        break;
                }

                return true;
            }

            public static bool TryWrite(BindableProperty property, ValueContainer valueContainer)
            {
                if (property.Target == null || string.IsNullOrEmpty(property.Path))
                    return false;

                var propertyAccessor = PropertyAccessorCache.GetFor(property);
                object valueToWrite = null;

                switch (property.Kind)
                {
                    case ValueKind.Int:
                        valueToWrite = valueContainer.IntValue;
                        break;
                    case ValueKind.Float:
                        valueToWrite = valueContainer.FloatValue;
                        break;
                    case ValueKind.Vector2:
                        valueToWrite = valueContainer.Vector2Value;
                        break;
                    case ValueKind.Vector3:
                        valueToWrite = valueContainer.Vector3Value;
                        break;
                    case ValueKind.Vector4:
                        valueToWrite = valueContainer.Vector4Value;
                        break;
                    case ValueKind.Color:
                        valueToWrite = valueContainer.ColorValue;
                        break;
                    case ValueKind.Quaternion:
                        valueToWrite = valueContainer.QuaternionValue;
                        break;
                    case ValueKind.Bool:
                        valueToWrite = valueContainer.BoolValue;
                        break;
                    case ValueKind.Enum:
                        valueToWrite = Enum.ToObject(propertyAccessor.GetValueType(), valueContainer.EnumValue);
                        break;
                    case ValueKind.Reference:
                        valueToWrite = valueContainer.ReferenceValue;
                        break;
                    case ValueKind.String:
                        valueToWrite = valueContainer.StringValue;
                        break;
                }

                propertyAccessor.SetValue(property.Target, valueToWrite);
                return true;
            }
            
            private static class PropertyAccessorCache
            {
                private static readonly Dictionary<(Type, string), PropertyAccessor> _cache = new Dictionary<(Type, string), PropertyAccessor>();

                public static PropertyAccessor GetFor(BindableProperty property)
                {
                    var key = (property.Target.GetType(), property.Path);
                    if (!_cache.TryGetValue(key, out var result))
                    {
                        result = new PropertyAccessor(property);
                        _cache[key] = result;
                    }

                    return result;
                }            
            }

            private class PropertyAccessor
            {
                [CanBeNull] private FieldInfo _fieldInfo;
                [CanBeNull] private PropertyInfo _propertyInfo;
                private Type _targetType;

                public PropertyAccessor(BindableProperty property)
                {
                    _targetType = property.Target.GetType();
                    
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    
                    _fieldInfo = _targetType.GetField(property.Path, flags);

                    if (_fieldInfo == null)
                        _propertyInfo = _targetType.GetProperty(property.Path, flags);
                }

                public bool IsValid() => _fieldInfo != null || _propertyInfo != null;

                public object GetValue(Object target)
                {
                    if (_fieldInfo != null)
                        return _fieldInfo.GetValue(target);
                    if (_propertyInfo != null)
                        return _propertyInfo.GetValue(target);

                    throw new InvalidOperationException($"No field or property found at path '{_fieldInfo?.Name ?? _propertyInfo?.Name}' on type '{_targetType.FullName}'.");
                
                }

                public void SetValue(Object target, object value)
                {
                    if (_fieldInfo != null)
                        _fieldInfo.SetValue(target, value);
                    else if (_propertyInfo != null)
                        _propertyInfo.SetValue(target, value);
                    else
                        throw new InvalidOperationException($"No field or property found at path '{_fieldInfo?.Name ?? _propertyInfo?.Name}' on type '{_targetType.FullName}'.");
                }

                public Type GetValueType()
                {
                    if (_fieldInfo != null)
                        return _fieldInfo.FieldType;
                    if (_propertyInfo != null)
                        return _propertyInfo.PropertyType;

                    throw new InvalidOperationException($"No field or property found at path '{_fieldInfo?.Name ?? _propertyInfo?.Name}' on type '{_targetType.FullName}'.");
                }
            }
        }
    }
}