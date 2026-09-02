using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TimboJimbo.PropertyBindings;
using TimboJimbo.PropertyBindings.Bindings;
using TimboJimboEditor.PropertyBindings.Utility;
using UnityEngine;

namespace TimboJimboTests.PropertyBindings
{
    [TestFixture]
    public class PropertyDescriptorTests
    {
        [Serializable]
        private sealed class PropertyHolder
        {
            public BindableProperty Property;
        }

        private GameObject _go;

        [SetUp]
        public void SetUp() => _go = new GameObject("PropertyDescriptorTest");

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_go);

        [Test]
        public void DescriptorConstruction_PersistsStableId()
        {
            var property = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);
            var json = JsonUtility.ToJson(new PropertyHolder { Property = property });

            Assert.IsTrue(property.HasDescriptor);
            Assert.IsFalse(property.IsAdHoc);
            Assert.AreEqual(TransformProperties.LocalPosition.Id, property.DescriptorId);
            StringAssert.Contains(TransformProperties.LocalPosition.Id, json);
            StringAssert.Contains("\"_path\":\"\"", json);
        }

        [Test]
        public void DescriptorIdentity_UsesTargetAndStableId()
        {
            var first = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);
            var second = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);
            var dictionary = new Dictionary<BindableProperty, int> { [first] = 42 };

            Assert.AreEqual(first, second);
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
            Assert.IsTrue(dictionary.TryGetValue(second, out var value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void RegisteredDescriptorId_IsAuthoritativeOverStaleSerializedMetadata()
        {
            var property = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);
            SetPrivateField(ref property, "_path", "stale.path");
            SetPrivateField(ref property, "_kind", ValueKind.Bool);

            Assert.AreEqual(TransformProperties.LocalPosition.SerializedPath, property.Path);
            Assert.AreEqual(TransformProperties.LocalPosition.Kind, property.Kind);
            Assert.IsTrue(property.TryGetDescriptor(out var descriptor));
            Assert.AreEqual(TransformProperties.LocalPosition.Id, descriptor.Id);
        }

        [Test]
        public void WrongDescriptorId_IsRejectedInsteadOfMatchingRawMetadata()
        {
            var property = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);
            SetPrivateField(ref property, "_descriptorId", GameObjectProperties.ActiveSelf.Id);

            Assert.IsFalse(property.TryGetDescriptor(out var resolved));
            Assert.IsNull(resolved);
            Assert.IsFalse(TransformProperties.LocalPosition.Matches(property));
        }

        [Test]
        public void AdHocProperty_WithCanonicalShape_DoesNotImpersonateDescriptor()
        {
            var adHoc = BindableProperty.CreateAdHoc(
                _go.transform, "m_LocalPosition", ValueKind.Vector3,
                ComponentLayout.Three, "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z");
            var descriptorProperty = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);

            Assert.IsTrue(adHoc.IsAdHoc);
            Assert.IsFalse(TransformProperties.LocalPosition.Matches(adHoc));
            Assert.AreNotEqual(descriptorProperty, adHoc);
            Assert.AreNotEqual(typeof(TransformPropertyBinding),
                PropertyBindingRegistry.ResolveBindingType(_go, adHoc));
        }

        [Test]
        public void EditorDiscovery_ReturnsDescriptorBackedBuiltInProperties()
        {
            var properties = new List<BindableProperty>();
            BindablePropertyUtility.GetBindableProperties(_go, properties, recursive: false);

            var position = properties.Find(property =>
                property.Target == _go.transform && property.Path == "m_LocalPosition");

            Assert.AreNotEqual(BindableProperty.Invalid, position);
            Assert.AreEqual(TransformProperties.LocalPosition.Id, position.DescriptorId);
        }

        [Test]
        public void BindingDiagnostics_ExposeResolvedDescriptorAndMatchKind()
        {
            var property = BindableProperty.Create(_go.transform, TransformProperties.LocalPosition);
            var report = PropertyBindingRegistry.Diagnose(_go, property);

            Assert.IsTrue(report.Success);
            Assert.AreEqual(TransformProperties.LocalPosition.Id, report.ResolvedDescriptor.Id);
            Assert.AreEqual(typeof(TransformPropertyBinding), report.SelectedBindingType);
            Assert.IsTrue(ContainsMatchedDescriptorCandidate(
                report.Candidates, typeof(TransformPropertyBinding), TransformProperties.LocalPosition.Id));
        }

        [Test]
        public void Descriptor_RejectsValueKindThatDisagreesWithValueType()
        {
            Assert.Throws<ArgumentException>(() =>
                new PropertyDescriptor<Transform, float>(
                    "tests.invalid-kind", "m_LocalPosition", ValueKind.Vector3,
                    ComponentLayout.One, "m_LocalPosition.x"));
        }

        private static bool ContainsMatchedDescriptorCandidate(
            IReadOnlyList<BindingCandidateReport> candidates, Type bindingType, string descriptorId)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.BindingType != bindingType || !candidate.Matched ||
                    candidate.MatchKind != BindingMatchKind.Descriptor)
                    continue;

                for (int j = 0; j < candidate.DescriptorIds.Count; j++)
                    if (candidate.DescriptorIds[j] == descriptorId)
                        return true;
            }

            return false;
        }

        private static void SetPrivateField<T>(ref BindableProperty property, string name, T value)
        {
            object boxed = property;
            typeof(BindableProperty).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(boxed, value);
            property = (BindableProperty)boxed;
        }
    }
}
