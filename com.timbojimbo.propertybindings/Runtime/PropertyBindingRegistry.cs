using System;
using System.Collections.Generic;
using TimboJimbo.PropertyBindings.Bindings;
using UnityEngine;

namespace TimboJimbo.PropertyBindings
{
    // Note, there is TRY CATCH's here, but really if a binding says it can bind, it should fail if it throws an exception.. 
    // right now, we rely on this catch behaaviour because GEnericPropertyBinding seems to return 'true' for CanBind on some properties, 
    // but then throws when trying to construct it. This is something we should investigate.. Once its fixed,
    // we can remvoe the try catches here, which is especailyl ugly in ResolveBindingType, where we construct the binding just to get its type, 
    // and then immediately dispose it.
    public static class PropertyBindingRegistry
    {
        public delegate bool MatchesDelegate(BindableProperty property);
        public delegate IPropertyBinding CreateDelegate(GameObject root, BindableProperty property);

        private struct Entry
        {
            public int Priority;
            public MatchesDelegate Matches;
            public CreateDelegate Create;
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        static PropertyBindingRegistry()
        {
            const int fallbackPriority = 1000;

            // Specialized bindings (order matters — most specific first)
            Register(TransformPropertyBinding.CanBind, (root, p) => new TransformPropertyBinding(root, p), fallbackPriority);
            Register(CanvasGroupPropertyBinding.CanBind, (root, p) => new CanvasGroupPropertyBinding(root, p), fallbackPriority);
            Register(ImagePropertyBinding.CanBind, (root, p) => new ImagePropertyBinding(root, p), fallbackPriority);
            Register(GraphicPropertyBinding.CanBind, (root, p) => new GraphicPropertyBinding(root, p), fallbackPriority);
            Register(SpriteRendererPropertyBinding.CanBind, (root, p) => new SpriteRendererPropertyBinding(root, p), fallbackPriority);
            Register(CameraPropertyBinding.CanBind, (root, p) => new CameraPropertyBinding(root, p), fallbackPriority);
            Register(BehaviourActivationPropertyBinding.CanBind, (root, p) => new BehaviourActivationPropertyBinding(root, p), fallbackPriority);
            Register(GameObjectActivationPropertyBinding.CanBind, (root, p) => new GameObjectActivationPropertyBinding(root, p), fallbackPriority);

            // Fallbacks
            Register(GenericPropertyBinding.CanBind, (root, p) => new GenericPropertyBinding(root, p), fallbackPriority + 1);
            Register(ReflectionPropertyBinding.CanBind, (root, p) => new ReflectionPropertyBinding(root, p), fallbackPriority + 2);
        }

        public static void Register(MatchesDelegate matches, CreateDelegate create, int priority = 0)
        {
            var entry = new Entry { Priority = priority, Matches = matches, Create = create };

            int index = 0;
            while (index < _entries.Count && _entries[index].Priority <= priority)
                index++;

            _entries.Insert(index, entry);
        }

        public static IPropertyBinding Create(GameObject root, BindableProperty property)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (!entry.Matches(property))
                    continue;

                IPropertyBinding binding = null;
                try
                {
                    binding = entry.Create(root, property);
                    return binding;
                }
                catch
                {
                    binding?.Dispose();
                }
            }

            throw new InvalidOperationException(
                $"No property binding could be created for '{property.Path}' on {property.Target}");
        }

        /// <summary>
        /// Returns the type of binding that would match this property, without fully constructing it.
        /// Returns null if no binding matches.
        /// </summary>
        public static Type ResolveBindingType(GameObject root, BindableProperty property)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (!_entries[i].Matches(property)) continue;
                IPropertyBinding binding = null;
                try
                {
                    binding = _entries[i].Create(root, property);
                    return binding?.GetType();
                }
                catch
                {
                    return null;
                }
                finally
                {
                    binding?.Dispose();
                }
            }
            return null;
        }
    }
}
