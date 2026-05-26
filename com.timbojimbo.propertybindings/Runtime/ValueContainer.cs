using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.PropertyBindings
{
    public enum ValueKind
    {
        Invalid,
        Int,
        Float,
        Vector2,
        Vector3,
        Vector4,
        Color,
        Quaternion,
        Bool,
        Enum,
        Reference,
        String,
    }

    public enum RotationInterpolationMode
    {
        [InspectorName("Slerp")]
        QuaternionSlerp,
        [InspectorName("Lerp")]
        QuaternionLerp,
        [InspectorName("Lerp (Euler)")]
        EulerLerp,
    }

    public enum VectorInterpolationMode
    {
        Lerp,
        Slerp,
    }

    public enum ColorInterpolationMode
    {
        [InspectorName("RGB")]
        RGB,
        [InspectorName("HSV")]
        HSV,
        [InspectorName("OkLab")]
        OkLab,
        [InspectorName("OkLCh")]
        OkLCh,
    }

    public enum DiscreteValueSelectionMode
    {
        Nearest,
        LeftSide,
        RightSide,
    }

    [Serializable]
    public struct InterpolationConfig
    {
        public RotationInterpolationMode Rotation;
        public ColorInterpolationMode Color;
        public VectorInterpolationMode Vector2;
        public VectorInterpolationMode Vector3;
    }

    [Serializable]
    public struct LerpConfig
    {
        public InterpolationConfig Interpolation;
        public DiscreteValueSelectionMode DiscreteValueSelection;
    }

    [Serializable]
    public struct ValueContainer : IEquatable<ValueContainer>
    {
        public ValueKind Kind;
        [SerializeField] private Vector4 _floatValue;
        [SerializeField] private int _discreteValue;
        [SerializeField] private Object _referenceValue;
        [SerializeField] private string _stringValue;

        public int IntValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Int, Kind);
                return _discreteValue;
            }
            set
            {
                Clear();
                Kind = ValueKind.Int;
                _discreteValue = value;
            }
        }

        public float FloatValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Float, Kind);
                return _floatValue.x;
            }
            set
            {
                Clear();
                Kind = ValueKind.Float;
                _floatValue.x = value;
            }
        }

        public Vector2 Vector2Value
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Vector2, Kind);
                return new Vector2(_floatValue.x, _floatValue.y);
            }
            set
            {
                Clear();
                Kind = ValueKind.Vector2;
                _floatValue = new Vector4(value.x, value.y, 0f, 0f);
            }
        }

        public Vector3 Vector3Value
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Vector3, Kind);
                return new Vector3(_floatValue.x, _floatValue.y, _floatValue.z);
            }
            set
            {
                Clear();
                Kind = ValueKind.Vector3;
                _floatValue = new Vector4(value.x, value.y, value.z, 0f);
            }
        }

        public Vector4 Vector4Value
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Vector4, Kind);
                return _floatValue;
            }
            set
            {
                Clear();
                Kind = ValueKind.Vector4;
                _floatValue = value;
            }
        }

        public Color ColorValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Color, Kind);
                return new Color(_floatValue.x, _floatValue.y, _floatValue.z, _floatValue.w);
            }
            set
            {
                Clear();
                Kind = ValueKind.Color;
                _floatValue = new Vector4(value.r, value.g, value.b, value.a);
            }
        }

        public Quaternion QuaternionValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Quaternion, Kind);
                return new Quaternion(_floatValue.x, _floatValue.y, _floatValue.z, _floatValue.w);
            }
            set
            {
                Clear();
                Kind = ValueKind.Quaternion;
                _floatValue = new Vector4(value.x, value.y, value.z, value.w);
            }
        }

        public bool BoolValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Bool, Kind);
                return _discreteValue != 0;
            }
            set
            {
                Clear();
                Kind = ValueKind.Bool;
                _discreteValue = value ? 1 : 0;
            }
        }

        public int EnumValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Enum, Kind);
                return _discreteValue;
            }
            set
            {
                Clear();
                Kind = ValueKind.Enum;
                _discreteValue = value;
            }
        }

        public Object ReferenceValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.Reference, Kind);
                return _referenceValue;
            }
            set
            {
                Clear();
                Kind = ValueKind.Reference;
                _referenceValue = value;
            }
        }

        public string StringValue
        {
            get
            {
                PropBindingsAssert.AreEqual(ValueKind.String, Kind);
                return _stringValue;
            }
            set
            {
                Clear();
                Kind = ValueKind.String;
                _stringValue = value;
            }
        }

        public static ValueContainer FromInt(int value) => new ValueContainer { IntValue = value };
        public static ValueContainer FromFloat(float value) => new ValueContainer { FloatValue = value };
        public static ValueContainer FromBool(bool value) => new ValueContainer { BoolValue = value };
        public static ValueContainer FromEnum<TEnum>(TEnum value) where TEnum : Enum => FromEnum(Convert.ToInt32(value));
        public static ValueContainer FromEnum(int enumValue) => new ValueContainer { EnumValue = enumValue };
        public static ValueContainer FromReference(Object value) => new ValueContainer { ReferenceValue = value };
        public static ValueContainer FromString(string value) => new ValueContainer { StringValue = value };

        public static ValueContainer FromVector2(Vector2 value)
            => new ValueContainer { Vector2Value = value };

        public static ValueContainer FromVector3(Vector3 value)
            => new ValueContainer { Vector3Value = value };

        public static ValueContainer FromVector4(Vector4 value) => new ValueContainer { Vector4Value = value };

        public static ValueContainer FromColor(Color value)
            => new ValueContainer { ColorValue = value };

        public static ValueContainer FromQuaternion(Quaternion value)
            => new ValueContainer { QuaternionValue = value };

        public static ValueContainer FromDefault(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Int:
                    return FromInt(0);
                case ValueKind.Float:
                    return FromFloat(0f);
                case ValueKind.Bool:
                    return FromBool(false);
                case ValueKind.Enum:
                    return FromEnum(0);
                case ValueKind.Reference:
                    return FromReference(null);
                case ValueKind.String:
                    return FromString(string.Empty);
                case ValueKind.Vector2:
                    return FromVector2(Vector2.zero);
                case ValueKind.Vector3:
                    return FromVector3(Vector3.zero);
                case ValueKind.Vector4:
                    return FromVector4(Vector4.zero);
                case ValueKind.Color:
                    return FromColor(Color.black);
                case ValueKind.Quaternion:
                    return FromQuaternion(Quaternion.identity);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static ValueContainer Lerp(in ValueContainer from, in ValueContainer to, float t, in LerpConfig config = default)
        {
            PropBindingsAssert.AreEqual(from.Kind, to.Kind);

            from.Clear();

            switch (from.Kind)
            {
                case ValueKind.Float:
                    return new ValueContainer { FloatValue = Mathf.Lerp(from.FloatValue, to.FloatValue, t) };
                case ValueKind.Color:
                    switch (config.Interpolation.Color)
                    {
                        case ColorInterpolationMode.RGB:
                            return new ValueContainer { ColorValue = Color.Lerp(from.ColorValue, to.ColorValue, t) };
                        case ColorInterpolationMode.HSV:
                            return new ValueContainer { ColorValue = ColorExtra.LerpHSV(from.ColorValue, to.ColorValue, t) };
                        case ColorInterpolationMode.OkLab:
                            return new ValueContainer { ColorValue = ColorExtra.LerpOkLab(from.ColorValue, to.ColorValue, t) };
                        case ColorInterpolationMode.OkLCh:
                            return new ValueContainer { ColorValue = ColorExtra.LerpOkLCh(from.ColorValue, to.ColorValue, t) };
                        default:
                            throw new NotImplementedException($"Unsupported color interpolation mode: {config.Interpolation.Color}");
                    }
                case ValueKind.Quaternion:
                    switch (config.Interpolation.Rotation)
                    {
                        case RotationInterpolationMode.EulerLerp:
                            {
                                Vector3 fromEuler = from.QuaternionValue.eulerAngles;
                                Vector3 toEuler = to.QuaternionValue.eulerAngles;

                                for (int i = 0; i < 3; i++)
                                {
                                    if (Mathf.Abs(toEuler[i] - fromEuler[i]) > 180f)
                                    {
                                        if (toEuler[i] > fromEuler[i])
                                            fromEuler[i] += 360f;
                                        else
                                            toEuler[i] += 360f;
                                    }
                                }

                                Vector3 euler = Vector3.Lerp(fromEuler, toEuler, t);
                                return new ValueContainer { QuaternionValue = Quaternion.Euler(euler) };
                            }
                        case RotationInterpolationMode.QuaternionLerp:
                            return new ValueContainer { QuaternionValue = Quaternion.Lerp(from.QuaternionValue, to.QuaternionValue, t) };
                        case RotationInterpolationMode.QuaternionSlerp:
                            return new ValueContainer { QuaternionValue = Quaternion.Slerp(from.QuaternionValue, to.QuaternionValue, t) };
                        default:
                            throw new NotImplementedException($"Unsupported rotation interpolation mode: {config.Interpolation.Rotation}");
                    }
                case ValueKind.Vector2:
                    switch (config.Interpolation.Vector2)
                    {
                        case VectorInterpolationMode.Lerp:
                            return new ValueContainer { Vector2Value = Vector2.Lerp(from.Vector2Value, to.Vector2Value, t) };
                        case VectorInterpolationMode.Slerp:
                            return new ValueContainer { Vector2Value = Vector3.Slerp(from.Vector2Value, to.Vector2Value, t) }; // Vector2 doesn't have a slerp..? or...sircular..lerp...?!
                        default:
                            throw new NotImplementedException($"Unsupported vector interpolation mode: {config.Interpolation.Vector2}");
                    }
                case ValueKind.Vector3:
                    switch (config.Interpolation.Vector3)
                    {
                        case VectorInterpolationMode.Lerp:
                            return new ValueContainer { Vector3Value = Vector3.Lerp(from.Vector3Value, to.Vector3Value, t) };
                        case VectorInterpolationMode.Slerp:
                            return new ValueContainer { Vector3Value = Vector3.Slerp(from.Vector3Value, to.Vector3Value, t) };
                        default:
                            throw new NotImplementedException($"Unsupported vector interpolation mode: {config.Interpolation.Vector3}");
                    }

                case ValueKind.Vector4:
                    return new ValueContainer { Vector4Value = Vector4.Lerp(from.Vector4Value, to.Vector4Value, t) };

                default:
                    return config.DiscreteValueSelection switch
                    {
                        DiscreteValueSelectionMode.Nearest => t < 0.5f ? from : to,
                        DiscreteValueSelectionMode.LeftSide => from,
                        DiscreteValueSelectionMode.RightSide => to,
                        _ => throw new NotImplementedException($"Unsupported discrete value selection mode: {config.DiscreteValueSelection}")
                    };
            }
        }

        public static ValueContainer Add(ValueContainer a, ValueContainer b)
        {
            PropBindingsAssert.AreEqual(a.Kind, b.Kind);

            switch (a.Kind)
            {
                case ValueKind.Int:
                    return FromInt(a.IntValue + b.IntValue);
                case ValueKind.Float:
                    return FromFloat(a.FloatValue + b.FloatValue);
                case ValueKind.Vector2:
                    return FromVector2(a.Vector2Value + b.Vector2Value);
                case ValueKind.Vector3:
                    return FromVector3(a.Vector3Value + b.Vector3Value);
                case ValueKind.Vector4:
                    return FromVector4(a.Vector4Value + b.Vector4Value);
                case ValueKind.Color:
                    return FromColor(a.ColorValue + b.ColorValue);
                case ValueKind.Quaternion:
                    return FromQuaternion(a.QuaternionValue * b.QuaternionValue);
                default:
                    throw new NotSupportedException($"Add is not supported for {a.Kind}");
            }
        }

        public static ValueContainer Subtract(ValueContainer a, ValueContainer b)
        {
            PropBindingsAssert.AreEqual(a.Kind, b.Kind);

            switch (a.Kind)
            {
                case ValueKind.Int:
                    return FromInt(a.IntValue - b.IntValue);
                case ValueKind.Float:
                    return FromFloat(a.FloatValue - b.FloatValue);
                case ValueKind.Vector2:
                    return FromVector2(a.Vector2Value - b.Vector2Value);
                case ValueKind.Vector3:
                    return FromVector3(a.Vector3Value - b.Vector3Value);
                case ValueKind.Vector4:
                    return FromVector4(a.Vector4Value - b.Vector4Value);
                case ValueKind.Color:
                    return FromColor(a.ColorValue - b.ColorValue);
                case ValueKind.Quaternion:
                    return FromQuaternion(a.QuaternionValue * Quaternion.Inverse(b.QuaternionValue));
                default:
                    throw new NotSupportedException($"Subtract is not supported for {a.Kind}");
            }
        }

        private void Clear()
        {
            Kind = default;
            _floatValue = default;
            _discreteValue = default;
            _referenceValue = default;
            _stringValue = default;
        }

        public bool ApproximatelyEquals(ValueContainer other, float tolerance = 0.0001f)
        {
            if (Kind != other.Kind)
                return false;

            switch (Kind)
            {
                case ValueKind.Int:
                    return IntValue == other.IntValue;
                case ValueKind.Float:
                    return Mathf.Abs(FloatValue - other.FloatValue) <= tolerance;
                case ValueKind.Vector2:
                    return Vector2.Distance(Vector2Value, other.Vector2Value) <= tolerance;
                case ValueKind.Vector3:
                    return Vector3.Distance(Vector3Value, other.Vector3Value) <= tolerance;
                case ValueKind.Vector4:
                    return Vector4.Distance(Vector4Value, other.Vector4Value) <= tolerance;
                case ValueKind.Color:
                    return Vector4.Distance(ColorValue, other.ColorValue) <= tolerance;
                case ValueKind.Quaternion:
                    // for quaternions, we can check the angle between them
                    float angle = Quaternion.Angle(QuaternionValue, other.QuaternionValue);
                    return angle <= tolerance * 360f; // convert tolerance to degrees
                case ValueKind.Bool:
                    return BoolValue == other.BoolValue;
                case ValueKind.Enum:
                    return EnumValue == other.EnumValue;
                case ValueKind.Reference:
                    return ReferenceEquals(ReferenceValue, other.ReferenceValue);
                case ValueKind.String:
                    return string.Equals(StringValue, other.StringValue, StringComparison.OrdinalIgnoreCase);
                default:
                    return false; // if Kind is None or unrecognized, consider them not approximately equal
            }
        }

        public bool Equals(ValueContainer other)
        {
            if (Kind != other.Kind)
                return false;

            switch (Kind)
            {
                case ValueKind.Int:
                    return IntValue == other.IntValue;
                case ValueKind.Float:
                    return Mathf.Approximately(FloatValue, other.FloatValue);
                case ValueKind.Vector2:
                    return Vector2Value == other.Vector2Value;
                case ValueKind.Vector3:
                    return Vector3Value == other.Vector3Value;
                case ValueKind.Vector4:
                    return Vector4Value == other.Vector4Value;
                case ValueKind.Color:
                    return ColorValue == other.ColorValue;
                case ValueKind.Quaternion:
                    return QuaternionValue == other.QuaternionValue;
                case ValueKind.Bool:
                    return BoolValue == other.BoolValue;
                case ValueKind.Enum:
                    return EnumValue == other.EnumValue;
                case ValueKind.Reference:
                    return ReferenceEquals(ReferenceValue, other.ReferenceValue);
                case ValueKind.String:
                    return StringValue == other.StringValue;
                default:
                    return false; // if Kind is None or unrecognized, consider them not equal
            }
        }

        public override bool Equals(object obj) => obj is ValueContainer other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ _floatValue.GetHashCode();
                hash = (hash * 397) ^ _discreteValue;
                hash = (hash * 397) ^ (_referenceValue != null ? _referenceValue.GetHashCode() : 0);
                hash = (hash * 397) ^ (_stringValue != null ? _stringValue.GetHashCode() : 0);
                return hash;
            }
        }

        public static bool operator ==(ValueContainer left, ValueContainer right) => left.Equals(right);

        public static bool operator !=(ValueContainer left, ValueContainer right) => !left.Equals(right);
    }
}
