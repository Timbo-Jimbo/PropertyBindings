using System;
using System.Collections.Generic;
using TimboJimbo.PropertyBindings.Bindings;
using UnityEngine;

namespace TimboJimbo.PropertyBindings
{
    public static class PropertyBindingRegistry
    {
        public delegate bool MatchesDelegate(BindableProperty property);
        public delegate IPropertyBinding CreateDelegate(GameObject root, BindableProperty property);

        private struct Entry
        {
            public int Priority;
            public Type BindingType;
            public MatchesDelegate Matches;
            public CreateDelegate Create;
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        static PropertyBindingRegistry()
        {
            const int fallbackPriority = 1000;

            // Specialized bindings (order matters — most specific first)
            Register<TransformPropertyBinding>(TransformPropertyBinding.CanBind, (root, p) => new TransformPropertyBinding(root, p), fallbackPriority);
            Register<CanvasGroupPropertyBinding>(CanvasGroupPropertyBinding.CanBind, (root, p) => new CanvasGroupPropertyBinding(root, p), fallbackPriority);
            Register<ImagePropertyBinding>(ImagePropertyBinding.CanBind, (root, p) => new ImagePropertyBinding(root, p), fallbackPriority);
            Register<GraphicPropertyBinding>(GraphicPropertyBinding.CanBind, (root, p) => new GraphicPropertyBinding(root, p), fallbackPriority);
            Register<SpriteRendererPropertyBinding>(SpriteRendererPropertyBinding.CanBind, (root, p) => new SpriteRendererPropertyBinding(root, p), fallbackPriority);
            Register<CameraPropertyBinding>(CameraPropertyBinding.CanBind, (root, p) => new CameraPropertyBinding(root, p), fallbackPriority);
            Register<BehaviourActivationPropertyBinding>(BehaviourActivationPropertyBinding.CanBind, (root, p) => new BehaviourActivationPropertyBinding(root, p), fallbackPriority);
            Register<GameObjectActivationPropertyBinding>(GameObjectActivationPropertyBinding.CanBind, (root, p) => new GameObjectActivationPropertyBinding(root, p), fallbackPriority);

            // Fallbacks
            Register<GenericPropertyBinding>(GenericPropertyBinding.CanBind, (root, p) => new GenericPropertyBinding(root, p), fallbackPriority + 1);
            Register<ReflectionPropertyBinding>(ReflectionPropertyBinding.CanBind, (root, p) => new ReflectionPropertyBinding(root, p), fallbackPriority + 2);
        }

        public static void Register(MatchesDelegate matches, CreateDelegate create, int priority = 0)
            => Register(null, matches, create, priority);

        public static void Register(Type bindingType, MatchesDelegate matches, CreateDelegate create, int priority = 0)
        {
            if (bindingType != null && !typeof(IPropertyBinding).IsAssignableFrom(bindingType))
                throw new ArgumentException($"{bindingType.FullName} does not implement {nameof(IPropertyBinding)}.", nameof(bindingType));
            var entry = new Entry { Priority = priority, BindingType = bindingType, Matches = matches, Create = create };

            int index = 0;
            while (index < _entries.Count && _entries[index].Priority <= priority)
                index++;

            _entries.Insert(index, entry);
        }

        private static void Register<TBinding>(MatchesDelegate matches, CreateDelegate create, int priority)
            where TBinding : IPropertyBinding => Register(typeof(TBinding), matches, create, priority);

        public static IPropertyBinding Create(GameObject root, BindableProperty property)
        {
            List<Exception> failures = null;
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
                catch (Exception exception)
                {
                    binding?.Dispose();
                    failures ??= new List<Exception>();
                    failures.Add(new InvalidOperationException(
                        $"Binding candidate '{entry.BindingType?.FullName ?? "<unreported custom type>"}' " +
                        $"at priority {entry.Priority} matched but failed to construct.", exception));
                }
            }

            string message = $"No property binding could be created for '{property.Path}' on {property.Target}.";
            if (failures != null)
                throw new AggregateException(message + $" {failures.Count} matching candidate(s) failed.", failures);
            throw new InvalidOperationException(message + " No registered candidate matched.");
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
                    // A matching candidate may reject this live target during construction.
                    // Continue exactly as Create does.
                }
                finally
                {
                    binding?.Dispose();
                }
            }
            return null;
        }

        /// <summary>
        /// Constructs matching candidates in priority order and returns a complete resolution report.
        /// This explicit diagnostic operation may allocate or invoke binding setup and teardown.
        /// </summary>
        public static BindingResolutionReport Diagnose(GameObject root, BindableProperty property)
        {
            var candidates = new List<BindingCandidateReport>(_entries.Count);
            Type selectedType = null;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                bool matched;
                try
                {
                    matched = entry.Matches(property);
                }
                catch (Exception exception)
                {
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, false, null, exception));
                    continue;
                }

                if (!matched)
                {
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, false, null, null));
                    continue;
                }

                IPropertyBinding binding = null;
                try
                {
                    binding = entry.Create(root, property);
                    var bindingType = binding?.GetType();
                    selectedType ??= bindingType;
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, true, bindingType, null));
                }
                catch (Exception exception)
                {
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, true, null, exception));
                }
                finally
                {
                    binding?.Dispose();
                }
            }

            bool isLiveInstance = root != null && root.scene.IsValid() && root.scene.isLoaded;
            return new BindingResolutionReport(property, isLiveInstance, selectedType, candidates);
        }
    }
}
