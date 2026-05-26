using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class CameraPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private Camera _camera;
        private CameraProperty _property;

        public CameraPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _camera, out _property))
                throw new System.ArgumentException($"CameraPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        public static bool CanBind(BindableProperty property)
        {
            return TryGetBindingInfo(property, out _, out _);
        }

        private static bool TryGetBindingInfo(BindableProperty property, out Camera camera, out CameraProperty cameraProperty)
        {
            camera = property.Target as Camera;

            if (camera == null)
            {
                cameraProperty = default;
                return false;
            }

            switch (property.Path)
            {
                case "field of view":
                    cameraProperty = CameraProperty.FieldOfView;
                    return true;
                case "orthographic size":
                    cameraProperty = CameraProperty.OrthographicSize;
                    return true;
                case "m_BackGroundColor":
                    cameraProperty = CameraProperty.BackgroundColor;
                    return true;
                case "near clip plane":
                    cameraProperty = CameraProperty.NearClipPlane;
                    return true;
                case "far clip plane":
                    cameraProperty = CameraProperty.FarClipPlane;
                    return true;
                default:
                    cameraProperty = default;
                    return false;
            }
        }

        public override void Dispose()
        {
            _camera = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_camera == null)
                return false;

            switch (_property)
            {
                case CameraProperty.FieldOfView:
                    valueContainer = ValueContainer.FromFloat(_camera.fieldOfView);
                    break;
                case CameraProperty.OrthographicSize:
                    valueContainer = ValueContainer.FromFloat(_camera.orthographicSize);
                    break;
                case CameraProperty.BackgroundColor:
                    valueContainer = ValueContainer.FromColor(_camera.backgroundColor);
                    break;
                case CameraProperty.NearClipPlane:
                    valueContainer = ValueContainer.FromFloat(_camera.nearClipPlane);
                    break;
                case CameraProperty.FarClipPlane:
                    valueContainer = ValueContainer.FromFloat(_camera.farClipPlane);
                    break;
                default:
                    return false;
            }

            return true;
        }

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_camera == null)
                return false;

            switch (_property)
            {
                case CameraProperty.FieldOfView:
                    _camera.fieldOfView = valueContainer.FloatValue;
                    break;
                case CameraProperty.OrthographicSize:
                    _camera.orthographicSize = valueContainer.FloatValue;
                    break;
                case CameraProperty.BackgroundColor:
                    _camera.backgroundColor = valueContainer.ColorValue;
                    break;
                case CameraProperty.NearClipPlane:
                    _camera.nearClipPlane = valueContainer.FloatValue;
                    break;
                case CameraProperty.FarClipPlane:
                    _camera.farClipPlane = valueContainer.FloatValue;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private enum CameraProperty
        {
            FieldOfView,
            OrthographicSize,
            BackgroundColor,
            NearClipPlane,
            FarClipPlane
        }
    }
}
