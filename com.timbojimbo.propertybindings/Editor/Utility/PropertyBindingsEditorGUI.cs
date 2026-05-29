using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.PropertyBindings.Editor.Utility
{
    public static class PropertyBindingsEditorGUI
    {
        public static ValueContainer ValueContainerField(Rect position, BindableProperty bindableProperty, ValueContainer value)
        {
            return ValueContainerField(position, value, bindableProperty);
        }

        public static ValueContainer ValueContainerField(Rect position, GUIContent label, BindableProperty bindableProperty, ValueContainer value)
        {
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2, position.y,
                position.width - EditorGUIUtility.labelWidth - 2, position.height);

            EditorGUI.LabelField(labelRect, label);
            return ValueContainerField(fieldRect, value, bindableProperty);
        }

        private static ValueContainer ValueContainerField(Rect position, ValueContainer value, BindableProperty bindableProperty)
        {
            switch (value.Kind)
            {
                case ValueKind.Int:
                    value.IntValue = EditorGUI.IntField(position, value.IntValue);
                    break;
                case ValueKind.Float:
                    value.FloatValue = EditorGUI.FloatField(position, value.FloatValue);
                    break;
                case ValueKind.Bool:
                    value.BoolValue = EditorGUI.Toggle(position, value.BoolValue);
                    break;
                case ValueKind.Enum:
                    value.EnumValue = EnumField(position, value.EnumValue, bindableProperty);
                    break;
                case ValueKind.Vector2:
                    value.Vector2Value = MultiFloatField(position, value.Vector2Value);
                    break;
                case ValueKind.Vector3:
                    value.Vector3Value = MultiFloatField(position, value.Vector3Value);
                    break;
                case ValueKind.Vector4:
                    value.Vector4Value = MultiFloatField(position, value.Vector4Value);
                    break;
                case ValueKind.Color:
                    value.ColorValue = EditorGUI.ColorField(position, value.ColorValue);
                    break;
                case ValueKind.Quaternion:
                {
                    var euler = value.QuaternionValue.eulerAngles;
                    euler = MultiFloatField(position, euler);
                    value.QuaternionValue = Quaternion.Euler(euler);
                    break;
                }
                case ValueKind.Reference:
                    value.ReferenceValue = EditorGUI.ObjectField(position, value.ReferenceValue, TryGetObjectReferenceType(bindableProperty) ?? typeof(Object), true);
                    break;
                case ValueKind.String:
                    value.StringValue = EditorGUI.TextField(position, value.StringValue);
                    break;
                default:
                    EditorGUI.LabelField(position, "—");
                    break;
            }

            return value;
        }

        private static int EnumField(Rect position, int enumValue, BindableProperty bindableProperty)
        {
            var enumType = TypeResolver.TryGetExactMemberType(bindableProperty);

            if (enumType != null && enumType.IsEnum)
            {
                var meta = EnumMetaResolver.GetEnumMeta(enumType);
                var selectedIndex = meta.ValueToIndex(enumValue);
                selectedIndex = Mathf.Clamp(selectedIndex, 0, meta.DisplayNames.Length - 1);
                var newIndex = EditorGUI.Popup(position, selectedIndex, meta.DisplayNames);
                return meta.IndexToValue(newIndex);
            }

            return EditorGUI.IntField(position, enumValue);
        }

        private static Type TryGetObjectReferenceType(BindableProperty bindableProperty)
        {
            var exactType = TypeResolver.TryGetExactMemberType(bindableProperty);
            return exactType != null && typeof(Object).IsAssignableFrom(exactType) ? exactType : typeof(Object);
        }


        private static readonly GUIContent[] Labels2 = { new GUIContent("X"), new GUIContent("Y") };
        private static readonly GUIContent[] Labels3 = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
        private static readonly GUIContent[] Labels4 = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") };

        private static Vector2 MultiFloatField(Rect position, Vector2 value)
        {
            var values = new float[] { value.x, value.y };
            EditorGUI.MultiFloatField(position, Labels2, values);
            return new Vector2(values[0], values[1]);
        }

        private static Vector3 MultiFloatField(Rect position, Vector3 value)
        {
            var values = new float[] { value.x, value.y, value.z };
            EditorGUI.MultiFloatField(position, Labels3, values);
            return new Vector3(values[0], values[1], values[2]);
        }

        private static Vector4 MultiFloatField(Rect position, Vector4 value)
        {
            var values = new float[] { value.x, value.y, value.z, value.w };
            EditorGUI.MultiFloatField(position, Labels4, values);
            return new Vector4(values[0], values[1], values[2], values[3]);
        }


        private class EnumMeta 
        {
            public string[] DisplayNames;
            public Array Values;

            public EnumMeta(string[] displayNames, Array values)
            {
                DisplayNames = displayNames;
                Values = values;
            }

            public int IndexToValue(int index) => Convert.ToInt32(Values.GetValue(index));
            public int ValueToIndex(int value) 
            {
                for (var i = 0; i < Values.Length; i++)
                {
                    if (Convert.ToInt32(Values.GetValue(i)) == value)
                        return i;
                }
                return 0;
            }
        }

        private static class EnumMetaResolver
        {
            private static readonly System.Collections.Generic.Dictionary<Type, EnumMeta> _cache = new();

            public static EnumMeta GetEnumMeta(Type enumType)
            {
                if (!_cache.TryGetValue(enumType, out var meta))
                {
                    var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
                    var displayNames = new string[fields.Length];
                    var values = Array.CreateInstance(enumType, fields.Length);

                    for (var i = 0; i < fields.Length; i++)
                    {
                        var inspectorName = fields[i].GetCustomAttribute<InspectorNameAttribute>();
                        displayNames[i] = inspectorName != null
                            ? inspectorName.displayName
                            : ObjectNames.NicifyVariableName(fields[i].Name);
                        values.SetValue(fields[i].GetValue(null), i);
                    }

                    meta = new EnumMeta(displayNames, values);
                    _cache[enumType] = meta;
                }
                return meta;
            }
        }

        private static class TypeResolver
        {
            private static readonly System.Collections.Generic.Dictionary<int, Type> _cache = new();

            public static Type TryGetExactMemberType(BindableProperty bindableProperty)
            {
                if (bindableProperty.Target == null || string.IsNullOrEmpty(bindableProperty.Path))
                    return null;

                int key = (bindableProperty.Target.GetType().GetHashCode() * 397) ^ bindableProperty.Path.GetHashCode();
                if (!_cache.TryGetValue(key, out var type))
                {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    var targetType = bindableProperty.Target.GetType();
                    type = targetType.GetField(bindableProperty.Path, flags)?.FieldType
                        ?? targetType.GetProperty(bindableProperty.Path, flags)?.PropertyType;
                    _cache[key] = type;
                }
                return type;
            }
        }
    }

    public static class PropertyBindingsEditorGUILayout
    {
        public static ValueContainer ValueContainerField(BindableProperty bindableProperty, ValueContainer value, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, options);
            return PropertyBindingsEditorGUI.ValueContainerField(rect, bindableProperty, value);
        }

        public static ValueContainer ValueContainerField(string label, BindableProperty bindableProperty, ValueContainer value, params GUILayoutOption[] options)
        {
            return ValueContainerField(new GUIContent(label), bindableProperty, value, options);
        }

        public static ValueContainer ValueContainerField(GUIContent label, BindableProperty bindableProperty, ValueContainer value, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
            return PropertyBindingsEditorGUI.ValueContainerField(rect, label, bindableProperty, value);
        }
    }
}
