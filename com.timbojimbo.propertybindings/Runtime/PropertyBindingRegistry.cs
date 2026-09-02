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
            public IReadOnlyList<IPropertyDescriptor> Descriptors;
            public MatchesDelegate Matches;
            public CreateDelegate Create;
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        static PropertyBindingRegistry()
        {
            const int fallbackPriority = 1000;

            // Specialized bindings (order matters — most specific first)
            Register<TransformPropertyBinding>((root, p) => new TransformPropertyBinding(root, p), fallbackPriority,
                TransformProperties.LocalPosition, TransformProperties.LocalRotation, TransformProperties.LocalScale,
                RectTransformProperties.AnchorMin, RectTransformProperties.AnchorMax,
                RectTransformProperties.AnchoredPosition, RectTransformProperties.SizeDelta, RectTransformProperties.Pivot);
            Register<CanvasGroupPropertyBinding>((root, p) => new CanvasGroupPropertyBinding(root, p), fallbackPriority,
                CanvasGroupProperties.Alpha, CanvasGroupProperties.Interactable,
                CanvasGroupProperties.BlocksRaycasts, CanvasGroupProperties.IgnoreParentGroups);
            Register<ImagePropertyBinding>((root, p) => new ImagePropertyBinding(root, p), fallbackPriority,
                ImageProperties.FillAmount, ImageProperties.FillClockwise, ImageProperties.PreserveAspect,
                ImageProperties.FillCenter, ImageProperties.PixelsPerUnitMultiplier, ImageProperties.Type,
                ImageProperties.FillMethod, ImageProperties.FillOrigin, ImageProperties.Sprite);
            Register<GraphicPropertyBinding>((root, p) => new GraphicPropertyBinding(root, p), fallbackPriority,
                GraphicProperties.Color, GraphicProperties.RaycastTarget, GraphicProperties.RaycastPadding);
            Register<SpriteRendererPropertyBinding>((root, p) => new SpriteRendererPropertyBinding(root, p), fallbackPriority,
                SpriteRendererProperties.Color, SpriteRendererProperties.Size,
                SpriteRendererProperties.FlipX, SpriteRendererProperties.FlipY);
            Register<CameraPropertyBinding>((root, p) => new CameraPropertyBinding(root, p), fallbackPriority,
                CameraProperties.FieldOfView, CameraProperties.OrthographicSize, CameraProperties.BackgroundColor,
                CameraProperties.NearClipPlane, CameraProperties.FarClipPlane);
            Register<BehaviourActivationPropertyBinding>((root, p) => new BehaviourActivationPropertyBinding(root, p), fallbackPriority,
                BehaviourProperties.Enabled);
            Register<GameObjectActivationPropertyBinding>((root, p) => new GameObjectActivationPropertyBinding(root, p), fallbackPriority,
                GameObjectProperties.ActiveSelf);

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

            Insert(entry);
        }

        public static void Register(
            Type bindingType,
            IReadOnlyList<IPropertyDescriptor> descriptors,
            CreateDelegate create,
            int priority = 0)
        {
            if (bindingType == null) throw new ArgumentNullException(nameof(bindingType));
            if (!typeof(IPropertyBinding).IsAssignableFrom(bindingType))
                throw new ArgumentException($"{bindingType.FullName} does not implement {nameof(IPropertyBinding)}.", nameof(bindingType));
            if (descriptors == null || descriptors.Count == 0)
                throw new ArgumentException("At least one descriptor is required.", nameof(descriptors));

            var descriptorCopy = new IPropertyDescriptor[descriptors.Count];
            for (int i = 0; i < descriptors.Count; i++)
            {
                descriptorCopy[i] = descriptors[i] ?? throw new ArgumentException("Descriptors cannot contain null.", nameof(descriptors));
                PropertyDescriptorRegistry.Register(descriptorCopy[i]);
            }

            Insert(new Entry
            {
                Priority = priority,
                BindingType = bindingType,
                Descriptors = descriptorCopy,
                Matches = property => MatchesAnyDescriptor(property, descriptorCopy),
                Create = create
            });
        }

        public static void Register<TBinding>(
            IReadOnlyList<IPropertyDescriptor> descriptors,
            CreateDelegate create,
            int priority = 0)
            where TBinding : IPropertyBinding => Register(typeof(TBinding), descriptors, create, priority);

        private static void Insert(Entry entry)
        {
            if (entry.Create == null) throw new ArgumentNullException(nameof(entry.Create));

            int index = 0;
            while (index < _entries.Count && _entries[index].Priority <= entry.Priority)
                index++;

            _entries.Insert(index, entry);
        }

        private static void Register<TBinding>(MatchesDelegate matches, CreateDelegate create, int priority)
            where TBinding : IPropertyBinding => Register(typeof(TBinding), matches, create, priority);

        private static void Register<TBinding>(
            CreateDelegate create,
            int priority,
            params IPropertyDescriptor[] descriptors)
            where TBinding : IPropertyBinding => Register(typeof(TBinding), descriptors, create, priority);

        private static bool MatchesAnyDescriptor(BindableProperty property, IReadOnlyList<IPropertyDescriptor> descriptors)
        {
            for (int i = 0; i < descriptors.Count; i++)
                if (descriptors[i].Matches(property)) return true;
            return false;
        }

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
            IPropertyDescriptor resolvedDescriptor = null;
            property.TryGetDescriptor(out resolvedDescriptor);

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var descriptorIds = GetDescriptorIds(entry.Descriptors);
                var matchKind = entry.Descriptors != null ? BindingMatchKind.Descriptor : BindingMatchKind.Predicate;
                bool matched;
                try
                {
                    matched = entry.Matches(property);
                }
                catch (Exception exception)
                {
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, descriptorIds,
                        matchKind, false, null, exception));
                    continue;
                }

                if (!matched)
                {
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, descriptorIds,
                        BindingMatchKind.None, false, null, null));
                    continue;
                }

                IPropertyBinding binding = null;
                try
                {
                    binding = entry.Create(root, property);
                    var bindingType = binding?.GetType();
                    selectedType ??= bindingType;
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, descriptorIds,
                        matchKind, true, bindingType, null));
                }
                catch (Exception exception)
                {
                    candidates.Add(new BindingCandidateReport(entry.Priority, entry.BindingType, descriptorIds,
                        matchKind, true, null, exception));
                }
                finally
                {
                    binding?.Dispose();
                }
            }

            bool isLiveInstance = root != null && root.scene.IsValid() && root.scene.isLoaded;
            return new BindingResolutionReport(property, isLiveInstance, selectedType, resolvedDescriptor, candidates);
        }

        private static IReadOnlyList<string> GetDescriptorIds(IReadOnlyList<IPropertyDescriptor> descriptors)
        {
            if (descriptors == null) return Array.Empty<string>();
            var ids = new string[descriptors.Count];
            for (int i = 0; i < descriptors.Count; i++) ids[i] = descriptors[i].Id;
            return ids;
        }
    }
}
