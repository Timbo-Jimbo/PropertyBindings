using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class GameObjectActivationPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private GameObject _gameObject;

        public GameObjectActivationPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _gameObject))
                throw new System.ArgumentException($"GameObjectPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        private static bool TryGetBindingInfo(BindableProperty property, out GameObject gameObject)
        {
            gameObject = property.Target as GameObject;

            if (gameObject == null || !GameObjectProperties.ActiveSelf.Matches(property))
            {
                gameObject = null;
                return false;
            }

            return true;
        }

        public override void Dispose()
        {
            _gameObject = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
            if (_gameObject == null)
                return false;

            _gameObject.SetActive(valueContainer.BoolValue);
            return true;
        }

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_gameObject == null)
                return false;

            valueContainer = ValueContainer.FromBool(_gameObject.activeSelf);
            return true;
        }
    }
}
