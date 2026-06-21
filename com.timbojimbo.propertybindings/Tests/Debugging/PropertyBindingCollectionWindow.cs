#if TJ_PROPERTY_BINDINGS_DEBUG_WINDOWS
using System;
using System.Collections.Generic;
using TimboJimbo.PropertyBindings;
using TimboJimboEditor.PropertyBindings.Utility;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimboTests.PropertyBindings.Debugging
{
    /// <summary>
    /// Editor window to test PropertyBindingCollection — reading, writing, and inspecting binding info.
    /// Open via Window > TimboJimbo > Property Binding Tester.
    /// </summary>
    public sealed class PropertyBindingCollectionWindow : EditorWindow
    {
        private GameObject _target;
        private PropertyBindingCollection _collection;
        private List<BindableProperty> _properties;

        [SerializeField] private TreeViewState<int> _treeViewState;
        [SerializeField] private MultiColumnHeaderState _columnHeaderState;
        private BindingTreeView _treeView;

        private readonly Dictionary<BindableProperty, object> _writeValues = new Dictionary<BindableProperty, object>();
        private readonly Dictionary<BindableProperty, bool> _readResults = new Dictionary<BindableProperty, bool>();

        private const int ColName = 0;
        private const int ColKind = 1;
        private const int ColBinding = 2;
        private const int ColValue = 3;
        private const int ColActions = 4;

        [MenuItem("Window/TimboJimbo/Property Binding Tester")]
        private static void ShowWindow()
        {
            var window = GetWindow<PropertyBindingCollectionWindow>("Property Binding Tester");
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            _treeViewState ??= new TreeViewState<int>();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            DisposeCollection();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.ExitingEditMode)
                DisposeCollection();
        }

        private void DisposeCollection()
        {
            _collection?.Dispose();
            _collection = null;
            _properties = null;
            _treeView = null;
            _writeValues.Clear();
            _readResults.Clear();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Property Binding Collection Tester", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            _target = (GameObject)EditorGUILayout.ObjectField("Target GameObject", _target, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                DisposeCollection();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = _target != null;
                if (GUILayout.Button("Discover & Bind", GUILayout.Height(24)))
                    DiscoverAndBind();

                GUI.enabled = _collection != null;
                if (GUILayout.Button("Dispose Bindings", GUILayout.Height(24)))
                    DisposeCollection();

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (_target == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject in the scene to begin.", MessageType.Info);
                return;
            }

            if (_treeView == null)
            {
                EditorGUILayout.HelpBox("Click 'Discover & Bind' to find animatable properties on the target.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Found {_properties.Count} properties", EditorStyles.miniLabel);
            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView.OnGUI(rect);
        }

        // ──────────────────────────────────────────────
        //  Discovery & Column Setup
        // ──────────────────────────────────────────────

        private MultiColumnHeader CreateColumnHeader()
        {
            var firstInit = _columnHeaderState == null;
            if (firstInit)
            {
                _columnHeaderState = new MultiColumnHeaderState(new[]
                {
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Property"),
                        width = 200, minWidth = 100, autoResize = true, canSort = false
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Kind"),
                        width = 100, minWidth = 60, maxWidth = 160, autoResize = false, canSort = false
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Binding"),
                        width = 140, minWidth = 80, maxWidth = 220, autoResize = false, canSort = false
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Value"),
                        width = 200, minWidth = 60, autoResize = true, canSort = false
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent(""),
                        width = 90, minWidth = 90, maxWidth = 90, autoResize = false, canSort = false
                    },
                });
            }

            var header = new MultiColumnHeader(_columnHeaderState) { canSort = false, height = 22 };
            if (firstInit)
                header.ResizeToFit();
            return header;
        }

        private void DiscoverAndBind()
        {
            DisposeCollection();

            _properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_target, _properties, recursive: true);
            _collection = PropertyBindingCollection.Bind(_target, _properties);

            _treeView = new BindingTreeView(_treeViewState, CreateColumnHeader(), this);
            _treeView.Reload();
            _treeView.ExpandAll();

            Debug.Log($"[BindingTester] Discovered and bound {_properties.Count} properties on '{_target.name}'.");
        }

        // ──────────────────────────────────────────────
        //  Value helpers
        // ──────────────────────────────────────────────

        private static object GetDefaultForKind(ValueKind kind) => kind switch
        {
            ValueKind.Float => 0f,
            ValueKind.Int => 0,
            ValueKind.Bool => false,
            ValueKind.Enum => 0,
            ValueKind.Vector2 => Vector2.zero,
            ValueKind.Vector3 => Vector3.zero,
            ValueKind.Vector4 => Vector4.zero,
            ValueKind.Color => Color.white,
            ValueKind.Quaternion => Quaternion.identity,
            ValueKind.Reference => null,
            ValueKind.String => string.Empty,
            _ => 0f,
        };

        private static object ValueContainerToObject(ValueContainer vc) => vc.Kind switch
        {
            ValueKind.Float => vc.FloatValue,
            ValueKind.Int => vc.IntValue,
            ValueKind.Bool => vc.BoolValue,
            ValueKind.Enum => vc.EnumValue,
            ValueKind.Vector2 => vc.Vector2Value,
            ValueKind.Vector3 => vc.Vector3Value,
            ValueKind.Vector4 => vc.Vector4Value,
            ValueKind.Color => vc.ColorValue,
            ValueKind.Quaternion => vc.QuaternionValue,
            ValueKind.Reference => vc.ReferenceValue,
            ValueKind.String => vc.StringValue,
            _ => 0f,
        };

        private static ValueContainer ObjectToValueContainer(object value, ValueKind kind) => kind switch
        {
            ValueKind.Float => ValueContainer.FromFloat((float)value),
            ValueKind.Int => ValueContainer.FromInt((int)value),
            ValueKind.Bool => ValueContainer.FromBool((bool)value),
            ValueKind.Enum => ValueContainer.FromEnum((int)value),
            ValueKind.Vector2 => ValueContainer.FromVector2((Vector2)value),
            ValueKind.Vector3 => ValueContainer.FromVector3((Vector3)value),
            ValueKind.Vector4 => ValueContainer.FromVector4((Vector4)value),
            ValueKind.Color => ValueContainer.FromColor((Color)value),
            ValueKind.Quaternion => ValueContainer.FromQuaternion((Quaternion)value),
            ValueKind.Reference => ValueContainer.FromReference((Object)value),
            ValueKind.String => ValueContainer.FromString((string)value),
            _ => default,
        };

        // ──────────────────────────────────────────────
        //  TreeView implementation
        // ──────────────────────────────────────────────

        private enum NodeType { GameObject, Component, Property }

        private struct NodeData
        {
            public NodeType Type;
            public BindableProperty Property;
            public Object Target;
        }

        private sealed class BindingTreeView : TreeView<int>
        {
            private readonly PropertyBindingCollectionWindow _window;
            private readonly Dictionary<int, NodeData> _nodes = new Dictionary<int, NodeData>();

            public BindingTreeView(TreeViewState<int> state, MultiColumnHeader header, PropertyBindingCollectionWindow window)
                : base(state, header)
            {
                _window = window;
                showAlternatingRowBackgrounds = true;
                rowHeight = 20f;
            }

            protected override TreeViewItem<int> BuildRoot()
            {
                var root = new TreeViewItem<int>(-1, -1, "Root");
                var items = new List<TreeViewItem<int>>();
                _nodes.Clear();

                int id = 0;

                // Group: GO → (direct props, Component → props)
                var goOrder = new List<EntityId>();
                var goMap = new Dictionary<EntityId, (GameObject go, List<BindableProperty> direct, List<EntityId> compOrder, Dictionary<EntityId, (Component comp, List<BindableProperty> props)> comps)>();

                foreach (var prop in _window._properties)
                {
                    var comp = prop.Target as Component;
                    var go = comp != null ? comp.gameObject : prop.Target as GameObject;
                    var goId = go != null ? go.GetEntityId() : default;

                    if (!goMap.TryGetValue(goId, out var g))
                    {
                        g = (go, new List<BindableProperty>(), new List<EntityId>(), new Dictionary<EntityId, (Component, List<BindableProperty>)>());
                        goMap[goId] = g;
                        goOrder.Add(goId);
                    }

                    if (comp == null)
                    {
                        g.direct.Add(prop);
                    }
                    else
                    {
                        var compId = comp.GetEntityId();
                        if (!g.comps.TryGetValue(compId, out var c))
                        {
                            c = (comp, new List<BindableProperty>());
                            g.comps[compId] = c;
                            g.compOrder.Add(compId);
                        }

                        c.props.Add(prop);
                    }
                }

                foreach (var goId in goOrder)
                {
                    var (go, direct, compOrder, comps) = goMap[goId];

                    var goItemId = id++;
                    items.Add(new TreeViewItem<int>(goItemId, 0, go != null ? go.name : "(unknown)"));
                    _nodes[goItemId] = new NodeData { Type = NodeType.GameObject, Target = go };

                    foreach (var prop in direct)
                    {
                        var pid = id++;
                        items.Add(new TreeViewItem<int>(pid, 1, prop.Path));
                        _nodes[pid] = new NodeData { Type = NodeType.Property, Property = prop };
                    }

                    foreach (var compId in compOrder)
                    {
                        var (comp, props) = comps[compId];
                        var cid = id++;
                        var typeName = comp != null ? ObjectNames.NicifyVariableName(comp.GetType().Name) : "(unknown)";
                        items.Add(new TreeViewItem<int>(cid, 1, $"{typeName} ({props.Count})"));
                        _nodes[cid] = new NodeData { Type = NodeType.Component, Target = comp };

                        foreach (var prop in props)
                        {
                            var pid = id++;
                            items.Add(new TreeViewItem<int>(pid, 2, prop.Path));
                            _nodes[pid] = new NodeData { Type = NodeType.Property, Property = prop };
                        }
                    }
                }

                SetupParentsAndChildrenFromDepths(root, items);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (!_nodes.TryGetValue(args.item.id, out var data))
                    return;

                for (int i = 0; i < args.GetNumVisibleColumns(); i++)
                {
                    var cellRect = args.GetCellRect(i);
                    var col = args.GetColumn(i);
                    CenterRectUsingSingleLineHeight(ref cellRect);
                    DrawCell(cellRect, args.item, col, ref data);
                }
            }

            private void DrawCell(Rect rect, TreeViewItem<int> item, int col, ref NodeData data)
            {
                switch (col)
                {
                    case ColName:
                        DrawNameCell(rect, item, ref data);
                        break;

                    case ColKind:
                        if (data.Type == NodeType.Property)
                            EditorGUI.LabelField(rect, data.Property.Kind.ToString(), EditorStyles.miniLabel);
                        break;

                    case ColBinding:
                        if (data.Type == NodeType.Property && _window._collection != null
                            && _window._collection.TryGetBindingType(data.Property, out var bindingType))
                            EditorGUI.LabelField(rect, bindingType.Name, EditorStyles.miniLabel);
                        break;

                    case ColValue:
                        if (data.Type == NodeType.Property)
                            DrawValueCell(rect, data.Property);
                        break;

                    case ColActions:
                        if (data.Type == NodeType.Property)
                            DrawActionsCell(rect, data.Property);
                        break;
                }
            }

            private void DrawNameCell(Rect rect, TreeViewItem<int> item, ref NodeData data)
            {
                var indent = GetContentIndent(item);
                rect.xMin += indent;

                if (data.Type != NodeType.Property && data.Target != null)
                {
                    var type = data.Type == NodeType.GameObject ? typeof(GameObject) : data.Target.GetType();
                    var icon = EditorGUIUtility.ObjectContent(data.Target, type).image;
                    if (icon != null)
                    {
                        var iconRect = new Rect(rect.x, rect.y, 16, rect.height);
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                        rect.xMin += 18;
                    }
                }

                var style = data.Type == NodeType.GameObject ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUI.LabelField(rect, item.displayName, style);
            }

            private void DrawValueCell(Rect rect, BindableProperty property)
            {
                if (!_window._writeValues.TryGetValue(property, out var val))
                {
                    val = GetDefaultForKind(property.Kind);
                    _window._writeValues[property] = val;
                }

                if (_window._readResults.TryGetValue(property, out var ok) && !ok)
                {
                    var prev = GUI.color;
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    EditorGUI.LabelField(rect, "read failed", EditorStyles.miniLabel);
                    GUI.color = prev;
                    return;
                }

                object newVal;
                switch (property.Kind)
                {
                    case ValueKind.Float:
                        newVal = EditorGUI.FloatField(rect, val is float f ? f : 0f);
                        break;
                    case ValueKind.Int:
                        newVal = EditorGUI.IntField(rect, val is int i ? i : 0);
                        break;
                    case ValueKind.Bool:
                        newVal = EditorGUI.Toggle(rect, val is bool b && b);
                        break;
                    case ValueKind.Enum:
                        newVal = EditorGUI.IntField(rect, val is int ei ? ei : 0);
                        break;
                    case ValueKind.Vector2:
                        newVal = EditorGUI.Vector2Field(rect, GUIContent.none, val is Vector2 v2 ? v2 : Vector2.zero);
                        break;
                    case ValueKind.Vector3:
                        newVal = EditorGUI.Vector3Field(rect, GUIContent.none, val is Vector3 v3 ? v3 : Vector3.zero);
                        break;
                    case ValueKind.Vector4:
                        newVal = EditorGUI.Vector4Field(rect, GUIContent.none, val is Vector4 v4 ? v4 : Vector4.zero);
                        break;
                    case ValueKind.Color:
                        newVal = EditorGUI.ColorField(rect, val is Color c ? c : Color.white);
                        break;
                    case ValueKind.Quaternion:
                        var q = val is Quaternion qv ? qv : Quaternion.identity;
                        var euler = q.eulerAngles;
                        var newEuler = EditorGUI.Vector3Field(rect, GUIContent.none, euler);
                        newVal = newEuler != euler ? Quaternion.Euler(newEuler) : q;
                        break;
                    case ValueKind.Reference:
                        newVal = EditorGUI.ObjectField(rect, val as Object, typeof(Object), true);
                        break;
                    case ValueKind.String:
                        newVal = EditorGUI.TextField(rect, val as string ?? string.Empty);
                        break;
                    default:
                        EditorGUI.LabelField(rect, val?.ToString() ?? "—");
                        newVal = val;
                        break;
                }

                if (!Equals(val, newVal))
                    _window._writeValues[property] = newVal;
            }

            private void DrawActionsCell(Rect rect, BindableProperty property)
            {
                var half = rect.width * 0.5f;
                var readRect = new Rect(rect.x, rect.y, half, rect.height);
                var writeRect = new Rect(rect.x + half, rect.y, half, rect.height);

                if (GUI.Button(readRect, "Read", EditorStyles.miniButtonLeft))
                {
                    if (_window._collection != null && _window._collection.TryRead(property, out var value))
                    {
                        _window._readResults[property] = true;
                        _window._writeValues[property] = ValueContainerToObject(value);
                    }
                    else
                    {
                        _window._readResults[property] = false;
                    }

                    _window.Repaint();
                }

                if (GUI.Button(writeRect, "Write", EditorStyles.miniButtonRight))
                {
                    if (_window._collection != null && _window._writeValues.TryGetValue(property, out var val))
                    {
                        var vc = ObjectToValueContainer(val, property.Kind);
                        if (_window._collection.TryWrite(property, vc))
                            Debug.Log($"[BindingTester] Wrote '{property.Path}' on '{property.Target?.name}': {val}");
                        else
                            Debug.LogWarning($"[BindingTester] Failed to write '{property.Path}' on '{property.Target?.name}'");
                    }
                }
            }
        }
    }
}
#endif