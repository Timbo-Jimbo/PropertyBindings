using UnityEngine;
using UnityEngine.UI;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class GraphicPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private Graphic _graphic;
        private GraphicProperty _property;

        public GraphicPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root,
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _graphic, out _property))
                throw new System.ArgumentException($"GraphicPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        public override void Dispose()
        {
            _graphic = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_graphic == null)
                return false;

            switch (_property)
            {
                case GraphicProperty.Color:
                    valueContainer = ValueContainer.FromColor(_graphic.color);
                    break;
                case GraphicProperty.RaycastTarget:
                    valueContainer = ValueContainer.FromBool(_graphic.raycastTarget);
                    break;
                case GraphicProperty.RaycastPadding:
                    valueContainer = ValueContainer.FromVector4(_graphic.raycastPadding);
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_graphic == null)
                return false;

            switch (_property)
            {
                case GraphicProperty.Color:
                    // Graphic.color is virtual. Custom Graphics may use its setter to invalidate
                    // generated geometry or propagate tint to child CanvasRenderers.
                    _graphic.color = valueContainer.ColorValue;
                    break;
                case GraphicProperty.RaycastTarget:
                    _graphic.raycastTarget = valueContainer.BoolValue;
                    break;
                case GraphicProperty.RaycastPadding:
                    _graphic.raycastPadding = valueContainer.Vector4Value;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static bool TryGetBindingInfo(BindableProperty property, out Graphic graphic, out GraphicProperty graphicProperty)
        {
            graphic = property.Target as Graphic;

            if (graphic == null)
            {
                graphicProperty = default;
                return false;
            }

            if (GraphicProperties.Color.Matches(property)) graphicProperty = GraphicProperty.Color;
            else if (GraphicProperties.RaycastTarget.Matches(property)) graphicProperty = GraphicProperty.RaycastTarget;
            else if (GraphicProperties.RaycastPadding.Matches(property)) graphicProperty = GraphicProperty.RaycastPadding;
            else
            {
                graphic = null;
                graphicProperty = default;
                return false;
            }
            return true;
        }

        private enum GraphicProperty
        {
            Color,
            RaycastTarget,
            RaycastPadding
        }
    }
}
