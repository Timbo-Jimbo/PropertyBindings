using UnityEngine;
using UnityEngine.UI;
using TimboJimbo.PropertyBindings;
using TimboJimbo.PropertyBindings.Bindings;

namespace TimboJimboTests.PropertyBindings
{
    [System.Serializable]
    internal struct ArrayStructValue
    {
        public float Value;
    }

    [System.Serializable]
    internal class ArrayClassValue
    {
        public float Value;
    }

    // This class matches name of the script, which unity sees 
    // and since it doesnt derive from MonoBehaviour, unity seems to 
    // suppress a bunch of warnings about editor-only componentes
    internal class Components {} 

    internal class NotifyOnWrite : MonoBehaviour
    {
        public float Test;
        public bool DidApplyAnimationPropertiesBeenCalled = false;
        
        public void OnDidApplyAnimationProperties()
        {
            DidApplyAnimationPropertiesBeenCalled = true;
        }
    }
    
    // A minimal MonoBehaviour with fields to bind against.
    // Added via AddComponent in tests and destroyed in TearDown.
    internal class PropertyBag : MonoBehaviour
    {
        public float Test;
        public bool Test2;
        public float TestFloatA;
        public float TestFloatB;
        public Vector2 TestVector2;
        public Vector3 TestVector3;
        public Vector4 TestVector4;
        public Color TestColor;
        public TestEnum TestEnumValue;
        public Quaternion TestQuaternion;
        public Material TestReference;
    }

    internal enum TestEnum
    {
        First,
        Second
    }

    internal class PrivatePropertyBag : MonoBehaviour
    {
        [SerializeField] private float _test;
        
        public float GetTest() => _test;
    }

    internal class SerializedFieldPropertyBag : MonoBehaviour
    {
        [field: SerializeField]
        public float Test { get; set; }
    }

    internal class ArrayPropertyBag : MonoBehaviour
    {
        public int[] PrimitiveArray;
        public ArrayStructValue[] StructArray;
        public ArrayClassValue[] ClassArray;
        public int[][] NestedPrimitiveArrays;
    }

    internal class SetterAwareGraphic : MaskableGraphic
    {
        public int ColorSetterCallCount { get; private set; }

        public override Color color
        {
            get => base.color;
            set
            {
                ColorSetterCallCount++;
                base.color = value;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
        }
    }

    internal sealed class TestPropertyBinding : IPropertyBinding
    {
        public void Dispose() { }
        public ReadResult Read() => new ReadResult { Success = true, Value = ValueContainer.FromFloat(0f) };
        public WriteResult Write(ValueContainer valueContainer) => new WriteResult { Success = true };
    }
}