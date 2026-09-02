using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class SpriteRendererPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private SpriteRenderer _spriteRenderer;
        private SpriteRendererProperty _property;

        public SpriteRendererPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _spriteRenderer, out _property))
                throw new System.ArgumentException($"SpriteRendererPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        private static bool TryGetBindingInfo(BindableProperty property, out SpriteRenderer spriteRenderer, out SpriteRendererProperty spriteRendererProperty)
        {
            spriteRenderer = property.Target as SpriteRenderer;

            if (spriteRenderer == null)
            {
                spriteRendererProperty = default;
                return false;
            }

            if (SpriteRendererProperties.Color.Matches(property)) spriteRendererProperty = SpriteRendererProperty.Color;
            else if (SpriteRendererProperties.Size.Matches(property)) spriteRendererProperty = SpriteRendererProperty.Size;
            else if (SpriteRendererProperties.FlipX.Matches(property)) spriteRendererProperty = SpriteRendererProperty.FlipX;
            else if (SpriteRendererProperties.FlipY.Matches(property)) spriteRendererProperty = SpriteRendererProperty.FlipY;
            else
            {
                spriteRenderer = null;
                spriteRendererProperty = default;
                return false;
            }
            return true;
        }

        public override void Dispose()
        {
            _spriteRenderer = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_spriteRenderer == null)
                return false;

            switch (_property)
            {
                case SpriteRendererProperty.Color:
                    valueContainer = ValueContainer.FromColor(_spriteRenderer.color);
                    break;
                case SpriteRendererProperty.Size:
                    valueContainer = ValueContainer.FromVector2(_spriteRenderer.size);
                    break;
                case SpriteRendererProperty.FlipX:
                    valueContainer = ValueContainer.FromBool(_spriteRenderer.flipX);
                    break;
                case SpriteRendererProperty.FlipY:
                    valueContainer = ValueContainer.FromBool(_spriteRenderer.flipY);
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_spriteRenderer == null)
                return false;

            switch (_property)
            {
                case SpriteRendererProperty.Color:
                    _spriteRenderer.color = valueContainer.ColorValue;
                    break;
                case SpriteRendererProperty.Size:
                    _spriteRenderer.size = valueContainer.Vector2Value;
                    break;
                case SpriteRendererProperty.FlipX:
                    _spriteRenderer.flipX = valueContainer.BoolValue;
                    break;
                case SpriteRendererProperty.FlipY:
                    _spriteRenderer.flipY = valueContainer.BoolValue;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private enum SpriteRendererProperty
        {
            Color,
            Size,
            FlipX,
            FlipY
        }
    }
}
