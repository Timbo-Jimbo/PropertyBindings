using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class CanvasGroupPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private CanvasGroup _canvasGroup;
        private CanvasGroupProperty _property;

        public CanvasGroupPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _canvasGroup, out _property))
                throw new System.ArgumentException($"CanvasGroupPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        private static bool TryGetBindingInfo(BindableProperty property, out CanvasGroup canvasGroup, out CanvasGroupProperty canvasGroupProperty)
        {
            canvasGroup = property.Target as CanvasGroup;

            if (canvasGroup == null)
            {
                canvasGroupProperty = default;
                return false;
            }

            if (CanvasGroupProperties.Alpha.Matches(property)) canvasGroupProperty = CanvasGroupProperty.Alpha;
            else if (CanvasGroupProperties.Interactable.Matches(property)) canvasGroupProperty = CanvasGroupProperty.Interactable;
            else if (CanvasGroupProperties.BlocksRaycasts.Matches(property)) canvasGroupProperty = CanvasGroupProperty.BlocksRaycasts;
            else if (CanvasGroupProperties.IgnoreParentGroups.Matches(property)) canvasGroupProperty = CanvasGroupProperty.IgnoreParentGroups;
            else
            {
                canvasGroup = null;
                canvasGroupProperty = default;
                return false;
            }
            return true;
        }

        public override void Dispose()
        {
            _canvasGroup = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_canvasGroup == null)
                return false;

            switch (_property)
            {
                case CanvasGroupProperty.Alpha:
                    valueContainer = ValueContainer.FromFloat(_canvasGroup.alpha);
                    break;
                case CanvasGroupProperty.Interactable:
                    valueContainer = ValueContainer.FromBool(_canvasGroup.interactable);
                    break;
                case CanvasGroupProperty.BlocksRaycasts:
                    valueContainer = ValueContainer.FromBool(_canvasGroup.blocksRaycasts);
                    break;
                case CanvasGroupProperty.IgnoreParentGroups:
                    valueContainer = ValueContainer.FromBool(_canvasGroup.ignoreParentGroups);
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_canvasGroup == null)
                return false;

            switch (_property)
            {
                case CanvasGroupProperty.Alpha:
                    _canvasGroup.alpha = valueContainer.FloatValue;
                    break;
                case CanvasGroupProperty.Interactable:
                    _canvasGroup.interactable = valueContainer.BoolValue;
                    break;
                case CanvasGroupProperty.BlocksRaycasts:
                    _canvasGroup.blocksRaycasts = valueContainer.BoolValue;
                    break;
                case CanvasGroupProperty.IgnoreParentGroups:
                    _canvasGroup.ignoreParentGroups = valueContainer.BoolValue;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private enum CanvasGroupProperty
        {
            Alpha,
            Interactable,
            BlocksRaycasts,
            IgnoreParentGroups
        }
    }
}
