using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Pool;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class GenericPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private readonly BindableProperty _property;
        private readonly GameObject _root;
        private readonly List<GenericBinding> _genericBindings;

        // Bound property arrays from BindProperties
        private NativeArray<BoundProperty> _floatProperties;
        private NativeArray<BoundProperty> _discreteProperties;
        private NativeArray<BoundProperty> _entityIdProperties;

        // Value buffers — sized to match the bound property arrays
        private NativeArray<float> _floatValueBuffer;
        private NativeArray<int> _discreteValueBuffer;
        private NativeArray<EntityId> _entityIdValueBuffer;

        // For each component (index into _genericBindings), the index into the
        // corresponding buffer. -1 means the component doesn't use that buffer type.
        private NativeArray<int> _componentToFloatIndex;
        private NativeArray<int> _componentToDiscreteIndex;
        private NativeArray<int> _componentToEntityIdIndex;

        private bool _disposed;
        private bool _targetMustBeNotifiedOnWrite;

        public static bool CanBind(BindableProperty property) =>
            property.Target is Component &&
            BindableProperty.IsGenericLayoutCompatible(property.Kind, property.ComponentLayout) &&
            property.Kind switch
            {
                ValueKind.Float => true,
                ValueKind.Int => true,
                ValueKind.Bool => true,
                ValueKind.Enum => true,
                ValueKind.Vector2 => true,
                ValueKind.Vector3 => true,
                ValueKind.Vector4 => true,
                ValueKind.Color => true,
                ValueKind.Quaternion => true,
                ValueKind.Reference => true,
                _ => false
            } &&
            !property.Path.Contains('['); // Unitys Generic Binding system does not support arrays

        public GenericPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            _property = property;
            _root = root;

            if (!BindableProperty.IsGenericLayoutCompatible(property.Kind, property.ComponentLayout))
                throw new ArgumentException(
                    $"Generic binding layout mismatch for {Describe(property)}. " +
                    $"Expected {BindableProperty.ExpectedGenericComponentLayout(property.Kind)} for {property.Kind}, but received {property.ComponentLayout}.",
                    nameof(property));
            if (!CanBind(property))
                throw new ArgumentException($"GenericPropertyBinding does not support {Describe(property)}.", nameof(property));

            _genericBindings = new List<GenericBinding>(4);
            _targetMustBeNotifiedOnWrite = property.Target is Component && property.Path != "m_Enabled";

            // Collect paths based on composite type
            using (ListPool<string>.Get(out var paths))
            {
                switch (property.ComponentLayout)
                {
                    case ComponentLayout.One:
                        paths.Add(property.Path);
                        break;

                    case ComponentLayout.Two:
                        paths.Add(property.ComponentOnePath);
                        paths.Add(property.ComponentTwoPath);
                        break;

                    case ComponentLayout.Three:
                        paths.Add(property.ComponentOnePath);
                        paths.Add(property.ComponentTwoPath);
                        paths.Add(property.ComponentThreePath);
                        break;

                    case ComponentLayout.Four:
                        paths.Add(property.ComponentOnePath);
                        paths.Add(property.ComponentTwoPath);
                        paths.Add(property.ComponentThreePath);
                        paths.Add(property.ComponentFourPath);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(property.ComponentLayout), property.ComponentLayout, null);
                }

                foreach (var path in paths)
                {
                    if (!GenericBindingUtility.CreateGenericBinding(property.Target, path, _root, isObjectReference: property.Kind == ValueKind.Reference, out var binding))
                    {
                        throw new InvalidOperationException(
                            $"Failed to create generic binding for property '{path}' on {property.Target}");
                    }

                    _genericBindings.Add(binding);
                }
            }

            try
            {
                int componentCount = _genericBindings.Count;
                if (componentCount == 0)
                    throw new InvalidOperationException($"Generic binding produced no components for {Describe(property)}.");

                var genericBindingsNative = new NativeArray<GenericBinding>(componentCount, Allocator.Temp);
                try
                {
                    for (int i = 0; i < componentCount; i++)
                        genericBindingsNative[i] = _genericBindings[i];

                    GenericBindingUtility.BindProperties(_root, genericBindingsNative,
                        out _floatProperties, out _discreteProperties, out _entityIdProperties, Allocator.Persistent);
                }
                finally
                {
                    genericBindingsNative.Dispose();
                }

                int expectedFloatCount = 0;
                int expectedDiscreteCount = 0;
                int expectedEntityIdCount = 0;
                for (int i = 0; i < componentCount; i++)
                {
                    var binding = _genericBindings[i];
                    bool expectsDiscrete = property.Kind == ValueKind.Enum;
                    bool expectsReference = property.Kind == ValueKind.Reference;
                    bool expectsFloat = !expectsDiscrete && !expectsReference;
                    bool isFloat = !binding.isDiscrete && !binding.isObjectReference;
                    if (binding.isDiscrete != expectsDiscrete ||
                        binding.isObjectReference != expectsReference ||
                        isFloat != expectsFloat)
                    {
                        throw new InvalidOperationException(
                            $"Generic binding component category mismatch for {Describe(property)} at component {i}. Expected " +
                            $"{(expectsDiscrete ? "discrete" : expectsReference ? "reference" : "float")}, but Unity returned " +
                            $"{(binding.isDiscrete ? "discrete" : binding.isObjectReference ? "reference" : "float")}.");
                    }
                    if (binding.isDiscrete) expectedDiscreteCount++;
                    else if (binding.isObjectReference) expectedEntityIdCount++;
                    else expectedFloatCount++;
                }

                int actualFloatCount = _floatProperties.IsCreated ? _floatProperties.Length : 0;
                int actualDiscreteCount = _discreteProperties.IsCreated ? _discreteProperties.Length : 0;
                int actualEntityIdCount = _entityIdProperties.IsCreated ? _entityIdProperties.Length : 0;
                if (actualFloatCount != expectedFloatCount ||
                    actualDiscreteCount != expectedDiscreteCount ||
                    actualEntityIdCount != expectedEntityIdCount)
                {
                    throw new InvalidOperationException(
                        $"Generic binding buffer mismatch for {Describe(property)}. " +
                        $"Expected float/discrete/reference counts {expectedFloatCount}/{expectedDiscreteCount}/{expectedEntityIdCount}, " +
                        $"but Unity returned {actualFloatCount}/{actualDiscreteCount}/{actualEntityIdCount}.");
                }

                if (actualFloatCount > 0)
                    _floatValueBuffer = new NativeArray<float>(actualFloatCount, Allocator.Persistent);
                if (actualDiscreteCount > 0)
                    _discreteValueBuffer = new NativeArray<int>(actualDiscreteCount, Allocator.Persistent);
                if (actualEntityIdCount > 0)
                    _entityIdValueBuffer = new NativeArray<EntityId>(actualEntityIdCount, Allocator.Persistent);

                _componentToFloatIndex = new NativeArray<int>(componentCount, Allocator.Persistent);
                _componentToDiscreteIndex = new NativeArray<int>(componentCount, Allocator.Persistent);
                _componentToEntityIdIndex = new NativeArray<int>(componentCount, Allocator.Persistent);

                int floatIdx = 0, discreteIdx = 0, entityIdIdx = 0;
                for (int i = 0; i < componentCount; i++)
                {
                    var binding = _genericBindings[i];
                    if (binding.isDiscrete)
                    {
                        _componentToFloatIndex[i] = -1;
                        _componentToDiscreteIndex[i] = discreteIdx++;
                        _componentToEntityIdIndex[i] = -1;
                    }
                    else if (binding.isObjectReference)
                    {
                        _componentToFloatIndex[i] = -1;
                        _componentToDiscreteIndex[i] = -1;
                        _componentToEntityIdIndex[i] = entityIdIdx++;
                    }
                    else
                    {
                        _componentToFloatIndex[i] = floatIdx++;
                        _componentToDiscreteIndex[i] = -1;
                        _componentToEntityIdIndex[i] = -1;
                    }
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static string Describe(BindableProperty property)
        {
            string target = property.Target == null
                ? "<missing target>"
                : $"'{property.Target.name}' ({property.Target.GetType().FullName})";
            return $"'{property.Path}' on {target} [kind={property.Kind}, layout={property.ComponentLayout}, " +
                   $"components='{property.ComponentOnePath}', '{property.ComponentTwoPath}', " +
                   $"'{property.ComponentThreePath}', '{property.ComponentFourPath}']";
        }


        protected override bool TargetMustBeNotifiedOnWrite() => _targetMustBeNotifiedOnWrite;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_disposed)
                return false;

            if (_genericBindings.Count == 0)
                return false;

            // Read all bound property values into the scratch buffers
            if (_floatValueBuffer.IsCreated)
                GenericBindingUtility.GetValues(_floatProperties, _floatValueBuffer);
            if (_discreteValueBuffer.IsCreated)
                GenericBindingUtility.GetValues(_discreteProperties, _discreteValueBuffer);
            if (_entityIdValueBuffer.IsCreated)
                GenericBindingUtility.GetValues(_entityIdProperties, _entityIdValueBuffer);

            // Assemble into ValueContainer based on ValueKind
            switch (_property.Kind)
            {
                case ValueKind.Float:
                {
                    int idx = _componentToFloatIndex[0];
                    valueContainer = ValueContainer.FromFloat(_floatValueBuffer[idx]);
                    return true;
                }

                case ValueKind.Int:
                {
                    int idx = _componentToFloatIndex[0];
                    valueContainer = ValueContainer.FromInt((int)_floatValueBuffer[idx]);
                    return true;
                }

                case ValueKind.Bool:
                {
                    int idx = _componentToFloatIndex[0];
                    valueContainer = ValueContainer.FromBool(_floatValueBuffer[idx] > 0.5f);
                    return true;
                }

                case ValueKind.Enum:
                {
                    int idx = _componentToDiscreteIndex[0];
                    valueContainer = ValueContainer.FromEnum(_discreteValueBuffer[idx]);
                    return true;
                }

                case ValueKind.Vector2:
                {
                    int idx0 = _componentToFloatIndex[0];
                    int idx1 = _componentToFloatIndex[1];
                    valueContainer = ValueContainer.FromVector2(
                        new Vector2(_floatValueBuffer[idx0], _floatValueBuffer[idx1])
                    );
                    return true;
                }

                case ValueKind.Vector3:
                {
                    int idx0 = _componentToFloatIndex[0];
                    int idx1 = _componentToFloatIndex[1];
                    int idx2 = _componentToFloatIndex[2];
                    valueContainer = ValueContainer.FromVector3(
                        new Vector3(_floatValueBuffer[idx0], _floatValueBuffer[idx1], _floatValueBuffer[idx2])
                    );
                    return true;
                }

                case ValueKind.Vector4:
                {
                    int idx0 = _componentToFloatIndex[0];
                    int idx1 = _componentToFloatIndex[1];
                    int idx2 = _componentToFloatIndex[2];
                    int idx3 = _componentToFloatIndex[3];
                    valueContainer = ValueContainer.FromVector4(
                        new Vector4(
                            _floatValueBuffer[idx0], 
                            _floatValueBuffer[idx1],
                            _floatValueBuffer[idx2], 
                            _floatValueBuffer[idx3]
                        )
                    );
                    return true;
                }

                case ValueKind.Color:
                {
                    int idx0 = _componentToFloatIndex[0];
                    int idx1 = _componentToFloatIndex[1];
                    int idx2 = _componentToFloatIndex[2];
                    int idx3 = _componentToFloatIndex[3];
                    valueContainer = ValueContainer.FromColor(
                        new Color(
                            _floatValueBuffer[idx0], 
                            _floatValueBuffer[idx1],
                            _floatValueBuffer[idx2], 
                            _floatValueBuffer[idx3]
                        )
                    );
                    return true;
                }

                case ValueKind.Quaternion:
                {
                    int idx0 = _componentToFloatIndex[0];
                    int idx1 = _componentToFloatIndex[1];
                    int idx2 = _componentToFloatIndex[2];
                    int idx3 = _componentToFloatIndex[3];
                    valueContainer = ValueContainer.FromQuaternion(
                        new Quaternion(
                            _floatValueBuffer[idx0], 
                            _floatValueBuffer[idx1],
                            _floatValueBuffer[idx2], 
                            _floatValueBuffer[idx3]
                        )
                    );
                    return true;
                }

                case ValueKind.Reference:
                {
                    int idx = _componentToEntityIdIndex[0];
                    var objectRef = Resources.EntityIdToObject(_entityIdValueBuffer[idx]);
                    valueContainer = ValueContainer.FromReference(objectRef);
                    return true;
                }
                default:
                    return false;
            }
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_disposed)
                return false;

            if (_genericBindings.Count == 0)
                return false;

            // Decompose ValueContainer into buffer values
            switch (_property.Kind)
            {
                case ValueKind.Float:
                {
                    _floatValueBuffer[_componentToFloatIndex[0]] = valueContainer.FloatValue;
                    break;
                }

                case ValueKind.Int:
                {
                    _floatValueBuffer[_componentToFloatIndex[0]] = valueContainer.IntValue;
                    break;
                }

                case ValueKind.Bool:
                {
                    _floatValueBuffer[_componentToFloatIndex[0]] = valueContainer.BoolValue ? 1f : 0f;
                    break;
                }

                case ValueKind.Enum:
                {
                    _discreteValueBuffer[_componentToDiscreteIndex[0]] = valueContainer.EnumValue;
                    break;
                }

                case ValueKind.Vector2:
                {
                    var v = valueContainer.Vector2Value;
                    _floatValueBuffer[_componentToFloatIndex[0]] = v.x;
                    _floatValueBuffer[_componentToFloatIndex[1]] = v.y;
                    break;
                }

                case ValueKind.Vector3:
                {
                    var v = valueContainer.Vector3Value;
                    _floatValueBuffer[_componentToFloatIndex[0]] = v.x;
                    _floatValueBuffer[_componentToFloatIndex[1]] = v.y;
                    _floatValueBuffer[_componentToFloatIndex[2]] = v.z;
                    break;
                }

                case ValueKind.Vector4:
                {
                    var v = valueContainer.Vector4Value;
                    _floatValueBuffer[_componentToFloatIndex[0]] = v.x;
                    _floatValueBuffer[_componentToFloatIndex[1]] = v.y;
                    _floatValueBuffer[_componentToFloatIndex[2]] = v.z;
                    _floatValueBuffer[_componentToFloatIndex[3]] = v.w;
                    break;
                }

                case ValueKind.Color:
                {
                    var c = valueContainer.ColorValue;
                    _floatValueBuffer[_componentToFloatIndex[0]] = c.r;
                    _floatValueBuffer[_componentToFloatIndex[1]] = c.g;
                    _floatValueBuffer[_componentToFloatIndex[2]] = c.b;
                    _floatValueBuffer[_componentToFloatIndex[3]] = c.a;
                    break;
                }

                case ValueKind.Quaternion:
                {
                    var q = valueContainer.QuaternionValue;
                    _floatValueBuffer[_componentToFloatIndex[0]] = q.x;
                    _floatValueBuffer[_componentToFloatIndex[1]] = q.y;
                    _floatValueBuffer[_componentToFloatIndex[2]] = q.z;
                    _floatValueBuffer[_componentToFloatIndex[3]] = q.w;
                    break;
                }

                case ValueKind.Reference:
                {
                    var entityId = valueContainer.ReferenceValue != null ? valueContainer.ReferenceValue.GetEntityId() : EntityId.None;
                    _entityIdValueBuffer[_componentToEntityIdIndex[0]] = entityId;
                    break;
                }

                default:
                    return false;
            }

            // Flush all buffers to the bound properties
            if (_floatValueBuffer.IsCreated)
                GenericBindingUtility.SetValues(_floatProperties, _floatValueBuffer);
            if (_discreteValueBuffer.IsCreated)
                GenericBindingUtility.SetValues(_discreteProperties, _discreteValueBuffer);
            if (_entityIdValueBuffer.IsCreated)
                GenericBindingUtility.SetValues(_entityIdProperties, _entityIdValueBuffer);

            return true;
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            DisposeBoundProperties(ref _floatProperties);
            DisposeBoundProperties(ref _discreteProperties);
            DisposeBoundProperties(ref _entityIdProperties);

            SafeDispose(ref _floatValueBuffer);
            SafeDispose(ref _discreteValueBuffer);
            SafeDispose(ref _entityIdValueBuffer);

            SafeDispose(ref _componentToFloatIndex);
            SafeDispose(ref _componentToDiscreteIndex);
            SafeDispose(ref _componentToEntityIdIndex);
        }

        private static void DisposeBoundProperties(ref NativeArray<BoundProperty> properties)
        {
            if (!properties.IsCreated)
                return;

            GenericBindingUtility.UnbindProperties(properties);
            properties.Dispose();
            properties = default;
        }

        private static void SafeDispose<T>(ref NativeArray<T> array) where T : unmanaged
        {
            if (array.IsCreated)
            {
                array.Dispose();
                array = default;
            }
        }
    }
}
