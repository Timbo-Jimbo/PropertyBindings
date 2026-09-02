using UnityEngine;
using UnityEngine.UI;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class ImagePropertyBinding : OptimizedReadWritePropertyBinding
    {
        private Image _image;
        private ImageProperty _property;

        public ImagePropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _image, out _property))
                throw new System.ArgumentException($"ImagePropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        private static bool TryGetBindingInfo(BindableProperty property, out Image image, out ImageProperty imageProperty)
        {
            image = property.Target as Image;

            if (image == null)
            {
                imageProperty = default;
                return false;
            }

            if (ImageProperties.FillAmount.Matches(property)) imageProperty = ImageProperty.FillAmount;
            else if (ImageProperties.FillClockwise.Matches(property)) imageProperty = ImageProperty.FillClockwise;
            else if (ImageProperties.PreserveAspect.Matches(property)) imageProperty = ImageProperty.PreserveAspect;
            else if (ImageProperties.FillCenter.Matches(property)) imageProperty = ImageProperty.FillCenter;
            else if (ImageProperties.PixelsPerUnitMultiplier.Matches(property)) imageProperty = ImageProperty.PixelsPerUnitMultiplier;
            else if (ImageProperties.Type.Matches(property)) imageProperty = ImageProperty.Type;
            else if (ImageProperties.FillMethod.Matches(property)) imageProperty = ImageProperty.FillMethod;
            else if (ImageProperties.FillOrigin.Matches(property)) imageProperty = ImageProperty.FillOrigin;
            else if (ImageProperties.Sprite.Matches(property)) imageProperty = ImageProperty.Sprite;
            else
            {
                image = null;
                imageProperty = default;
                return false;
            }
            return true;
        }

        public override void Dispose()
        {
            _image = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => true;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_image == null)
                return false;

            switch (_property)
            {
                case ImageProperty.FillAmount:
                    valueContainer = ValueContainer.FromFloat(_image.fillAmount);
                    break;
                case ImageProperty.FillClockwise:
                    valueContainer = ValueContainer.FromBool(_image.fillClockwise);
                    break;
                case ImageProperty.PreserveAspect:
                    valueContainer = ValueContainer.FromBool(_image.preserveAspect);
                    break;
                case ImageProperty.FillCenter:
                    valueContainer = ValueContainer.FromBool(_image.fillCenter);
                    break;
                case ImageProperty.PixelsPerUnitMultiplier:
                    valueContainer = ValueContainer.FromFloat(_image.pixelsPerUnitMultiplier);
                    break;
                case ImageProperty.Type:
                    valueContainer = ValueContainer.FromEnum((int)_image.type);
                    break;
                case ImageProperty.FillMethod:
                    valueContainer = ValueContainer.FromEnum((int)_image.fillMethod);
                    break;
                case ImageProperty.FillOrigin:
                    valueContainer = ValueContainer.FromInt(_image.fillOrigin);
                    break;
                case ImageProperty.Sprite:
                    valueContainer = ValueContainer.FromReference(_image.sprite);
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_image == null)
                return false;

            switch (_property)
            {
                case ImageProperty.FillAmount:
                    _image.fillAmount = valueContainer.FloatValue;
                    break;
                case ImageProperty.FillClockwise:
                    _image.fillClockwise = valueContainer.BoolValue;
                    break;
                case ImageProperty.PreserveAspect:
                    _image.preserveAspect = valueContainer.BoolValue;
                    break;
                case ImageProperty.FillCenter:
                    _image.fillCenter = valueContainer.BoolValue;
                    break;
                case ImageProperty.PixelsPerUnitMultiplier:
                    _image.pixelsPerUnitMultiplier = valueContainer.FloatValue;
                    break;
                case ImageProperty.Type:
                    _image.type = (Image.Type)valueContainer.EnumValue;
                    break;
                case ImageProperty.FillMethod:
                    _image.fillMethod = (Image.FillMethod)valueContainer.EnumValue;
                    break;
                case ImageProperty.FillOrigin:
                    _image.fillOrigin = valueContainer.IntValue;
                    break;
                case ImageProperty.Sprite:
                    _image.sprite = valueContainer.ReferenceValue as Sprite;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private enum ImageProperty
        {
            FillAmount,
            FillClockwise,
            PreserveAspect,
            FillCenter,
            PixelsPerUnitMultiplier,
            Type,
            FillMethod,
            FillOrigin,
            Sprite
        }
    }
}
