using System;
using System.Collections.Generic;
using TimboJimbo.PropertyBindings;
using TimboJimboEditor.PropertyBindings.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace TimboJimboEditor.PropertyBindings
{
    [CustomPropertyDrawer(typeof(BindableProperty))]
    public sealed class BindablePropertyDrawer : PropertyDrawer
    {
        private const float RowSpacing = 2f;
        private static readonly Dictionary<(Object target, string propertyPath), BindableProperty> PendingSelections = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProp = property.FindPropertyRelative("_target");
            var descriptorIdProp = property.FindPropertyRelative("_descriptorId");
            var pathProp = property.FindPropertyRelative("_path");
            var kindProp = property.FindPropertyRelative("_kind");
            var componentLayoutProp = property.FindPropertyRelative("_componentLayout");
            var componentOnePathProp = property.FindPropertyRelative("_componentOnePath");
            var componentTwoPathProp = property.FindPropertyRelative("_componentTwoPath");
            var componentThreePathProp = property.FindPropertyRelative("_componentThreePath");
            var componentFourPathProp = property.FindPropertyRelative("_componentFourPath");

            if (
                targetProp == null || descriptorIdProp == null || pathProp == null || kindProp == null || componentLayoutProp == null ||
                componentOnePathProp == null || componentTwoPathProp == null || componentThreePathProp == null || componentFourPathProp == null
            )
            {
                EditorGUI.LabelField(position, label, new GUIContent("Invalid BindableProperty layout"));
                return;
            }

            var lineHeight = EditorGUIUtility.singleLineHeight;
            var line1 = new Rect(position.x, position.y, position.width, lineHeight);
            var line2 = new Rect(position.x, position.y + lineHeight + RowSpacing, position.width, lineHeight);

            EditorGUI.BeginProperty(position, label, property);

            var targetKey = (property.serializedObject.targetObject, property.propertyPath);
            if (PendingSelections.TryGetValue(targetKey, out var pendingSelection))
            {
                WriteBindableProperty(
                    targetProp,
                    descriptorIdProp,
                    pathProp,
                    kindProp,
                    componentLayoutProp,
                    componentOnePathProp,
                    componentTwoPathProp,
                    componentThreePathProp,
                    componentFourPathProp,
                    pendingSelection
                );
                PendingSelections.Remove(targetKey);
            }

            var fieldId = GUIUtility.GetControlID(FocusType.Passive);
            line1 = EditorGUI.PrefixLabel(line1, fieldId, label);
            line2.xMin = line1.xMin;

            using (new EditorGUI.MixedValueScope(property.hasMultipleDifferentValues || targetProp.hasMultipleDifferentValues))
            {
                var currentGo = GetTargetGameObject(targetProp.objectReferenceValue);
                EditorGUI.BeginChangeCheck();
                var newGo = (GameObject)EditorGUI.ObjectField(line1, new GUIContent("Target"), currentGo, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    targetProp.objectReferenceValue = newGo;
                    ClearBinding(descriptorIdProp, pathProp, kindProp, componentLayoutProp, componentOnePathProp, componentTwoPathProp, componentThreePathProp, componentFourPathProp);
                }
            }

            line2 = EditorGUI.PrefixLabel(line2, fieldId, new GUIContent("Property"));
            var buttonLabel = GetPropertyButtonLabel(targetProp.objectReferenceValue, pathProp.stringValue, property.hasMultipleDifferentValues);

            using (new EditorGUI.DisabledScope(property.hasMultipleDifferentValues || GetTargetGameObject(targetProp.objectReferenceValue) == null))
            {
                if (EditorGUI.DropdownButton(line2, buttonLabel, FocusType.Passive))
                {
                    var targetGo = GetTargetGameObject(targetProp.objectReferenceValue);
                    if (targetGo != null)
                    {
                        var current = ReadCurrent(property.hasMultipleDifferentValues, targetProp, descriptorIdProp, pathProp, kindProp, componentLayoutProp,
                            componentOnePathProp, componentTwoPathProp, componentThreePathProp, componentFourPathProp);

                        PopupWindow.Show(line2, new BindablePropertyPickerPopup(targetGo, current, selected =>
                        {
                            PendingSelections[targetKey] = selected;
                        }));
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight * 2f) + RowSpacing;
        }

        private static void ClearBinding(
            SerializedProperty descriptorIdProp,
            SerializedProperty pathProp,
            SerializedProperty kindProp,
            SerializedProperty componentLayoutProp,
            SerializedProperty componentOnePathProp,
            SerializedProperty componentTwoPathProp,
            SerializedProperty componentThreePathProp,
            SerializedProperty componentFourPathProp)
        {
            descriptorIdProp.stringValue = string.Empty;
            pathProp.stringValue = string.Empty;
            kindProp.enumValueIndex = (int)ValueKind.Invalid;
            componentLayoutProp.enumValueIndex = (int)ComponentLayout.One;
            componentOnePathProp.stringValue = string.Empty;
            componentTwoPathProp.stringValue = string.Empty;
            componentThreePathProp.stringValue = string.Empty;
            componentFourPathProp.stringValue = string.Empty;
        }

        private static void WriteBindableProperty(
            SerializedProperty targetProp,
            SerializedProperty descriptorIdProp,
            SerializedProperty pathProp,
            SerializedProperty kindProp,
            SerializedProperty componentLayoutProp,
            SerializedProperty componentOnePathProp,
            SerializedProperty componentTwoPathProp,
            SerializedProperty componentThreePathProp,
            SerializedProperty componentFourPathProp,
            BindableProperty bindableProperty)
        {
            targetProp.objectReferenceValue = bindableProperty.Target;
            descriptorIdProp.stringValue = bindableProperty.DescriptorId ?? string.Empty;
            bool isAdHoc = bindableProperty.IsAdHoc;
            pathProp.stringValue = isAdHoc ? bindableProperty.Path : string.Empty;
            kindProp.enumValueIndex = (int)(isAdHoc ? bindableProperty.Kind : ValueKind.Invalid);
            componentLayoutProp.enumValueIndex = (int)(isAdHoc ? bindableProperty.ComponentLayout : ComponentLayout.One);
            componentOnePathProp.stringValue = isAdHoc ? bindableProperty.ComponentOnePath ?? string.Empty : string.Empty;
            componentTwoPathProp.stringValue = isAdHoc ? bindableProperty.ComponentTwoPath ?? string.Empty : string.Empty;
            componentThreePathProp.stringValue = isAdHoc ? bindableProperty.ComponentThreePath ?? string.Empty : string.Empty;
            componentFourPathProp.stringValue = isAdHoc ? bindableProperty.ComponentFourPath ?? string.Empty : string.Empty;
        }

        private static BindableProperty ReadCurrent(
            bool isMixed,
            SerializedProperty targetProp,
            SerializedProperty descriptorIdProp,
            SerializedProperty pathProp,
            SerializedProperty kindProp,
            SerializedProperty componentLayoutProp,
            SerializedProperty componentOnePathProp,
            SerializedProperty componentTwoPathProp,
            SerializedProperty componentThreePathProp,
            SerializedProperty componentFourPathProp)
        {
            if (isMixed)
                return BindableProperty.Invalid;

            var target = targetProp.objectReferenceValue;
            if (target == null)
                return BindableProperty.Invalid;

            var kind = (ValueKind)kindProp.enumValueIndex;
            var layout = (ComponentLayout)componentLayoutProp.enumValueIndex;
            var c1 = componentOnePathProp.stringValue;
            var c2 = componentTwoPathProp.stringValue;
            var c3 = componentThreePathProp.stringValue;
            var c4 = componentFourPathProp.stringValue;

            try
            {
                if (PropertyDescriptorRegistry.TryGet(descriptorIdProp.stringValue, out var descriptor) &&
                    descriptor.SupportsTarget(target))
                    return descriptor.Create(target);

                if (string.IsNullOrEmpty(pathProp.stringValue)) return BindableProperty.Invalid;
                return BindableProperty.CreateAdHoc(target, pathProp.stringValue, kind, layout, c1, c2, c3, c4);
            }
            catch
            {
                return BindableProperty.Invalid;
            }
        }

        private static GUIContent GetPropertyButtonLabel(Object targetObject, string path, bool isMixed)
        {
            if (isMixed)
                return new GUIContent("—");

            if (targetObject == null)
                return new GUIContent("Select target first");

            if (string.IsNullOrEmpty(path))
                return new GUIContent("Select property...");

            var componentName = GetComponentLabel(targetObject);
            var propertyName = NicifyPropertyPath(path);
            var iconContent = EditorGUIUtility.ObjectContent(targetObject, targetObject.GetType());
            return new GUIContent($"{componentName} > {propertyName}", iconContent != null ? iconContent.image : null);
        }

        private static string GetComponentLabel(Object targetObject)
        {
            if (targetObject is GameObject)
                return "GameObject";

            if (targetObject == null)
                return "(Missing)";

            return ObjectNames.NicifyVariableName(targetObject.GetType().Name);
        }

        private static string NicifyPropertyPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "(None)";

            var basePath = BindablePropertyUtility.GetBasePath(path);
            var lastDot = basePath.LastIndexOf('.');
            var leaf = lastDot >= 0 ? basePath.Substring(lastDot + 1) : basePath;
            if (leaf.StartsWith("m_", StringComparison.Ordinal))
                leaf = leaf.Substring(2);
            return ObjectNames.NicifyVariableName(leaf);
        }

        private static GameObject GetTargetGameObject(Object targetObject)
        {
            return targetObject switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => null
            };
        }

        private sealed class BindablePropertyPickerPopup : PopupWindowContent
        {
            private readonly GameObject _target;
            private readonly BindableProperty _current;
            private readonly Action<BindableProperty> _onSelected;

            private readonly List<Entry> _entries = new List<Entry>();
            private Vector2 _scroll;
            private string _search = string.Empty;

            public BindablePropertyPickerPopup(GameObject target, BindableProperty current, Action<BindableProperty> onSelected)
            {
                _target = target;
                _current = current;
                _onSelected = onSelected;
                RebuildEntries();
            }

            public override Vector2 GetWindowSize() => new Vector2(420f, 360f);

            public override void OnOpen()
            {
                editorWindow.wantsMouseMove = true;
            }

            public override void OnGUI(Rect rect)
            {
                DrawHeader(rect);

                var listRect = new Rect(rect.x + 6f, rect.y + 30f, rect.width - 12f, rect.height - 36f);
                using var scope = new GUILayout.AreaScope(listRect);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                string currentGroup = null;
                bool anyVisible = false;
                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];
                    if (!MatchesSearch(entry))
                        continue;

                    anyVisible = true;
                    if (!string.Equals(currentGroup, entry.Group, StringComparison.Ordinal))
                    {
                        currentGroup = entry.Group;
                        GUILayout.Space(4f);
                        EditorGUILayout.LabelField(currentGroup, EditorStyles.boldLabel);
                    }

                    DrawEntryButton(entry);
                }

                if (!anyVisible)
                    EditorGUILayout.HelpBox("No matching properties found.", MessageType.Info);

                EditorGUILayout.EndScrollView();
            }

            private void DrawHeader(Rect rect)
            {
                var searchRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, 18f);
                EditorGUI.BeginChangeCheck();
                var next = EditorGUI.TextField(searchRect, _search, EditorStyles.toolbarSearchField);
                if (EditorGUI.EndChangeCheck())
                {
                    _search = next ?? string.Empty;
                    _scroll = Vector2.zero;
                }
            }

            private void DrawEntryButton(Entry entry)
            {
                var buttonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                var isCurrent = entry.Property == _current;

                if (isCurrent)
                    EditorGUI.DrawRect(buttonRect, new Color(0.24f, 0.44f, 0.82f, 0.25f));

                var content = new GUIContent(entry.Display, entry.Icon);
                if (GUI.Button(buttonRect, content, EditorStyles.label))
                {
                    _onSelected?.Invoke(entry.Property);
                    editorWindow.Close();
                }
            }

            private bool MatchesSearch(Entry entry)
            {
                if (string.IsNullOrWhiteSpace(_search))
                    return true;

                var token = _search.Trim();
                return entry.SearchKey.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private void RebuildEntries()
            {
                _entries.Clear();
                if (_target == null)
                    return;

                using var _ = ListPool<BindableProperty>.Get(out var properties);
                BindablePropertyUtility.GetBindableProperties(_target, properties, recursive: false);

                var labelsByTarget = BuildTargetLabels(_target);

                for (var i = 0; i < properties.Count; i++)
                {
                    var p = properties[i];
                    if (p.Target == null || string.IsNullOrEmpty(p.Path))
                        continue;

                    var group = labelsByTarget.TryGetValue(p.Target, out var targetLabel)
                        ? targetLabel
                        : ObjectNames.NicifyVariableName(p.Target.GetType().Name);

                    var propertyName = NicifyPropertyPath(p.Path);
                    var componentName = GetComponentLabel(p.Target);
                    var display = $"{componentName} > {propertyName}";
                    var search = $"{group} {componentName} {propertyName} {p.Path}";
                    var iconContent = EditorGUIUtility.ObjectContent(p.Target, p.Target.GetType());

                    _entries.Add(new Entry
                    {
                        Group = group,
                        Display = display,
                        SearchKey = search,
                        Icon = iconContent?.image,
                        Property = p,
                    });
                }

                _entries.Sort((a, b) =>
                {
                    var groupCompare = string.Compare(a.Group, b.Group, StringComparison.Ordinal);
                    if (groupCompare != 0)
                        return groupCompare;

                    return string.Compare(a.Display, b.Display, StringComparison.Ordinal);
                });
            }

            private static Dictionary<Object, string> BuildTargetLabels(GameObject gameObject)
            {
                var result = new Dictionary<Object, string>();
                result[gameObject] = GetComponentLabel(gameObject);

                using var _ = DictionaryPool<Type, int>.Get(out var countsByType);
                var components = gameObject.GetComponents<Component>();
                for (var i = 0; i < components.Length; i++)
                {
                    var component = components[i];
                    if (component == null)
                        continue;

                    var type = component.GetType();
                    countsByType.TryGetValue(type, out var count);
                    count++;
                    countsByType[type] = count;

                    var typeLabel = ObjectNames.NicifyVariableName(type.Name);
                    result[component] = count > 1 ? $"{typeLabel} ({count})" : typeLabel;
                }

                return result;
            }

            private sealed class Entry
            {
                public string Group;
                public string Display;
                public string SearchKey;
                public Texture Icon;
                public BindableProperty Property;
            }
        }
    }
}