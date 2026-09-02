using UnityEngine;

namespace TimboJimbo.PropertyBindings.Bindings
{
    public sealed class BehaviourActivationPropertyBinding : OptimizedReadWritePropertyBinding
    {
        private Behaviour _behaviour;

        public BehaviourActivationPropertyBinding(
            GameObject root,
            BindableProperty property
        ) : base(
            root: root, 
            optimizationConfig: OptimizationConfig.Aggressive
        )
        {
            if(!TryGetBindingInfo(property, out _behaviour))
                throw new System.ArgumentException($"BehaviourPropertyBinding does not support property path: {property.Path}", nameof(property));
        }

        private static bool TryGetBindingInfo(BindableProperty property, out Behaviour behaviour)
        {
            behaviour = property.Target as Behaviour;

            if (behaviour == null || !BehaviourProperties.Enabled.Matches(property))
            {
                behaviour = null;
                return false;
            }

            return true;
        }

        public override void Dispose()
        {
            _behaviour = null;
        }

        protected override bool TargetMustBeNotifiedOnWrite() => false;

        protected override bool TryWriteToTarget(ValueContainer valueContainer)
        {
             if (_behaviour == null)
                return false;

            _behaviour.enabled = valueContainer.BoolValue;
            return true;
        }

        protected override bool TryReadFromTarget(out ValueContainer valueContainer)
        {
            valueContainer = default;

            if (_behaviour == null)
                return false;

            valueContainer = ValueContainer.FromBool(_behaviour.enabled);
            return true;
        }
    }
}
