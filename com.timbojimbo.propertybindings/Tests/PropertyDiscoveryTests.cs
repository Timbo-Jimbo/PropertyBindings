using System.Collections.Generic;
using NUnit.Framework;
using TimboJimbo.PropertyBindings;
using TimboJimboEditor.PropertyBindings.Utility;
using UnityEngine;

namespace TimboJimboTests.PropertyBindings
{
    [TestFixture]
    public class PropertyDiscoveryTests
    {
        GameObject _go;
        PropertyBag _comp;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PropertyDiscoveryTest");
            _comp = _go.AddComponent<PropertyBag>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static BindableProperty FindProperty(List<BindableProperty> properties, Object target, string path)
        {
            foreach (var p in properties)
            {
                if (p.Target == target && p.Path == path)
                    return p;
            }
            return BindableProperty.Invalid;
        }

        [Test]
        public void NullTarget_DoesNotThrow()
        {
            var properties = new List<BindableProperty>();
            Assert.DoesNotThrow(() => BindablePropertyUtility.GetBindableProperties(null, properties));
            Assert.IsEmpty(properties);
        }

        [Test]
        public void FloatField_DiscoveredAsFloat()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.Test));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Float, prop.Kind);
            Assert.AreEqual(ComponentLayout.One, prop.ComponentLayout);
            Assert.AreEqual(prop.ComponentOnePath, prop.Path);
        }

        [Test]
        public void BoolField_DiscoveredAsBool()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.Test2));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Bool, prop.Kind);
            Assert.AreEqual(ComponentLayout.One, prop.ComponentLayout);
        }

        [Test]
        public void Vector2Field_DiscoveredAsVector2()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.TestVector2));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Vector2, prop.Kind);
            Assert.AreEqual(ComponentLayout.Two, prop.ComponentLayout);
            Assert.IsNotNull(prop.ComponentOnePath);
            Assert.IsNotNull(prop.ComponentTwoPath);
            Assert.IsEmpty(prop.ComponentThreePath);
            Assert.IsEmpty(prop.ComponentFourPath);
        }

        [Test]
        public void Vector3Field_DiscoveredAsVector3()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.TestVector3));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Vector3, prop.Kind);
            Assert.AreEqual(ComponentLayout.Three, prop.ComponentLayout);
            Assert.IsNotNull(prop.ComponentOnePath);
            Assert.IsNotNull(prop.ComponentTwoPath);
            Assert.IsNotNull(prop.ComponentThreePath);
            Assert.IsEmpty(prop.ComponentFourPath);
        }

        [Test]
        public void Vector4Field_DiscoveredAsVector4()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.TestVector4));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Vector4, prop.Kind);
            Assert.AreEqual(ComponentLayout.Four, prop.ComponentLayout);
            Assert.IsNotNull(prop.ComponentOnePath);
            Assert.IsNotNull(prop.ComponentTwoPath);
            Assert.IsNotNull(prop.ComponentThreePath);
            Assert.IsNotNull(prop.ComponentFourPath);
        }

        [Test]
        public void ColorField_DiscoveredAsColor()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.TestColor));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Color, prop.Kind);
            Assert.AreEqual(ComponentLayout.Four, prop.ComponentLayout);
        }

        [Test]
        public void QuaternionField_DiscoveredAsQuaternion()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _comp, nameof(PropertyBag.TestQuaternion));
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Quaternion, prop.Kind);
            Assert.AreEqual(ComponentLayout.Four, prop.ComponentLayout);
        }

        [Test]
        public void TransformPosition_DiscoveredAsVector3()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var transform = _go.transform;
            var prop = FindProperty(properties, transform, "m_LocalPosition");
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Vector3, prop.Kind);
            Assert.AreEqual(ComponentLayout.Three, prop.ComponentLayout);
        }

        [Test]
        public void TransformRotation_DiscoveredAsQuaternion()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var transform = _go.transform;
            var prop = FindProperty(properties, transform, "m_LocalRotation");
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Quaternion, prop.Kind);
            Assert.AreEqual(ComponentLayout.Four, prop.ComponentLayout);
        }

        [Test]
        public void TransformScale_DiscoveredAsVector3()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var transform = _go.transform;
            var prop = FindProperty(properties, transform, "m_LocalScale");
            Assert.AreNotEqual(BindableProperty.Invalid, prop);
            Assert.AreEqual(ValueKind.Vector3, prop.Kind);
            Assert.AreEqual(ComponentLayout.Three, prop.ComponentLayout);
        }

        [Test]
        public void GameObjectActive_NotAvailableOnRoot()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var prop = FindProperty(properties, _go, "m_IsActive");
            Assert.AreEqual(BindableProperty.Invalid, prop);
        }

        [Test]
        public void Recursive_DiscoversChildProperties()
        {
            var child = new GameObject("Child");
            child.transform.SetParent(_go.transform);
            var childComp = child.AddComponent<PropertyBag>();

            try
            {
                var properties = new List<BindableProperty>();
                BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: true);

                // Child's float should be discovered
                var prop = FindProperty(properties, childComp, nameof(PropertyBag.Test));
                Assert.AreNotEqual(BindableProperty.Invalid, prop);
                Assert.AreEqual(ValueKind.Float, prop.Kind);

                // Child's Vector3 should be discovered
                var vec3Prop = FindProperty(properties, childComp, nameof(PropertyBag.TestVector3));
                Assert.AreNotEqual(BindableProperty.Invalid, vec3Prop);
                Assert.AreEqual(ValueKind.Vector3, vec3Prop.Kind);
            }
            finally
            {
                Object.DestroyImmediate(child);
            }
        }

        [Test]
        public void NonRecursive_DoesNotDiscoverChildProperties()
        {
            var child = new GameObject("Child");
            child.transform.SetParent(_go.transform);
            var childComp = child.AddComponent<PropertyBag>();

            try
            {
                var properties = new List<BindableProperty>();
                BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

                // Child component's properties should NOT be present
                var prop = FindProperty(properties, childComp, nameof(PropertyBag.Test));
                Assert.AreEqual(BindableProperty.Invalid, prop);
            }
            finally
            {
                Object.DestroyImmediate(child);
            }
        }

        [Test]
        public void MultipleFloatFields_EachDiscoveredSeparately()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var propA = FindProperty(properties, _comp, nameof(PropertyBag.TestFloatA));
            var propB = FindProperty(properties, _comp, nameof(PropertyBag.TestFloatB));

            Assert.AreNotEqual(BindableProperty.Invalid, propA);
            Assert.AreEqual(ValueKind.Float, propA.Kind);
            Assert.AreEqual(ComponentLayout.One, propA.ComponentLayout);

            Assert.AreNotEqual(BindableProperty.Invalid, propB);
            Assert.AreEqual(ValueKind.Float, propB.Kind);
            Assert.AreEqual(ComponentLayout.One, propB.ComponentLayout);

            Assert.AreNotEqual(propA.Path, propB.Path);
        }

        [Test]
        public void CompositeProperties_TransformPropertiesHaveCorrectComponentPaths()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var transform = _go.transform;
            var positionProp = FindProperty(properties, transform, "m_LocalPosition");
            var rotationProp = FindProperty(properties, transform, "m_LocalRotation");
            var scaleProp = FindProperty(properties, transform, "m_LocalScale");

            Assert.AreNotEqual(BindableProperty.Invalid, positionProp);
            Assert.AreEqual(ValueKind.Vector3, positionProp.Kind);
            Assert.AreEqual(ComponentLayout.Three, positionProp.ComponentLayout);
            Assert.AreEqual("m_LocalPosition.x", positionProp.ComponentOnePath);
            Assert.AreEqual("m_LocalPosition.y", positionProp.ComponentTwoPath);
            Assert.AreEqual("m_LocalPosition.z", positionProp.ComponentThreePath);

            Assert.AreNotEqual(BindableProperty.Invalid, rotationProp);
            Assert.AreEqual(ValueKind.Quaternion, rotationProp.Kind);
            Assert.AreEqual(ComponentLayout.Four, rotationProp.ComponentLayout);
            Assert.AreEqual("m_LocalRotation.x", rotationProp.ComponentOnePath);
            Assert.AreEqual("m_LocalRotation.y", rotationProp.ComponentTwoPath);
            Assert.AreEqual("m_LocalRotation.z", rotationProp.ComponentThreePath);
            Assert.AreEqual("m_LocalRotation.w", rotationProp.ComponentFourPath);

            Assert.AreNotEqual(BindableProperty.Invalid, scaleProp);
            Assert.AreEqual(ValueKind.Vector3, scaleProp.Kind);
            Assert.AreEqual(ComponentLayout.Three, scaleProp.ComponentLayout);
            Assert.AreEqual("m_LocalScale.x", scaleProp.ComponentOnePath);
            Assert.AreEqual("m_LocalScale.y", scaleProp.ComponentTwoPath);
            Assert.AreEqual("m_LocalScale.z", scaleProp.ComponentThreePath);
        }
    }
}
