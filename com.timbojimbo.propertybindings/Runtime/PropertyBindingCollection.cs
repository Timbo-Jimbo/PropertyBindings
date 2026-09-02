using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IReadOnlyList<BindableProperty> _properties;
        private int _bulkWriteRequestCount = 0;
        private HashSet<Object> _targetsToNotifyAfterBulkWrite = new HashSet<Object>();

        public IReadOnlyList<BindableProperty> Properties => _properties;

        private PropertyBindingCollection(GameObject root, IReadOnlyList<BindableProperty> properties)
        {
            try
            {
                foreach (var property in properties)
                    _bindings[property] = PropertyBindingRegistry.Create(root, property);
                _properties = properties.ToList().AsReadOnly(); // create a copy to ensure immutability
            }
            catch
            {
                foreach (var binding in _bindings.Values)
                    binding.Dispose();
                _bindings.Clear();
                throw;
            }
        }

        public static PropertyBindingCollection Bind(GameObject root, IReadOnlyList<BindableProperty> properties)
        {
            return new PropertyBindingCollection(root, properties);
        }
        
        public bool TryGetBindingType(BindableProperty property, out Type bindingType)
        {
            bindingType = null;
            if (_bindings.TryGetValue(property, out var binding))
            {
                bindingType = binding.GetType();
                return true;
            }
            return false;
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

        /// <summary>
        /// Write to a property. Will attempt a direct write if no bulk write scope is active, otherwise will attempt a bulk write. 
        /// This is the recommended way to write to properties.
        /// </summary>
        public bool TryWrite(BindableProperty property, ValueContainer valueContainer)
        {
            if (_bulkWriteRequestCount > 0)
                return TryBulkWrite(property, valueContainer);

            return TryDirectWrite(property, valueContainer);
        }

        /// <summary>
        /// Write to a property directly. Will throw if a bulk write scope is active. Prefer TryWrite() in general, and use TryDirectWrite if you want to enforce that writes happen immediately.
        /// </summary>
        public bool TryDirectWrite(BindableProperty property, ValueContainer valueContainer)
        {
            if (_bulkWriteRequestCount > 0)
                throw new InvalidOperationException("Bulk write in progress. Call TryBulkWrite() to write during a bulk write operation.");

            if (!_bindings.TryGetValue(property, out var binding))
                return false;

            var writeResult = binding.Write(valueContainer);

            if (!writeResult.Success)
                return false;

            if (writeResult.NotifyTarget)
                AnimationPropertyNotifier.Notify(property.Target);

            return true;
        }

        public BulkWriteScope BulkWriteScope() => new BulkWriteScope(this);

        public void StartBulkWrite()
        {
            var wasBulkWriteInProgress = _bulkWriteRequestCount > 0;

            _bulkWriteRequestCount++;

            if (!wasBulkWriteInProgress)
                _targetsToNotifyAfterBulkWrite.Clear();
        }

        /// <summary>
        /// Attempt to write to a property as part of a bulk write operation. Will throw if no bulk write is in progress. Prefer TryWrite() in general, and use TryBulkWrite if you want to ensure writes happen as part of a bulk write operation.
        /// </summary>
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

    public struct BulkWriteScope : IDisposable
    {
        private readonly PropertyBindingCollection _collection;

        public BulkWriteScope(PropertyBindingCollection collection)
        {
            _collection = collection;
            _collection.StartBulkWrite();
        }

        public void Dispose()
        {
            _collection.EndBulkWrite();
        }
    }
}