using System;
using NUnit.Framework;
using TimboJimbo.PropertyBindings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimboTests.PropertyBindings
{    
    [TestFixture]
    public class PropertyBindingCollectionTests
    {
        GameObject _go;
        GameObject _child;
        PropertyBag _comp;
        PropertyBag _childComp;
        NotifyOnWrite _notifyComp;
        PrivatePropertyBag _privateBag;
        SerializedFieldPropertyBag _serializedFieldBag;
        ArrayPropertyBag _arrayBag;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PropertyBindingTest");
            _child = new GameObject("Child");
            _child.transform.SetParent(_go.transform);
            _comp = _go.AddComponent<PropertyBag>();
            _childComp = _child.AddComponent<PropertyBag>();
            _notifyComp = _go.AddComponent<NotifyOnWrite>();
            _privateBag = _go.AddComponent<PrivatePropertyBag>();
            _serializedFieldBag = _go.AddComponent<SerializedFieldPropertyBag>();
            _arrayBag = _go.AddComponent<ArrayPropertyBag>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        // ───────────── Test 5 ─────────────

        [Test]
        public void Float_ReadWrite()
        {
            _comp.Test = 123f;

            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.Test), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(123f, read.FloatValue, 0.001f);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(456f)));
                Assert.AreEqual(456f, _comp.Test, 0.001f);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(456f, readBack.FloatValue, 0.001f);
            }
        }

        [Test]
        public void Bool_ReadWrite()
        {
            _comp.Test2 = false;

            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.Test2), ValueKind.Bool);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.IsFalse(read.BoolValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(true)));
                Assert.IsTrue(_comp.Test2);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.IsTrue(readBack.BoolValue);
            }
        }

        [Test]
        public void BulkWrite_TwoFloats()
        {
            _comp.TestFloatA = 1f;
            _comp.TestFloatB = 2f;

            var propA = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestFloatA), ValueKind.Float);
            var propB = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestFloatB), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { propA, propB }))
            {
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryWrite(propA, ValueContainer.FromFloat(100f)));
                    Assert.IsTrue(collection.TryWrite(propB, ValueContainer.FromFloat(200f)));
                }

                Assert.AreEqual(100f, _comp.TestFloatA, 0.001f);
                Assert.AreEqual(200f, _comp.TestFloatB, 0.001f);
            }
        }

        [Test]
        public void NestedBulkWrite()
        {
            _comp.TestFloatA = 0f;

            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestFloatA), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                using(collection.StartBulkWriteScope())
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryBulkWrite(prop, ValueContainer.FromFloat(42f)));
                    Assert.AreEqual(42f, _comp.TestFloatA, 0.001f);
                }
            }
        }

        [Test]
        public void TryDirectWrite_Throws_DuringBulkWriteScope()
        {
            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestFloatA), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                using (collection.StartBulkWriteScope())
                {
                    Assert.Throws<InvalidOperationException>(() =>
                        collection.TryDirectWrite(prop, ValueContainer.FromFloat(42f)));
                }
            }
        }

        [Test]
        public void TryBulkWrite_Throws_WhenNoBulkWriteInProgress()
        {
            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestFloatA), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.Throws<InvalidOperationException>(() =>
                    collection.TryBulkWrite(prop, ValueContainer.FromFloat(42f)));
            }
        }

        [Test]
        public void TransformPosition_ReadWrite()
        {
            var posProperty = BindableProperty.CreateThreeComponent(_go.transform, "m_LocalPosition", ValueKind.Vector3, "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { posProperty }))
            {
                Assert.IsTrue(collection.TryRead(posProperty, out var posRead));

                var newPos = new Vector3(10f, 20f, 30f);
                Assert.IsTrue(collection.TryWrite(posProperty, ValueContainer.FromVector3(newPos)));
                Assert.That(Vector3.Distance(_go.transform.localPosition, newPos), Is.LessThan(0.001f));

                Assert.IsTrue(collection.TryRead(posProperty, out var posReadBack));
                Assert.That(Vector3.Distance(posReadBack.Vector3Value, newPos), Is.LessThan(0.001f));
            }
        }

        // ───────────── Test 6 ─────────────

        [Test]
        public void Vector3_ReadWrite()
        {
            _comp.TestVector3 = new Vector3(1f, 2f, 3f);

            var prop = BindableProperty.CreateThreeComponent(_comp, nameof(PropertyBag.TestVector3), ValueKind.Vector3, $"{nameof(PropertyBag.TestVector3)}.x", $"{nameof(PropertyBag.TestVector3)}.y", $"{nameof(PropertyBag.TestVector3)}.z");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.That(Vector3.Distance(read.Vector3Value, new Vector3(1, 2, 3)), Is.LessThan(0.001f));

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromVector3(new Vector3(10f, 20f, 30f))));
                Assert.That(Vector3.Distance(_comp.TestVector3, new Vector3(10, 20, 30)), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void Vector2_ReadWrite()
        {
            _comp.TestVector2 = new Vector2(4f, 5f);

            var prop = BindableProperty.CreateTwoComponent(_comp, nameof(PropertyBag.TestVector2), ValueKind.Vector2, $"{nameof(PropertyBag.TestVector2)}.x", $"{nameof(PropertyBag.TestVector2)}.y");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.That(Vector2.Distance(read.Vector2Value, new Vector2(4, 5)), Is.LessThan(0.001f));

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromVector2(new Vector2(99f, 88f))));
                Assert.That(Vector2.Distance(_comp.TestVector2, new Vector2(99, 88)), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void Vector4_ReadWrite()
        {
            _comp.TestVector4 = new Vector4(1f, 2f, 3f, 4f);

            var prop = BindableProperty.CreateFourComponent(_comp, nameof(PropertyBag.TestVector4), ValueKind.Vector4, $"{nameof(PropertyBag.TestVector4)}.x", $"{nameof(PropertyBag.TestVector4)}.y", $"{nameof(PropertyBag.TestVector4)}.z", $"{nameof(PropertyBag.TestVector4)}.w");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.That(Vector4.Distance(read.Vector4Value, new Vector4(1, 2, 3, 4)), Is.LessThan(0.001f));

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromVector4(new Vector4(5f, 6f, 7f, 8f))));
                Assert.That(Vector4.Distance(_comp.TestVector4, new Vector4(5, 6, 7, 8)), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void Color_ReadWrite()
        {
            _comp.TestColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            var prop = BindableProperty.CreateFourComponent(_comp, nameof(PropertyBag.TestColor), ValueKind.Color, $"{nameof(PropertyBag.TestColor)}.r", $"{nameof(PropertyBag.TestColor)}.g", $"{nameof(PropertyBag.TestColor)}.b", $"{nameof(PropertyBag.TestColor)}.a");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.That(Vector4.Distance((Vector4)read.ColorValue, new Vector4(0.1f, 0.2f, 0.3f, 0.4f)), Is.LessThan(0.001f));

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromColor(new Color(0.5f, 0.6f, 0.7f, 0.8f))));
                Assert.That(Vector4.Distance((Vector4)_comp.TestColor, new Vector4(0.5f, 0.6f, 0.7f, 0.8f)), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void Quaternion_ReadWrite()
        {
            _comp.TestQuaternion = new Quaternion(0f, 0f, 0f, 1f);

            var prop = BindableProperty.CreateFourComponent(_comp, nameof(PropertyBag.TestQuaternion), ValueKind.Quaternion, $"{nameof(PropertyBag.TestQuaternion)}.x", $"{nameof(PropertyBag.TestQuaternion)}.y", $"{nameof(PropertyBag.TestQuaternion)}.z", $"{nameof(PropertyBag.TestQuaternion)}.w");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.That(Quaternion.Angle(read.QuaternionValue, new Quaternion(0, 0, 0, 1)), Is.LessThan(0.01f));

                var writeQ = new Quaternion(0.1f, 0.2f, 0.3f, 0.8f).normalized;
                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromQuaternion(writeQ)));
                Assert.That(Quaternion.Angle(_comp.TestQuaternion, writeQ), Is.LessThan(0.01f));
            }
        }

        [Test]
        public void Quaternion_BulkWrite()
        {
            var prop = BindableProperty.CreateFourComponent(_comp, nameof(PropertyBag.TestQuaternion), ValueKind.Quaternion, $"{nameof(PropertyBag.TestQuaternion)}.x", $"{nameof(PropertyBag.TestQuaternion)}.y", $"{nameof(PropertyBag.TestQuaternion)}.z", $"{nameof(PropertyBag.TestQuaternion)}.w");

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromQuaternion(Quaternion.identity)));
                }

                Assert.AreEqual(Quaternion.identity, _comp.TestQuaternion);
            }
        }

        // ───────────── NotifyOnWrite Tests ─────────────

        [Test]
        public void NotifyOnWrite_SingleWrite_CallsOnDidApplyAnimationProperties()
        {
            _notifyComp.DidApplyAnimationPropertiesBeenCalled = false;

            var prop = BindableProperty.CreateScalar(_notifyComp, nameof(NotifyOnWrite.Test), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(42f)));

                // After a single write, OnDidApplyAnimationProperties should have been called
                Assert.IsTrue(_notifyComp.DidApplyAnimationPropertiesBeenCalled,
                    "Expected OnDidApplyAnimationProperties to be called after TryWrite");
            }
        }

        [Test]
        public void NotifyOnWrite_BulkWrite_DefersNotificationUntilScopeDisposed()
        {
            _notifyComp.DidApplyAnimationPropertiesBeenCalled = false;

            var prop = BindableProperty.CreateScalar(_notifyComp, nameof(NotifyOnWrite.Test), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(99f)));

                    // During bulk write, notification should NOT have been called yet
                    Assert.IsFalse(_notifyComp.DidApplyAnimationPropertiesBeenCalled,
                        "Expected OnDidApplyAnimationProperties NOT to be called during bulk write");
                }

                // After the scope is disposed, notification should have been called
                Assert.IsTrue(_notifyComp.DidApplyAnimationPropertiesBeenCalled,
                    "Expected OnDidApplyAnimationProperties to be called after BulkPropertyWriter disposed");
            }
        }

        [Test]
        public void NotifyOnWrite_ManualBulkWrite_DefersNotificationUntilEnd()
        {
            // This test explicitly exercises the manual StartBulkWrite/EndBulkWrite workflow.
            _notifyComp.DidApplyAnimationPropertiesBeenCalled = false;

            var prop = BindableProperty.CreateScalar(_notifyComp, nameof(NotifyOnWrite.Test), ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                collection.StartBulkWrite();

                Assert.IsTrue(collection.TryBulkWrite(prop, ValueContainer.FromFloat(99f)));

                // During bulk write, notification should NOT have been called yet
                Assert.IsFalse(_notifyComp.DidApplyAnimationPropertiesBeenCalled,
                    "Expected OnDidApplyAnimationProperties NOT to be called during bulk write");

                collection.EndBulkWrite();

                // After EndBulkWrite, notification should have been called
                Assert.IsTrue(_notifyComp.DidApplyAnimationPropertiesBeenCalled,
                    "Expected OnDidApplyAnimationProperties to be called after EndBulkWrite");
            }
        }

        // ───────────── SerializedFieldPropertyBag Tests ─────────────

        [Test]
        public void SerializedFieldAutoProperty_ReadWrite()
        {
            _serializedFieldBag.Test = 5f;

            // [field: SerializeField] on an auto-property generates a backing field named <Test>k__BackingField
            var prop = BindableProperty.CreateScalar(_serializedFieldBag, "<Test>k__BackingField", ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(5f, read.FloatValue, 0.001f);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(99f)));
                Assert.AreEqual(99f, _serializedFieldBag.Test, 0.001f);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(99f, readBack.FloatValue, 0.001f);
            }
        }

        [Test]
        public void SerializedFieldAutoProperty_BulkWrite()
        {
            var prop = BindableProperty.CreateScalar(_serializedFieldBag, "<Test>k__BackingField", ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(123f)));
                }

                Assert.AreEqual(123f, _serializedFieldBag.Test, 0.001f);
            }
        }

        // ───────────── PrivatePropertyBag Tests ─────────────

        [Test]
        public void PrivateSerializedField_ReadWrite()
        {
            var prop = BindableProperty.CreateScalar(_privateBag, "_test", ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                // Initial value should be default (0f)
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(0f, read.FloatValue, 0.001f);

                // Write via binding, read back via GetTest()
                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(42f)));
                Assert.AreEqual(42f, _privateBag.GetTest(), 0.001f);

                // Read back via binding
                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(42f, readBack.FloatValue, 0.001f);
            }
        }

        [Test]
        public void PrivateSerializedField_BulkWrite()
        {
            var prop = BindableProperty.CreateScalar(_privateBag, "_test", ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(77f)));
                }

                Assert.AreEqual(77f, _privateBag.GetTest(), 0.001f);
            }
        }

        // ───────────── Object Reference Tests ─────────────

        [Test]
        public void Reference_ReadWrite()
        {
            var mat = new Material(Shader.Find("Standard")) { name = "TestMat" };
            _comp.TestReference = mat;

            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestReference), ValueKind.Reference);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(mat, read.ReferenceValue);

                var mat2 = new Material(Shader.Find("Standard")) { name = "TestMat2" };
                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromReference(mat2)));
                Assert.AreEqual(mat2, _comp.TestReference);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(mat2, readBack.ReferenceValue);

                Object.DestroyImmediate(mat2);
            }

            Object.DestroyImmediate(mat);
        }

        [Test]
        public void Reference_WriteNull()
        {
            var mat = new Material(Shader.Find("Standard"));
            _comp.TestReference = mat;

            var prop = BindableProperty.CreateScalar(_comp, nameof(PropertyBag.TestReference), ValueKind.Reference);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromReference(null)));
                Assert.IsNull(_comp.TestReference);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.IsNull(readBack.ReferenceValue);
            }

            Object.DestroyImmediate(mat);
        }

        // ───────────── GameObject & Component Toggle Tests ─────────────

        [Test]
        public void ChildGameObject_ToggleActive()
        {
            _child.SetActive(true);

            // m_IsActive is the serialized path for GameObject.activeSelf
            var prop = BindableProperty.CreateScalar(_child, "m_IsActive", ValueKind.Bool);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.IsTrue(read.BoolValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(false)));
                Assert.IsFalse(_child.activeSelf);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.IsFalse(readBack.BoolValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(true)));
                Assert.IsTrue(_child.activeSelf);
            }
        }

        [Test]
        public void Component_ToggleEnabled()
        {
            // Behaviour.m_Enabled is the serialized path for the enabled toggle
            var prop = BindableProperty.CreateScalar(_comp, "m_Enabled", ValueKind.Bool);

            _comp.enabled = true;

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.IsTrue(read.BoolValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(false)));
                Assert.IsFalse(_comp.enabled);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.IsFalse(readBack.BoolValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(true)));
                Assert.IsTrue(_comp.enabled);
            }
        }

        [Test]
        public void ChildComponent_ToggleEnabled()
        {
            _childComp.enabled = true;

            var prop = BindableProperty.CreateScalar(_childComp, "m_Enabled", ValueKind.Bool);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.IsTrue(read.BoolValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(false)));
                Assert.IsFalse(_childComp.enabled);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromBool(true)));
                Assert.IsTrue(_childComp.enabled);
            }
        }

        // ───────────── Multiple Composite Types ─────────────

        [Test]
        public void MultipleCompositeTypes_ReadAndBulkWrite()
        {
            _comp.TestVector2 = Vector2.one;
            _comp.TestVector3 = Vector3.forward;
            _comp.TestColor = Color.magenta;

            var props = new[]
            {
                BindableProperty.CreateTwoComponent(_comp, nameof(PropertyBag.TestVector2), ValueKind.Vector2, $"{nameof(PropertyBag.TestVector2)}.x", $"{nameof(PropertyBag.TestVector2)}.y"),
                BindableProperty.CreateThreeComponent(_comp, nameof(PropertyBag.TestVector3), ValueKind.Vector3, $"{nameof(PropertyBag.TestVector3)}.x", $"{nameof(PropertyBag.TestVector3)}.y", $"{nameof(PropertyBag.TestVector3)}.z"),
                BindableProperty.CreateFourComponent(_comp, nameof(PropertyBag.TestColor), ValueKind.Color, $"{nameof(PropertyBag.TestColor)}.r", $"{nameof(PropertyBag.TestColor)}.g", $"{nameof(PropertyBag.TestColor)}.b", $"{nameof(PropertyBag.TestColor)}.a"),
            };

            using (var collection = PropertyBindingCollection.Bind(_go, props))
            {
                // Read all
                Assert.IsTrue(collection.TryRead(props[0], out var v2Read));
                Assert.IsTrue(collection.TryRead(props[1], out var v3Read));
                Assert.IsTrue(collection.TryRead(props[2], out var cRead));

                Assert.AreEqual(Vector2.one, v2Read.Vector2Value);
                Assert.AreEqual(Vector3.forward, v3Read.Vector3Value);
                Assert.AreEqual(Color.magenta, cRead.ColorValue);

                // Bulk write all
                using (collection.StartBulkWriteScope())
                {
                    Assert.IsTrue(collection.TryWrite(props[0], ValueContainer.FromVector2(Vector2.right)));
                    Assert.IsTrue(collection.TryWrite(props[1], ValueContainer.FromVector3(Vector3.up)));
                    Assert.IsTrue(collection.TryWrite(props[2], ValueContainer.FromColor(Color.cyan)));
                }

                Assert.AreEqual(Vector2.right, _comp.TestVector2);
                Assert.AreEqual(Vector3.up, _comp.TestVector3);
                Assert.AreEqual(Color.cyan, _comp.TestColor);
            }
        }

        // ───────────── Array Path Tests ─────────────

        [Test]
        public void PrimitiveArray_Element_ReadWrite()
        {
            _arrayBag.PrimitiveArray = new[] { 10, 20, 30 };

            var prop = BindableProperty.CreateScalar(_arrayBag, "PrimitiveArray.Array.data[1]", ValueKind.Int);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(20, read.IntValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromInt(99)));
                Assert.AreEqual(99, _arrayBag.PrimitiveArray[1]);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(99, readBack.IntValue);
            }
        }

        [Test]
        public void StructArray_Member_ReadWrite()
        {
            _arrayBag.StructArray = new[]
            {
                new ArrayStructValue { Value = 1.5f },
                new ArrayStructValue { Value = 2.5f },
            };

            var prop = BindableProperty.CreateScalar(_arrayBag, "StructArray.Array.data[0].Value", ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(1.5f, read.FloatValue, 0.001f);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(7.25f)));
                Assert.AreEqual(7.25f, _arrayBag.StructArray[0].Value, 0.001f);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(7.25f, readBack.FloatValue, 0.001f);
            }
        }

        [Test]
        public void ClassArray_Member_ReadWrite()
        {
            _arrayBag.ClassArray = new[]
            {
                new ArrayClassValue { Value = 3.5f },
                new ArrayClassValue { Value = 4.5f },
            };

            var prop = BindableProperty.CreateScalar(_arrayBag, "ClassArray.Array.data[1].Value", ValueKind.Float);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(4.5f, read.FloatValue, 0.001f);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromFloat(8.75f)));
                Assert.AreEqual(8.75f, _arrayBag.ClassArray[1].Value, 0.001f);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(8.75f, readBack.FloatValue, 0.001f);
            }
        }

        [Test]
        public void NestedPrimitiveArrays_Element_ReadWrite()
        {
            _arrayBag.NestedPrimitiveArrays = new[]
            {
                new[] { 1, 2, 3 },
                new[] { 4, 5, 6 },
            };

            var prop = BindableProperty.CreateScalar(_arrayBag, "NestedPrimitiveArrays.Array.data[1].Array.data[2]", ValueKind.Int);

            using (var collection = PropertyBindingCollection.Bind(_go, new[] { prop }))
            {
                Assert.IsTrue(collection.TryRead(prop, out var read));
                Assert.AreEqual(6, read.IntValue);

                Assert.IsTrue(collection.TryWrite(prop, ValueContainer.FromInt(42)));
                Assert.AreEqual(42, _arrayBag.NestedPrimitiveArrays[1][2]);

                Assert.IsTrue(collection.TryRead(prop, out var readBack));
                Assert.AreEqual(42, readBack.IntValue);
            }
        }
    }
}
