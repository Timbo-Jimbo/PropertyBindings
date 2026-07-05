#if TJ_PROPERTY_BINDINGS_DEBUG_WINDOWS
using System.Collections.Generic;
using System;
using TimboJimbo.PropertyBindings;
using TimboJimboEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimboTests.PropertyBindings.Debugging
{
    public sealed class UserEditTrackerTestWindow : EditorWindow
    {
        private GameObject _target;
        private UserEditTracker _tracker;
        private bool _isTracking;

        [SerializeField] private TreeViewState<int> _treeViewState;
        [SerializeField] private MultiColumnHeaderState _columnHeaderState;
        private EditTreeView _treeView;

        private readonly List<EditEntry> _entries = new List<EditEntry>();
        private readonly List<KeyValuePair<BindableProperty, float>> _sortedFilteredEdits = new List<KeyValuePair<BindableProperty, float>>();
        private int _dataRevision;

        private struct EditEntry
        {
            public BindableProperty Property;
            public ValueContainer InitialValue;
            public ValueContainer LatestValue;
        }

        [MenuItem("Window/TimboJimbo/UserEditTracker Test")]
        private static void ShowWindow()
        {
            var window = GetWindow<UserEditTrackerTestWindow>("UserEditTracker Test");
            window.Show();
        }

        private void OnEnable()
        {
            _treeViewState ??= new TreeViewState<int>();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
            StopTracking();
        }

        private void OnEditorUpdate()
        {
            if (_isTracking)
                Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.ExitingEditMode)
                StopTracking();
        }

        private void StartTracking()
        {
            StopTracking();
            _tracker = new UserEditTracker();
            _entries.Clear();
            _dataRevision++;
            _tracker.StartDetecting(OnEdit);
            _isTracking = true;
            RebuildTreeView();
        }

        private void StopTracking()
        {
            if (_tracker != null)
            {
                _tracker.StopDetecting();
                _tracker = null;
            }

            _isTracking = false;
        }

        private void OnEdit(EditType editType, BindablePropertyValueEdit edit)
        {
            switch (editType)
            {
                case EditType.Added:
                {
                    _entries.Add(new EditEntry
                    {
                        Property = edit.BindableProperty,
                        InitialValue = edit.InitialValue,
                        LatestValue = edit.LatestValue,
                    });
                    break;
                }
                case EditType.Modified:
                {
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        if (_entries[i].Property.Equals(edit.BindableProperty))
                        {
                            var e = _entries[i];
                            e.LatestValue = edit.LatestValue;
                            _entries[i] = e;
                            break;
                        }
                    }
                    break;
                }
                case EditType.Removed:
                {
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        if (_entries[i].Property.Equals(edit.BindableProperty))
                        {
                            _entries.RemoveAt(i);
                            break;
                        }
                    }
                    break;
                }
            }

            _dataRevision++;
            _treeView?.Reload();
            _treeView?.ExpandAll();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("UserEditTracker Test", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = !_isTracking;
                if (GUILayout.Button("Start Tracking", GUILayout.Height(24)))
                    StartTracking();

                GUI.enabled = _isTracking;
                if (GUILayout.Button("Stop Tracking", GUILayout.Height(24)))
                    StopTracking();

                if (GUILayout.Button("Clear", GUILayout.Height(24)))
                {
                    _entries.Clear();
                    _dataRevision++;
                    _treeView?.Reload();
                    Repaint();
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            var statusText = _isTracking ? "Tracking active" : "Not tracking";
            EditorGUILayout.LabelField($"Status: {statusText}   |   Edits: {_entries.Count}", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            if (_treeView == null)
                RebuildTreeView();

            if (_treeView != null)
            {
                var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
                _treeView.OnGUI(rect);
            }
        }

        private MultiColumnHeader CreateColumnHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Property"),
                    width = 200, minWidth = 100, autoResize = true, canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Kind"),
                    width = 80, minWidth = 50, maxWidth = 120, autoResize = false, canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Initial Value"),
                    width = 160, minWidth = 80, autoResize = true, canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Latest Value"),
                    width = 160, minWidth = 80, autoResize = true, canSort = false
                },
            };

            _columnHeaderState = new MultiColumnHeaderState(columns);
            var header = new MultiColumnHeader(_columnHeaderState) { canSort = false, height = 22 };
            header.ResizeToFit();
            return header;
        }

        private void RebuildTreeView()
        {
            _treeView = new EditTreeView(_treeViewState, CreateColumnHeader(), this);
            _treeView.Reload();
            _treeView.ExpandAll();
        }

        private static string ValueContainerToString(ValueContainer vc) => vc.Kind switch
        {
            ValueKind.Float => vc.FloatValue.ToString("F3"),
            ValueKind.Int => vc.IntValue.ToString(),
            ValueKind.Bool => vc.BoolValue.ToString(),
            ValueKind.Enum => vc.EnumValue.ToString(),
            ValueKind.Vector2 => vc.Vector2Value.ToString("F2"),
            ValueKind.Vector3 => vc.Vector3Value.ToString("F2"),
            ValueKind.Vector4 => vc.Vector4Value.ToString("F2"),
            ValueKind.Color => vc.ColorValue.ToString("F2"),
            ValueKind.Quaternion => vc.QuaternionValue.eulerAngles.ToString("F1"),
            ValueKind.Reference => vc.ReferenceValue != null ? vc.ReferenceValue.name : "(null)",
            _ => "—",
        };

        // ──────────────────────────────────────────────
        //  TreeView
        // ──────────────────────────────────────────────

        private const int ColName = 0;
        private const int ColKind = 1;
        private const int ColInitial = 2;
        private const int ColLatest = 3;

        private enum NodeType { GameObject, Component, Property }

        private struct NodeData
        {
            public NodeType Type;
            public Object Target;
            public int EntryIndex; // index into _entries, valid only for Property nodes
        }

        private sealed class EditTreeView : TreeView<int>
        {
            private readonly UserEditTrackerTestWindow _window;
            private readonly Dictionary<int, NodeData> _nodes = new Dictionary<int, NodeData>();

            public EditTreeView(TreeViewState<int> state, MultiColumnHeader header, UserEditTrackerTestWindow window)
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

                if (_window._entries.Count == 0)
                {
                    var emptyItem = new TreeViewItem<int>(0, 0, _window._isTracking ? "No edits detected yet. Modify properties in the Inspector." : "Click 'Start Tracking' to begin.");
                    _nodes[0] = new NodeData { Type = NodeType.GameObject, EntryIndex = -1 };
                    items.Add(emptyItem);
                    SetupParentsAndChildrenFromDepths(root, items);
                    return root;
                }

                int id = 0;

                // Group entries by GameObject > Component > Property (same pattern as StyleSet editor)
                var goOrder = new List<int>();
                var goMap = new Dictionary<int, (Object go, string goName, List<int> direct, List<int> compOrder, Dictionary<int, (Object comp, string compName, List<int> entryIndices)> comps)>();

                for (int i = 0; i < _window._entries.Count; i++)
                {
                    var entry = _window._entries[i];
                    var target = entry.Property.Target;

                    var comp = target as Component;
                    var go = comp != null ? comp.gameObject : target as GameObject;
                    var goKey = go != null ? go.GetInstanceID() : 0;

                    if (!goMap.TryGetValue(goKey, out var g))
                    {
                        g = (go, go != null ? go.name : "(unknown)", new List<int>(), new List<int>(), new Dictionary<int, (Object, string, List<int>)>());
                        goMap[goKey] = g;
                        goOrder.Add(goKey);
                    }

                    if (comp == null)
                    {
                        g.direct.Add(i);
                    }
                    else
                    {
                        var compKey = comp.GetInstanceID();
                        if (!g.comps.TryGetValue(compKey, out var c))
                        {
                            c = (comp, ObjectNames.NicifyVariableName(comp.GetType().Name), new List<int>());
                            g.comps[compKey] = c;
                            g.compOrder.Add(compKey);
                        }

                        c.entryIndices.Add(i);
                    }
                }

                foreach (var goKey in goOrder)
                {
                    var (go, goName, direct, compOrder, comps) = goMap[goKey];

                    var goItemId = id++;
                    items.Add(new TreeViewItem<int>(goItemId, 0, goName));
                    _nodes[goItemId] = new NodeData { Type = NodeType.GameObject, Target = go as Object, EntryIndex = -1 };

                    foreach (var entryIdx in direct)
                    {
                        var pid = id++;
                        items.Add(new TreeViewItem<int>(pid, 1, _window._entries[entryIdx].Property.Path));
                        _nodes[pid] = new NodeData { Type = NodeType.Property, EntryIndex = entryIdx };
                    }

                    foreach (var compKey in compOrder)
                    {
                        var (comp, compName, entryIndices) = comps[compKey];
                        var cid = id++;
                        items.Add(new TreeViewItem<int>(cid, 1, $"{compName} ({entryIndices.Count})"));
                        _nodes[cid] = new NodeData { Type = NodeType.Component, Target = comp as Object, EntryIndex = -1 };

                        foreach (var entryIdx in entryIndices)
                        {
                            var pid = id++;
                            items.Add(new TreeViewItem<int>(pid, 2, _window._entries[entryIdx].Property.Path));
                            _nodes[pid] = new NodeData { Type = NodeType.Property, EntryIndex = entryIdx };
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
                        if (data.Type == NodeType.Property && data.EntryIndex >= 0)
                            EditorGUI.LabelField(rect, _window._entries[data.EntryIndex].Property.Kind.ToString(), EditorStyles.miniLabel);
                        break;
                    case ColInitial:
                        if (data.Type == NodeType.Property && data.EntryIndex >= 0)
                            EditorGUI.LabelField(rect, ValueContainerToString(_window._entries[data.EntryIndex].InitialValue), EditorStyles.miniLabel);
                        break;
                    case ColLatest:
                        if (data.Type == NodeType.Property && data.EntryIndex >= 0)
                            EditorGUI.LabelField(rect, ValueContainerToString(_window._entries[data.EntryIndex].LatestValue), EditorStyles.miniLabel);
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

                if (data.Type == NodeType.Property)
                {
                    EditorGUI.LabelField(rect, item.displayName, EditorStyles.label);
                }
                else
                {
                    var style = data.Type == NodeType.GameObject ? EditorStyles.boldLabel : EditorStyles.label;
                    EditorGUI.LabelField(rect, item.displayName, style);
                }
            }
        }
    }
}
#endif