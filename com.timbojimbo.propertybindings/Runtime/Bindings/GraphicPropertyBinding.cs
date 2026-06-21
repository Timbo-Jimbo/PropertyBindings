using UnityEngine;
using UnityEngine.UI;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class GraphicPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private Graphic _graphic;
        private bool _rwColorFromCanvasRenderer;
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

            _rwColorFromCanvasRenderer = TargetIsConsideredLiveInstance() && _property == GraphicProperty.Color;

            if(_rwColorFromCanvasRenderer)
            {
                _graphic.canvasRenderer.SetColor(_graphic.color);
                _graphic.color = Color.white;
            }
        }

        public override void Dispose()
        {
            if(_graphic && _rwColorFromCanvasRenderer)
            {
                _graphic.color = _graphic.canvasRenderer.GetColor();
                _graphic.canvasRenderer.SetColor(Color.white);
            }

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
                    if(_rwColorFromCanvasRenderer)
                        valueContainer = ValueContainer.FromColor(_graphic.canvasRenderer.GetColor());
                    else
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
            Debug.Log($"Attempting to write value to Graphic property '{_property}' on '{_graphic.name}'. Value: {valueContainer}");
            if (_graphic == null)
                return false;

            switch (_property)
            {
                case GraphicProperty.Color:
                    if (_rwColorFromCanvasRenderer)
                        _graphic.canvasRenderer.SetColor(valueContainer.ColorValue);
                    else
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

        public static bool CanBind(BindableProperty property)
        {
            return TryGetBindingInfo(property, out _, out _);
        }

        private static bool TryGetBindingInfo(BindableProperty property, out Graphic graphic, out GraphicProperty graphicProperty)
        {
            graphic = property.Target as Graphic;

            if (graphic == null)
            {
                graphicProperty = default;
                return false;
            }

            switch (property.Path)
            {
                case "m_Color":
                    graphicProperty = GraphicProperty.Color;
                    return true;
                case "m_RaycastTarget":
                    graphicProperty = GraphicProperty.RaycastTarget;
                    return true;
                case "m_RaycastPadding":
                    graphicProperty = GraphicProperty.RaycastPadding;
                    return true;
                default:
                    graphicProperty = default;
                    return false;
            }
        }

        private enum GraphicProperty
        {
            Color,
            RaycastTarget,
            RaycastPadding
        }
    }
}
