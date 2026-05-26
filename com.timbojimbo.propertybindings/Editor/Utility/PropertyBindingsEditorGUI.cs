using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.PropertyBindings.Editor.Utility
{
    public static class PropertyBindingsEditorGUI
    {
        public static ValueContainer ValueContainerField(Rect position, ValueContainer value)
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
                    value.EnumValue = EditorGUI.IntField(position, value.EnumValue);
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
                    value.ReferenceValue = EditorGUI.ObjectField(position, value.ReferenceValue, typeof(Object), true);
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

        public static ValueContainer ValueContainerField(Rect position, string label, ValueContainer value)
        {
            return ValueContainerField(position, new GUIContent(label), value);
        }

        public static ValueContainer ValueContainerField(Rect position, GUIContent label, ValueContainer value)
        {
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2, position.y,
                position.width - EditorGUIUtility.labelWidth - 2, position.height);

            EditorGUI.LabelField(labelRect, label);
            return ValueContainerField(fieldRect, value);
        }
    }

    public static class PropertyBindingsEditorGUILayout
    {
        public static ValueContainer ValueContainerField(ValueContainer value, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, options);
            return PropertyBindingsEditorGUI.ValueContainerField(rect, value);
        }

        public static ValueContainer ValueContainerField(string label, ValueContainer value, params GUILayoutOption[] options)
        {
            return ValueContainerField(new GUIContent(label), value, options);
        }

        public static ValueContainer ValueContainerField(GUIContent label, ValueContainer value, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
            return PropertyBindingsEditorGUI.ValueContainerField(rect, label, value);
        }
    }
}
