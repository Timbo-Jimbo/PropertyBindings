using System;
using System.Collections;
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

                try
                {
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

                catch
                {
                    return false;
                }
            }

            public static bool TryWrite(BindableProperty property, ValueContainer valueContainer)
            {
                if (property.Target == null || string.IsNullOrEmpty(property.Path))
                    return false;

                try
                {
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
                catch
                {
                    return false;
                }
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
                private readonly PathStep[] _steps;
                private Type _targetType;
                private readonly Type _valueType;
                private readonly bool _isValid;

                public PropertyAccessor(BindableProperty property)
                {
                    _targetType = property.Target.GetType();

                    try
                    {
                        _steps = BuildSteps(property.Path, _targetType);
                        _valueType = _steps.Length > 0 ? _steps[_steps.Length - 1].ValueType : _targetType;
                        _isValid = true;
                    }
                    catch
                    {
                        _steps = Array.Empty<PathStep>();
                        _valueType = typeof(object);
                        _isValid = false;
                    }
                }

                public bool IsValid() => _isValid;

                public object GetValue(Object target)
                {
                    if (!_isValid)
                        throw new InvalidOperationException($"No field or property found at path '{_targetType.FullName}'.");

                    return GetValueRecursive(target, 0);
                }

                public void SetValue(Object target, object value)
                {
                    if (!_isValid)
                        throw new InvalidOperationException($"No field or property found at path '{_targetType.FullName}'.");

                    SetValueRecursive(target, 0, value);
                }

                public Type GetValueType()
                {
                    if (!_isValid)
                        throw new InvalidOperationException($"No field or property found at path '{_targetType.FullName}'.");

                    return _valueType;
                }

                private object GetValueRecursive(object target, int stepIndex)
                {
                    if (stepIndex >= _steps.Length)
                        return target;

                    var value = _steps[stepIndex].GetValue(target);

                    if (value == null && stepIndex < _steps.Length - 1)
                        throw new InvalidOperationException($"Null encountered while resolving path on type '{_targetType.FullName}'.");

                    return GetValueRecursive(value, stepIndex + 1);
                }

                private object SetValueRecursive(object target, int stepIndex, object value)
                {
                    var step = _steps[stepIndex];

                    if (stepIndex == _steps.Length - 1)
                    {
                        step.SetValue(target, value);
                        return target;
                    }

                    var child = step.GetValue(target);

                    if (child == null)
                        throw new InvalidOperationException($"Null encountered while resolving path on type '{_targetType.FullName}'.");

                    var modifiedChild = SetValueRecursive(child, stepIndex + 1, value);
                    step.SetValue(target, modifiedChild);
                    return target;
                }

                private static PathStep[] BuildSteps(string path, Type targetType)
                {
                    var steps = new List<PathStep>();
                    var currentType = targetType;

                    var pathSegments = path.Split('.');

                    for (var i = 0; i < pathSegments.Length; i++)
                    {
                        var segment = pathSegments[i];

                        if (segment == "Array" && i + 1 < pathSegments.Length && TryParseArrayDataSegment(pathSegments[i + 1], out var arrayIndex))
                        {
                            steps.Add(new IndexStep(currentType, arrayIndex));
                            currentType = GetElementType(currentType);
                            i++;
                            continue;
                        }

                        ParseMemberSegment(segment, currentType, steps, ref currentType);
                    }

                    return steps.ToArray();
                }

                private static void ParseMemberSegment(string segment, Type currentType, List<PathStep> steps, ref Type nextType)
                {
                    if (string.IsNullOrEmpty(segment))
                        throw new InvalidOperationException("Invalid empty segment in property path.");

                    var bracketIndex = segment.IndexOf('[');
                    var memberName = bracketIndex >= 0 ? segment.Substring(0, bracketIndex) : segment;

                    if (!string.IsNullOrEmpty(memberName))
                    {
                        var memberStep = new MemberStep(currentType, memberName);
                        steps.Add(memberStep);
                        nextType = memberStep.ValueType;
                    }

                    while (bracketIndex >= 0)
                    {
                        var endBracketIndex = segment.IndexOf(']', bracketIndex + 1);
                        if (endBracketIndex < 0)
                            throw new InvalidOperationException($"Invalid array index segment '{segment}'.");

                        var indexText = segment.Substring(bracketIndex + 1, endBracketIndex - bracketIndex - 1);
                        if (!int.TryParse(indexText, out var index))
                            throw new InvalidOperationException($"Invalid array index segment '{segment}'.");

                        var indexStep = new IndexStep(nextType, index);
                        steps.Add(indexStep);
                        nextType = indexStep.ValueType;
                        bracketIndex = segment.IndexOf('[', endBracketIndex + 1);
                    }
                }

                private static bool TryParseArrayDataSegment(string segment, out int index)
                {
                    index = default;

                    if (!segment.StartsWith("data[", StringComparison.Ordinal) || !segment.EndsWith("]", StringComparison.Ordinal))
                        return false;

                    var indexText = segment.Substring("data[".Length, segment.Length - "data[".Length - 1);
                    return int.TryParse(indexText, out index);
                }

                private static Type GetElementType(Type collectionType)
                {
                    if (collectionType.IsArray)
                        return collectionType.GetElementType();

                    if (collectionType.IsGenericType)
                    {
                        var genericArguments = collectionType.GetGenericArguments();
                        if (genericArguments.Length == 1 && typeof(IList).IsAssignableFrom(collectionType))
                            return genericArguments[0];
                    }

                    foreach (var implementedInterface in collectionType.GetInterfaces())
                    {
                        if (!implementedInterface.IsGenericType)
                            continue;

                        if (implementedInterface.GetGenericTypeDefinition() != typeof(IList<>))
                            continue;

                        return implementedInterface.GetGenericArguments()[0];
                    }

                    return typeof(object);
                }

                private abstract class PathStep
                {
                    public abstract Type ValueType { get; }
                    public abstract object GetValue(object target);
                    public abstract void SetValue(object target, object value);
                }

                private sealed class MemberStep : PathStep
                {
                    [CanBeNull] private readonly FieldInfo _fieldInfo;
                    [CanBeNull] private readonly PropertyInfo _propertyInfo;
                    private readonly Type _valueType;
                    private readonly string _memberName;
                    private readonly Type _declaringType;

                    public MemberStep(Type declaringType, string memberName)
                    {
                        _declaringType = declaringType;
                        _memberName = memberName;

                        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                        _fieldInfo = declaringType.GetField(memberName, flags);

                        if (_fieldInfo != null)
                        {
                            _valueType = _fieldInfo.FieldType;
                            return;
                        }

                        _propertyInfo = declaringType.GetProperty(memberName, flags);

                        if (_propertyInfo != null)
                        {
                            _valueType = _propertyInfo.PropertyType;
                            return;
                        }

                        throw new InvalidOperationException($"No field or property found at path '{memberName}' on type '{declaringType.FullName}'.");
                    }

                    public override Type ValueType => _valueType;

                    public override object GetValue(object target)
                    {
                        if (_fieldInfo != null)
                            return _fieldInfo.GetValue(target);

                        if (_propertyInfo != null)
                            return _propertyInfo.GetValue(target);

                        throw new InvalidOperationException($"No field or property found at path '{_memberName}' on type '{_declaringType.FullName}'.");
                    }

                    public override void SetValue(object target, object value)
                    {
                        if (_fieldInfo != null)
                        {
                            _fieldInfo.SetValue(target, value);
                            return;
                        }

                        if (_propertyInfo != null)
                        {
                            _propertyInfo.SetValue(target, value);
                            return;
                        }

                        throw new InvalidOperationException($"No field or property found at path '{_memberName}' on type '{_declaringType.FullName}'.");
                    }
                }

                private sealed class IndexStep : PathStep
                {
                    private readonly int _index;
                    private readonly Type _valueType;
                    private readonly Type _collectionType;

                    public IndexStep(Type collectionType, int index)
                    {
                        _collectionType = collectionType;
                        _index = index;
                        _valueType = GetElementType(collectionType);
                    }

                    public override Type ValueType => _valueType;

                    public override object GetValue(object target)
                    {
                        if (target is Array array)
                            return array.GetValue(_index);

                        if (target is IList list)
                            return list[_index];

                        throw new InvalidOperationException($"Type '{_collectionType.FullName}' does not support indexed access.");
                    }

                    public override void SetValue(object target, object value)
                    {
                        if (target is Array array)
                        {
                            array.SetValue(value, _index);
                            return;
                        }

                        if (target is IList list)
                        {
                            list[_index] = value;
                            return;
                        }

                        throw new InvalidOperationException($"Type '{_collectionType.FullName}' does not support indexed access.");
                    }
                }
            }
        }
    }
}