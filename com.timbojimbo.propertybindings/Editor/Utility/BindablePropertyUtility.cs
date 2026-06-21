using System;
using System.Collections.Generic;
using System.Linq;
using TimboJimbo.PropertyBindings;
using TimboJimbo.PropertyBindings.Bindings;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace TimboJimboEditor.PropertyBindings.Utility
{
    public static class BindablePropertyUtility
    {
        public static void GetBindableProperties(GameObject target, List<BindableProperty> properties, bool recursive = true)
        {
            if (target == null)
                return;

            using(HashSetPool<BindableProperty>.Get(out var uniqueProperties))
            using(ListPool<BindableProperty>.Get(out var resultBuffer))
            {
                GetBindablePropertiesFromAnimatableBindings(target, recursive, resultBuffer);

                foreach (var prop in resultBuffer)
                {
                    if (uniqueProperties.Add(prop))
                        properties.Add(prop);
                }

                resultBuffer.Clear();

                GetBindablePropertiesFromSerializedObject(target, recursive, resultBuffer);

                foreach (var prop in resultBuffer)
                {
                    if (uniqueProperties.Add(prop))
                        properties.Add(prop);
                }
            }

            FilterBindableProperties(target, properties);
        }

        private static void GetBindablePropertiesFromSerializedObject(
            GameObject root, 
            bool recursive, 
            List<BindableProperty> output,
            Func<BindableProperty, bool> filterOut = null
        )
        {
            output.Clear();

            using (ListPool<Object>.Get(out var targets))
            {
                if (recursive)
                {
                    using var _ = ListPool<Transform>.Get(out var transforms);
                    root.GetComponentsInChildren(results: transforms, includeInactive: true);
                    
                    foreach (var t in transforms)
                    {
                        targets.Add(t.gameObject);
                        targets.AddRange(t.GetComponents<Component>());
                    }
                }
                else
                {
                    targets.Add(root);
                    targets.AddRange(root.GetComponents<Component>());
                }

                foreach (var target in targets)
                {
                    if (target == null)
                        continue;

                    using (var so = new SerializedObject(target))
                    {
                        //top level properties only
                        var sp = so.GetIterator();
                        var enterChildren = true;
                        while (sp.NextVisible(enterChildren))
                        {
                             // only enter children of generic properties (e.g., structs)
                            enterChildren = sp.propertyType == SerializedPropertyType.Generic;

                            if (TryCreateBindableProperty(target, sp.propertyPath, out var bindableProperty))
                            {
                                if (filterOut != null && filterOut(bindableProperty))
                                    continue;

                                output.Add(bindableProperty);
                            }
                        }
                    }
                }
            }

            //ensure they are accessible via reflection (e.g., not internal properties without [SerializeField])
            // There are some fields that show up in SerializedObject but cannot be accessed via reflection, 
            // like ie GameObject m_Name
            output.RemoveAll(bp => !ReflectionPropertyBinding.CanBind(bp));
        }

        private static void GetBindablePropertiesFromAnimatableBindings(
            GameObject root, 
            bool recursive, 
            List<BindableProperty> output
        )
        {
            output.Clear();

            using var _ = ListPool<GameObject>.Get(out var targetGameObjects);
            
            if (recursive)
            {
                using var __ = ListPool<Transform>.Get(out var transforms);

                root.GetComponentsInChildren(results: transforms, includeInactive: true);

                foreach (var t in transforms)
                    targetGameObjects.Add(t.gameObject);
            }
            else
            {
                targetGameObjects.Add(root);
            }

            foreach(var target in targetGameObjects)
            {
                var animatableBindings = AnimationUtility.GetAnimatableBindings(
                    targetObject: target,
                    root: root
                );

                // Group bindings by (target object, base property path).
                // Unity returns composite properties as separate entries like "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z".
                // We use the path prefix (before the component suffix) to group them into a single composite Property.
                using var __ = HashSetPool<(Object bindingTarget, string bindingPath)>.Get(out var uniqueBasePaths);

                foreach (var animatableBinding in animatableBindings)
                {
                    var bindingTarget = AnimationUtility.GetAnimatedObject(
                        root: root, 
                        binding: animatableBinding
                    );
                    
                    if (bindingTarget == null)
                        continue;

                    string bindingPath = GetBasePath(animatableBinding.propertyName);
                    uniqueBasePaths.Add((bindingTarget, bindingPath));
                }

                foreach (var (bindingTarget, bindingPath) in uniqueBasePaths)
                {
                    if (TryCreateBindableProperty(bindingTarget, bindingPath, out var bindableProperty))
                    {
                        output.Add(bindableProperty);
                    }
                }
            }
        }
        
        public static bool TryCreateBindableProperty(Object target, string propertyPath, out BindableProperty bindableProperty)
        {
            if(!TryGetComponentLayout(target, propertyPath, out var valueKind, out var componentLayout))
            {
                bindableProperty = default;
                return false;
            }

            bindableProperty = BindableProperty.CreateWithComponentLayout(
                target: target,
                path: propertyPath,
                kind: valueKind,
                componentOnePath: componentLayout >= ComponentLayout.Two ? (valueKind == ValueKind.Color ? $"{propertyPath}.r" : $"{propertyPath}.x") : null,
                componentTwoPath: componentLayout >= ComponentLayout.Two ? (valueKind == ValueKind.Color ? $"{propertyPath}.g" : $"{propertyPath}.y") : null,
                componentThreePath: componentLayout >= ComponentLayout.Three ? (valueKind == ValueKind.Color ? $"{propertyPath}.b" : $"{propertyPath}.z") : null,
                componentFourPath: componentLayout >= ComponentLayout.Four ? (valueKind == ValueKind.Color ? $"{propertyPath}.a" : $"{propertyPath}.w") : null,
                componentLayout: componentLayout
            );

            return true;
        }

        private static void FilterBindableProperties(GameObject root, List<BindableProperty> properties)
        {
            properties.RemoveAll(CheckIfShouldRemove);
            
            bool CheckIfShouldRemove(BindableProperty property)
            {
                if(property.Target is Component && property.Path == "m_Script")
                    return true;

                // remove m_LocalPosition on RectTransform targets - prefer AnchoredPosition
                if (property.Target is RectTransform && property.Path == "m_LocalPosition")
                    return true;

                // remove m_Color on TMP UGUI components - prefer m_faceColor
                if (property.Target != null && property.Target.GetType().FullName == "TMPro.TextMeshProUGUI" && property.Path == "m_Color")
                    return true;

                // remove  m_FontSizeBase on TMP UGUI components - prefer m_fontSize
                if (property.Target != null && property.Target.GetType().FullName == "TMPro.TextMeshProUGUI" && property.Path == "m_fontSizeBase")
                    return true;

                return false;
            }
        }
       
        /// <summary>
        /// Given a binding propertyName like "m_LocalPosition.x", extracts the base path ("m_LocalPosition").
        /// Returns the original name if it's not a component of a composite.
        /// </summary>
        public static string GetBasePath(string propertyName)
        {
            int lastDot = propertyName.LastIndexOf('.');
            if (lastDot < 0 || lastDot >= propertyName.Length - 1)
                return propertyName;

            string suffix = propertyName.Substring(lastDot + 1);
            if (IsComponentSuffix(suffix))
                return propertyName.Substring(0, lastDot);

            return propertyName;
        }

        /// <summary>
        /// Uses SerializedProperty to determine the ValueKind and ComponentLayout
        /// of a composite property (Vector2/3/4, Quaternion, Color) from its base path.
        /// Returns false if the SerializedProperty could not be found.
        /// </summary>
        private static bool TryGetComponentLayout(Object target, string basePath, out ValueKind kind, out ComponentLayout componentLayout)
        {
            kind = ValueKind.Invalid;
            componentLayout = ComponentLayout.One;

            SerializedProperty sp = null;
            if (target is Component comp)
            {
                var so = new SerializedObject(comp);
                sp = so.FindProperty(basePath);
            }
            else if (target is GameObject go)
            {
                var so = new SerializedObject(go);
                sp = so.FindProperty(basePath);
            }

            if (sp == null)
                return false;

            switch (sp.propertyType)
            {
                case SerializedPropertyType.Vector2:
                    kind = ValueKind.Vector2;
                    componentLayout = ComponentLayout.Two;
                    return true;
                case SerializedPropertyType.Vector3:
                    kind = ValueKind.Vector3;
                    componentLayout = ComponentLayout.Three;
                    return true;
                case SerializedPropertyType.Vector4:
                    kind = ValueKind.Vector4;
                    componentLayout = ComponentLayout.Four;
                    return true;
                case SerializedPropertyType.Quaternion:
                    kind = ValueKind.Quaternion;
                    componentLayout = ComponentLayout.Four;
                    return true;
                case SerializedPropertyType.Color:
                    kind = ValueKind.Color;
                    componentLayout = ComponentLayout.Four;
                    return true;
                case SerializedPropertyType.Integer:
                    kind = ValueKind.Int;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.Boolean:
                    kind = ValueKind.Bool;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.Float:
                    kind = ValueKind.Float;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.ObjectReference:
                    kind = ValueKind.Reference;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.Enum:
                    kind = ValueKind.Enum;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.EntityId:
                    kind = ValueKind.Reference;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.String:
                    kind = ValueKind.String;
                    componentLayout = ComponentLayout.One;
                    return true;
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Hash128:
                case SerializedPropertyType.RenderingLayerMask:
                case SerializedPropertyType.Generic:
                default:
                    return false;
            }
        }

        private static bool IsComponentSuffix(string suffix)
        {
            switch (suffix)
            {
                case "x":
                case "y":
                case "z":
                case "w":
                case "r":
                case "g":
                case "b":
                case "a":
                    return true;
                default:
                    return false;
            }
        }

    }
}