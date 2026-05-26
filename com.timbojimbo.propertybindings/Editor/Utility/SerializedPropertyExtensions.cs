using System;
using UnityEditor;

namespace TimboJimbo.PropertyBindings.Editor
{
    public static class SerializedPropertyExtensions
    {
        public static ValueContainer GetValueContainer(this SerializedProperty property, ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Bool:
                    return ValueContainer.FromBool(property.boolValue);
                case ValueKind.Int:
                    return ValueContainer.FromInt(property.intValue);
                case ValueKind.Float:
                    return ValueContainer.FromFloat(property.floatValue);
                case ValueKind.Vector2:
                    return ValueContainer.FromVector2(property.vector2Value);
                case ValueKind.Vector3:
                    return ValueContainer.FromVector3(property.vector3Value);
                case ValueKind.Vector4:
                    return ValueContainer.FromVector4(property.vector4Value);
                case ValueKind.Color:
                    return ValueContainer.FromColor(property.colorValue);
                case ValueKind.Quaternion:
                    return ValueContainer.FromQuaternion(property.quaternionValue);
                case ValueKind.Enum:
                    return ValueContainer.FromEnum(property.enumValueIndex);
                case ValueKind.Reference:
                    return ValueContainer.FromReference(property.objectReferenceValue);
                case ValueKind.String:
                    return ValueContainer.FromString(property.stringValue);
                case ValueKind.Invalid:
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}