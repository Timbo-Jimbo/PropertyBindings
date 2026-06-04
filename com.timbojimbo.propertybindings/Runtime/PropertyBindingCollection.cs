using System;
using System.Collections.Generic;
using System.Reflection;
using TimboJimbo.Core.Utility;
using TimboJimbo.PropertyBindings.Bindings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.PropertyBindings
{
    public sealed class PropertyBindingCollection : IDisposable
    {
        private readonly Dictionary<BindableProperty, IPropertyBinding> _bindings = new Dictionary<BindableProperty, IPropertyBinding>(new BindablePropertyEqualityComparer());
        private int _bulkWriteRequestCount = 0;
        private HashSet<Object> _targetsToNotifyAfterBulkWrite = new HashSet<Object>();

        public IReadOnlyDictionary<BindableProperty, IPropertyBinding> Bindings => _bindings;
        
        private PropertyBindingCollection(GameObject root, IReadOnlyList<BindableProperty> properties)
        {
            foreach (var property in properties)
                _bindings[property] = PropertyBindingRegistry.Create(root, property);
        }

        public static PropertyBindingCollection Bind(GameObject root, IReadOnlyList<BindableProperty> properties)
        {
            return new PropertyBindingCollection(root, properties);
        }

        public bool TryRead(BindableProperty property, out ValueContainer valueContainer)
        {
            valueContainer = default;
            
            if (_bindings.TryGetValue(property, out var binding))
            {
                var readResult = binding.Read();
                if (readResult.Success)
                {
                    valueContainer = readResult.Value;
                    return true;
                }
            }

            return false;
        }

        public bool TryWrite(BindableProperty property, ValueContainer valueContainer)
        {
            if (!_bindings.TryGetValue(property, out var binding))
                return false;

            var writeResult = binding.Write(valueContainer);

            if (!writeResult.Success)
                return false;

            if (writeResult.NotifyTarget)
                AnimationPropertyNotifier.Notify(property.Target);

            return true;
        }

        public BulkPropertyWriter StartBulkWriteScope() => new BulkPropertyWriter(this);

        public void StartBulkWrite()
        {
            var wasBulkWriteInProgress = _bulkWriteRequestCount > 0;

            _bulkWriteRequestCount++;

            if (!wasBulkWriteInProgress)
                _targetsToNotifyAfterBulkWrite.Clear();
        }

        public bool TryBulkWrite(BindableProperty property, ValueContainer valueContainer)
        {
            var bulkWriteInProgress = _bulkWriteRequestCount > 0;
            if (!bulkWriteInProgress)
                throw new InvalidOperationException("No bulk write in progress. Call StartBulkWrite() before appending bulk writes.");

            if (!_bindings.TryGetValue(property, out var binding))
                return false;

            var writeResult = binding.Write(valueContainer);

            if (!writeResult.Success)
                return false;

            if (writeResult.NotifyTarget)
                _targetsToNotifyAfterBulkWrite.Add(property.Target);

            return true;
        }

        public void EndBulkWrite()
        {
            var wasBulkWriteInProgress = _bulkWriteRequestCount > 0;

            _bulkWriteRequestCount = Math.Max(0, _bulkWriteRequestCount - 1);

            var bulkWriteInProgress = _bulkWriteRequestCount > 0;

            if (wasBulkWriteInProgress && !bulkWriteInProgress)
            {
                foreach (var target in _targetsToNotifyAfterBulkWrite)
                    AnimationPropertyNotifier.Notify(target);

                _targetsToNotifyAfterBulkWrite.Clear();
            }
        }

        private static class AnimationPropertyNotifier
        {
            private static readonly Dictionary<Type, MethodInfo> _methodCache = new Dictionary<Type, MethodInfo>();

            public static void Notify(Object target)
            {
                if (target is not Component c)
                    return;

                if (!EditorAwareUtility.IsLiveInstance(c))
                {
                    // SendMessage asserts on components without [ExecuteAlways] in edit mode
                    // or on prefab assets. Use direct invocation instead.
                    var type = c.GetType();
                    if (!_methodCache.TryGetValue(type, out var method))
                    {
                        method = type.GetMethod("OnDidApplyAnimationProperties",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        _methodCache[type] = method;
                    }
                    method?.Invoke(c, null);
                    return;
                }

                c.SendMessage("OnDidApplyAnimationProperties", SendMessageOptions.DontRequireReceiver);
            }

        }

        public void Dispose()
        {
            foreach (var binding in _bindings.Values)
            {
                binding.Dispose();
            }
        }
    }

    public struct BulkPropertyWriter : IDisposable
    {
        private readonly PropertyBindingCollection _collection;

        public BulkPropertyWriter(PropertyBindingCollection collection)
        {
            _collection = collection;
            _collection.StartBulkWrite();
        }

        public bool TryWrite(BindableProperty property, ValueContainer valueContainer)
        {
            return _collection.TryBulkWrite(property, valueContainer);
        }

        public void Dispose()
        {
            _collection.EndBulkWrite();
        }
    }
}