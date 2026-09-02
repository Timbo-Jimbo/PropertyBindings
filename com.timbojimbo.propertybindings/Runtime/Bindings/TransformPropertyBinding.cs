using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class TransformPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private TransformProperty _transformProperty;
        private Transform _transform;
        private RectTransform _rectTransform; 

        public TransformPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _transform, out _rectTransform, out _transformProperty))
                throw new System.ArgumentException($"TransformPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        private static bool TryGetBindingInfo(BindableProperty property, out Transform transform, out RectTransform rectTransform, out TransformProperty transformProperty)
        {
            transform = property.Target as Transform;
            rectTransform = null;

            if (transform == null)
            {
                transformProperty = default;
                return false;
            }

            if (TransformProperties.LocalPosition.Matches(property))
            {
                transformProperty = TransformProperty.Position;
                return true;
            }
            if (TransformProperties.LocalRotation.Matches(property))
            {
                transformProperty = TransformProperty.Rotation;
                return true;
            }
            if (TransformProperties.LocalScale.Matches(property))
            {
                transformProperty = TransformProperty.Scale;
                return true;
            }

            if (RectTransformProperties.AnchorMin.Matches(property)) transformProperty = TransformProperty.AnchorMin;
            else if (RectTransformProperties.AnchorMax.Matches(property)) transformProperty = TransformProperty.AnchorMax;
            else if (RectTransformProperties.AnchoredPosition.Matches(property)) transformProperty = TransformProperty.AnchoredPosition;
            else if (RectTransformProperties.SizeDelta.Matches(property)) transformProperty = TransformProperty.SizeDelta;
            else if (RectTransformProperties.Pivot.Matches(property)) transformProperty = TransformProperty.Pivot;
            else
            {
                transform = null;
                transformProperty = default;
                return false;
            }

            // RectTransform-specific properties
            rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                transform = null;
                transformProperty = default;
                return false;
            }

            return true;
        }

        public override void Dispose()
        {
            _transform = null;
            _rectTransform = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;
            
            if (_transform == null)
                    return false;

            switch (_transformProperty)
            {
                case TransformProperty.Position:
                    valueContainer = ValueContainer.FromVector3(_transform.localPosition);
                    break;
                case TransformProperty.Rotation:
                    valueContainer = ValueContainer.FromQuaternion(_transform.localRotation);
                    break;
                case TransformProperty.Scale:
                    valueContainer = ValueContainer.FromVector3(_transform.localScale);
                    break;
                case TransformProperty.AnchorMin:
                    valueContainer = ValueContainer.FromVector2(_rectTransform.anchorMin);
                    break;
                case TransformProperty.AnchorMax:
                    valueContainer = ValueContainer.FromVector2(_rectTransform.anchorMax);
                    break;
                case TransformProperty.AnchoredPosition:
                    valueContainer = ValueContainer.FromVector2(_rectTransform.anchoredPosition);
                    break;
                case TransformProperty.SizeDelta:
                    valueContainer = ValueContainer.FromVector2(_rectTransform.sizeDelta);
                    break;
                case TransformProperty.Pivot:
                    valueContainer = ValueContainer.FromVector2(_rectTransform.pivot);
                    break;
                default:
                    return false; // should never happen
            }

            return true;
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_transform == null)
                return false;

            switch (_transformProperty)
            {
                case TransformProperty.Position:
                {
                    _transform.localPosition = valueContainer.Vector3Value;
                    break;   
                }

                case TransformProperty.Rotation:
                {
                    _transform.localRotation = valueContainer.QuaternionValue;
                    break;
                }

                case TransformProperty.Scale:
                {
                    _transform.localScale = valueContainer.Vector3Value;
                    break;
                }

                case TransformProperty.AnchorMin:
                {
                    if (_transform is RectTransform rectTransform1)
                    {
                        rectTransform1.anchorMin = valueContainer.Vector2Value;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                }

                case TransformProperty.AnchorMax:
                {
                    if (_transform is RectTransform rectTransform2)
                    {
                        rectTransform2.anchorMax = valueContainer.Vector2Value;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                }

                case TransformProperty.AnchoredPosition:
                {
                    if (_transform is RectTransform rectTransform3)
                    {
                        rectTransform3.anchoredPosition = valueContainer.Vector2Value;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                }

                case TransformProperty.SizeDelta:
                {
                    if (_transform is RectTransform rectTransform4)
                    {
                        rectTransform4.sizeDelta = valueContainer.Vector2Value;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                }

                case TransformProperty.Pivot:
                {
                    if (_transform is RectTransform rectTransform5)
                    {
                        rectTransform5.pivot = valueContainer.Vector2Value;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                }

                default:
                    return false; // should never happen
            }

            return true;
        }
        
        private enum TransformProperty
        {
            Position,
            Rotation,
            Scale,
            AnchorMin,
            AnchorMax,
            AnchoredPosition,
            SizeDelta,
            Pivot
        }
    }
}