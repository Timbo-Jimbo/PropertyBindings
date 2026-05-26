using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TimboJimbo.PropertyBindings;
using TimboJimbo.PropertyBindings.Editor;
using TimboJimbo.PropertyBindings.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace TimboJimboEditor
{
    public class BindablePropertyValueEdit
    {
        public BindableProperty BindableProperty;
        public ValueContainer InitialValue;
        public ValueContainer LatestValue;
    }

    public enum EditType
    {
        Added,
        Modified,
        Removed
    }
    
    public delegate void BindablePropertyEditHandler(EditType editType, BindablePropertyValueEdit edit);

    public class UserEditTracker
    {
        [CanBeNull] private Func<BindableProperty, bool> _filterOut;
        private BindablePropertyEditHandler _onEdit;
        private Dictionary<BindableProperty, BindablePropertyEditState> _changes = new Dictionary<BindableProperty, BindablePropertyEditState>();
        private bool _detecting;

        public UserEditTracker(Func<BindableProperty, bool> filterOut = null)
        {
            _filterOut = filterOut;
        }

        public void StartDetecting(BindablePropertyEditHandler onPropertyChanged)
        {
            if(_detecting)
                throw new InvalidOperationException("Already detecting property changes. Call StopDetecting() before starting again.");

            _onEdit = onPropertyChanged;
            _detecting = true;

            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoEvent += OnUndoRedoEvent;
        }

        public void GetEdits(List<BindablePropertyValueEdit> result)
        {
            result.Clear();
            
            foreach (var (key, state) in _changes)
            {
                var isLive = state.LiveUndoGroups.Count > 0;
                if (isLive) result.Add(state.BindablePropertyValueEdit);
            }
        }

        private void FlushUndoRecord()
        {
            using (ListPool<BindableProperty>.Get(out var keysToRemove))
            {
                foreach (var (key, state) in _changes)
                {
                    //once flushed, what was previously undone can no longer be redone, so we should clear out
                    //associated undo groups that are currently not live, as they will never transition back to being live
                    state.AssociatedUndoGroups.Clear();
                    foreach (var liveGroup in state.LiveUndoGroups)
                        state.AssociatedUndoGroups.Add(liveGroup);

                    // If there are no live undo groups, then this change is effectively removed, so we can clear it out entirely
                    if (state.LiveUndoGroups.Count == 0)
                        keysToRemove.Add(key);
                }

                foreach (var key in keysToRemove)
                {
                    var change = _changes[key];
                    _changes.Remove(key);
                    InvokePropertyChangedSafely(EditType.Removed, change.BindablePropertyValueEdit);
                }
            }
        }

        private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] undoPropertyModification)
        {
            Debug.Log($"PostprocessModifications called with {undoPropertyModification.Length} modifications");

            FlushUndoRecord();

            using(ListPool<SimplifiedModification>.Get(out var parsedModifications))
            {
                using(DictionaryPool<BindableProperty, SimplifiedModification>.Get(out var descToSimplifiedMod))
                {
                    foreach (var modification in undoPropertyModification)
                    {
                        if (!BindablePropertyUtility.TryCreateBindableProperty(
                            target: modification.previousValue.target, 
                            propertyPath: BindablePropertyUtility.GetBasePath(modification.previousValue.propertyPath), // Undo's are always leaf paths
                            bindableProperty: out var bindableProperty
                        )) continue;

                        using var serialisedObject = new SerializedObject(modification.previousValue.target);
                        using var serialisedProperty = serialisedObject.FindProperty(bindableProperty.Path);
                        
                        if (!descToSimplifiedMod.TryGetValue(bindableProperty, out var existingModification))
                        {
                            existingModification = new SimplifiedModification(bindableProperty)
                            {
                                // Note, we are initialising both values to whatever the value currently is, which is wrong..!
                                // But we will stomp over the values with the correct pre/post values in a moment
                                PreModificationValue = serialisedProperty.GetValueContainer(bindableProperty.Kind),
                                PostModificationValue = serialisedProperty.GetValueContainer(bindableProperty.Kind)
                            };

                            descToSimplifiedMod.Add(bindableProperty, existingModification);
                        }

                        // is this..right..? The docs are...nonexistent !
                        // https://docs.unity3d.com/6000.4/Documentation/ScriptReference/PropertyModification-value.html
                        static (float prev, float post) ParseModificationsAsFloat(UndoPropertyModification modification)
                        {
                            return (float.Parse(modification.previousValue.value), float.Parse(modification.currentValue.value));
                        }

                        static (int prev, int post) ParseModificationsAsInt(UndoPropertyModification modification)
                        {
                            return (int.Parse(modification.previousValue.value), int.Parse(modification.currentValue.value));
                        }

                        static (string prev, string post) ParseModificationsAsString(UndoPropertyModification modification)
                        {
                            return (modification.previousValue.value, modification.currentValue.value);
                        }

                        static (bool prev, bool post) ParseModificationsAsBool(UndoPropertyModification modification)
                        {
                            var asFloat = ParseModificationsAsFloat(modification);
                            return (asFloat.prev > 0.5f, asFloat.post > 0.5f);
                        }

                        if (bindableProperty.ComponentLayout == ComponentLayout.One)
                        {
                            switch (bindableProperty.Kind)
                            {
                                case ValueKind.Float:
                                {
                                    var (prevValueAsFloat, postValueAsFloat) = ParseModificationsAsFloat(modification);
                                    existingModification.PreModificationValue.FloatValue = prevValueAsFloat;
                                    existingModification.PostModificationValue.FloatValue = postValueAsFloat;
                                    break;
                                }
                                case ValueKind.Int:
                                {
                                    var (prevValueAsInt, postValueAsInt) = ParseModificationsAsInt(modification);
                                    existingModification.PreModificationValue.IntValue = prevValueAsInt;
                                    existingModification.PostModificationValue.IntValue = postValueAsInt;
                                    break;
                                }
                                
                                case ValueKind.Bool:
                                {
                                    var (prevValueAsBool, postValueAsBool) = ParseModificationsAsBool(modification);
                                    existingModification.PreModificationValue.BoolValue = prevValueAsBool;
                                    existingModification.PostModificationValue.BoolValue = postValueAsBool;
                                    break;
                                }
                                
                                case ValueKind.Enum:
                                {
                                    var (preValueAsInt, postValueAsInt) = ParseModificationsAsInt(modification);
                                    existingModification.PreModificationValue.EnumValue = preValueAsInt;
                                    existingModification.PostModificationValue.EnumValue = postValueAsInt;
                                    break;
                                }
                                
                                case ValueKind.Reference:
                                {
                                    var (prevValueAsInt, postValueAsInt) = ParseModificationsAsInt(modification);
                                    existingModification.PreModificationValue.ReferenceValue = EditorUtility.EntityIdToObject(prevValueAsInt);
                                    existingModification.PostModificationValue.ReferenceValue = EditorUtility.EntityIdToObject(postValueAsInt);
                                    break;
                                }

                                case ValueKind.String:
                                {
                                    var (prevValueAsString, postValueAsString) = ParseModificationsAsString(modification);
                                    existingModification.PreModificationValue.StringValue = prevValueAsString;
                                    existingModification.PostModificationValue.StringValue = postValueAsString;
                                    break;
                                }

                                default:
                                    throw new InvalidOperationException($"Unsupported value kind for component layout {bindableProperty.ComponentLayout}: {bindableProperty.Kind}");
                            }
                        }
                        else
                        {
                            var modifiedComponentChar = char.ToLower(modification.previousValue.propertyPath[modification.previousValue.propertyPath.Length - 1]);
                            var modifiedComponentIndex = modifiedComponentChar switch
                            {
                                'x' or 'r' => 0,
                                'y' or 'g' => 1,
                                'z' or 'b' => 2,
                                'w' or 'a' => 3,
                                _ => throw new InvalidOperationException($"Unexpected component suffix in property path: {modification.previousValue.propertyPath}")
                            };

                            var (prevValueAsFloat, postValueAsFloat) = ParseModificationsAsFloat(modification);

                            switch (bindableProperty.Kind)
                            {
                                case ValueKind.Vector2:
                                {
                                    var vector2 = existingModification.PreModificationValue.Vector2Value;
                                    vector2[modifiedComponentIndex] = prevValueAsFloat;
                                    existingModification.PreModificationValue.Vector2Value = vector2;

                                    vector2 = existingModification.PostModificationValue.Vector2Value;
                                    vector2[modifiedComponentIndex] = postValueAsFloat;
                                    existingModification.PostModificationValue.Vector2Value = vector2;
                                    break;
                                }

                                case ValueKind.Vector3:
                                {
                                    var vector3 = existingModification.PreModificationValue.Vector3Value;
                                    vector3[modifiedComponentIndex] = prevValueAsFloat;
                                    existingModification.PreModificationValue.Vector3Value = vector3;

                                    vector3 = existingModification.PostModificationValue.Vector3Value;
                                    vector3[modifiedComponentIndex] = postValueAsFloat;
                                    existingModification.PostModificationValue.Vector3Value = vector3;
                                    break;
                                }

                                case ValueKind.Vector4:
                                {
                                    var vector4 = existingModification.PreModificationValue.Vector4Value;
                                    vector4[modifiedComponentIndex] = prevValueAsFloat;
                                    existingModification.PreModificationValue.Vector4Value = vector4;

                                    vector4 = existingModification.PostModificationValue.Vector4Value;
                                    vector4[modifiedComponentIndex] = postValueAsFloat;
                                    existingModification.PostModificationValue.Vector4Value = vector4;
                                    break;
                                }

                                case ValueKind.Color:
                                {
                                    var color = existingModification.PreModificationValue.ColorValue;
                                    color[modifiedComponentIndex] = prevValueAsFloat;
                                    existingModification.PreModificationValue.ColorValue = color;

                                    color = existingModification.PostModificationValue.ColorValue;
                                    color[modifiedComponentIndex] = postValueAsFloat;
                                    existingModification.PostModificationValue.ColorValue = color;
                                    break;
                                }

                                case ValueKind.Quaternion:
                                {
                                    var quaternion = existingModification.PreModificationValue.QuaternionValue;
                                    quaternion[modifiedComponentIndex] = prevValueAsFloat;
                                    existingModification.PreModificationValue.QuaternionValue = quaternion;

                                    quaternion = existingModification.PostModificationValue.QuaternionValue;
                                    quaternion[modifiedComponentIndex] = postValueAsFloat;
                                    existingModification.PostModificationValue.QuaternionValue = quaternion;
                                    break;
                                }

                                default:
                                    throw new InvalidOperationException($"Unsupported value kind for component layout {bindableProperty.ComponentLayout}: {bindableProperty.Kind}");
                            }
                        }
                    }

                    foreach (var kvp in descToSimplifiedMod)
                        parsedModifications.Add(kvp.Value);
                }

                //external filtering
                if( _filterOut != null)
                {
                    for (int i = parsedModifications.Count - 1; i >= 0; i--)
                    {
                        var modification = parsedModifications[i];
                        if (_filterOut(modification.BindableProperty))
                            parsedModifications.RemoveAt(i);
                    }
                }

                // 'Built-in' filtering
                {
                    // The rationale here is that: when a gameobject was *just* activated or deactivated, that
                    // can result in shifting around RectTransforms due to layout changes. This is an indirect
                    // consequence of the user activating/deactivating an object, so we want to filter this out
                    // (Any changes in the future to those RectTransforms will be captured and treated as normal, 
                    // this is just to avoid the noise of the cascade of changes that happens on activation/deactivation)
                    var anyGoActivationChanges = parsedModifications.Exists(sm => sm.BindableProperty.Target is GameObject go && sm.BindableProperty.Path is "m_IsActive");
                    var anyBehaviourActivationChanges = parsedModifications.Exists(sm => sm.BindableProperty.Target is Behaviour behaviour && sm.BindableProperty.Path is "m_Enabled");

                    if (anyGoActivationChanges || anyBehaviourActivationChanges)
                    {
                        parsedModifications.RemoveAll(sm => sm.BindableProperty.Target is RectTransform);
                    }
                }


                foreach(var m in parsedModifications)
                {
                    var propertyChangeState = _changes.ContainsKey(m.BindableProperty) ? _changes[m.BindableProperty] : null;
                    var firstChange = propertyChangeState == null;

                    if(firstChange)
                    {
                        propertyChangeState = new BindablePropertyEditState
                        {
                            BindablePropertyValueEdit = new BindablePropertyValueEdit
                            {
                                BindableProperty = m.BindableProperty,
                                InitialValue = m.PreModificationValue,
                                LatestValue = m.PostModificationValue
                            },
                            AssociatedUndoGroups = new HashSet<int> { Undo.GetCurrentGroup() },
                            LiveUndoGroups = new HashSet<int> { Undo.GetCurrentGroup() }
                        };

                        _changes[m.BindableProperty] = propertyChangeState;
                    }
                    
                    propertyChangeState.AssociatedUndoGroups.Add(Undo.GetCurrentGroup());
                    propertyChangeState.LiveUndoGroups.Add(Undo.GetCurrentGroup());
                    propertyChangeState.BindablePropertyValueEdit.LatestValue = m.PostModificationValue;
                    var changeType = firstChange ? EditType.Added : EditType.Modified;
                    InvokePropertyChangedSafely(changeType, propertyChangeState.BindablePropertyValueEdit);
                }
            }

            return undoPropertyModification;
        }


        private void OnUndoRedoEvent(in UndoRedoInfo undoRedoInfo)
        {
            foreach (var (_, state) in _changes)
            {
                if (state.AssociatedUndoGroups.Contains(undoRedoInfo.undoGroup))
                {
                    var wasLive = state.LiveUndoGroups.Count > 0;

                    if(undoRedoInfo.isRedo)
                        state.LiveUndoGroups.Add(undoRedoInfo.undoGroup);
                    else
                        state.LiveUndoGroups.Remove(undoRedoInfo.undoGroup);

                    var isLive = state.LiveUndoGroups.Count > 0;

                    using var serialisedObject = new SerializedObject(state.BindableProperty.Target);
                    using var serialisedProperty = serialisedObject.FindProperty(state.BindableProperty.Path);
                    state.BindablePropertyValueEdit.LatestValue = serialisedProperty.GetValueContainer(state.BindableProperty.Kind);

                    if (wasLive && !isLive)
                    {
                        // Transitioned from live to not live => treat as removed
                        InvokePropertyChangedSafely(EditType.Removed, state.BindablePropertyValueEdit);
                    }
                    else if (!wasLive && isLive)
                    {
                        // Transitioned from not live to live => treat as added
                        InvokePropertyChangedSafely(EditType.Added, state.BindablePropertyValueEdit);
                    }
                    else if (isLive)
                    {
                        // Still live => treat as modified
                        InvokePropertyChangedSafely(EditType.Modified, state.BindablePropertyValueEdit);
                    }
                }
            }
        }

        public void StopDetecting()
        {
            if(!_detecting)
                return;

            _changes.Clear();
            _onEdit = null;
            _detecting = false;
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoEvent -= OnUndoRedoEvent;
        }

        private void InvokePropertyChangedSafely(EditType changeType, BindablePropertyValueEdit change)
        {
            try
            {
                _onEdit?.Invoke(changeType, change);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in PropertyChangedHandler: {ex}");
            }
        }
        
        private class SimplifiedModification
        {
            public BindableProperty BindableProperty;
            public ValueContainer PreModificationValue;
            public ValueContainer PostModificationValue;

            public SimplifiedModification(BindableProperty bindableProperty)
            {
                BindableProperty = bindableProperty;
            }
        }
            
        private class BindablePropertyEditState
        {
            public BindableProperty BindableProperty => BindablePropertyValueEdit.BindableProperty;
            public BindablePropertyValueEdit BindablePropertyValueEdit;
            public HashSet<int> AssociatedUndoGroups = new HashSet<int>();
            public HashSet<int> LiveUndoGroups = new HashSet<int>();
        }
    }
}