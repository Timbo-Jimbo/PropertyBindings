using System.Collections;
using NUnit.Framework;
using TimboJimbo.PropertyBindings;
using TimboJimbo.PropertyBindings.Bindings;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TimboJimboTests.PropertyBindings.PlayMode
{
    public sealed class GraphicPropertyBindingPlayModeTests
    {
        private GameObject _canvasRoot;
        private GameObject _graphicObject;
        private SetterAwareGraphic _graphic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _canvasRoot = new GameObject("Canvas", typeof(Canvas));
            _canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _graphicObject = new GameObject("Graphic", typeof(RectTransform), typeof(CanvasRenderer));
            _graphicObject.transform.SetParent(_canvasRoot.transform, false);
            _graphic = _graphicObject.AddComponent<SetterAwareGraphic>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_canvasRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ColorBinding_UsesVirtualSetterAndPreservesFinalValueOnDispose()
        {
            var property = BindableProperty.Create(_graphic, GraphicProperties.Color);
            var finalColor = new Color(0.2f, 0.7f, 0.9f, 0.6f);
            int setterCallsBeforeWrite = _graphic.ColorSetterCallCount;

            using (var collection = PropertyBindingCollection.Bind(_canvasRoot, new[] { property }))
            {
                Assert.AreEqual(typeof(GraphicPropertyBinding),
                    PropertyBindingRegistry.ResolveBindingType(_canvasRoot, property));
                Assert.IsTrue(collection.TryWrite(property, ValueContainer.From(finalColor)));
                Assert.Greater(_graphic.ColorSetterCallCount, setterCallsBeforeWrite);
                Assert.AreEqual(finalColor, _graphic.GeneratedColor);
                yield return null;
            }

            Assert.AreEqual(finalColor, _graphic.color);
            Assert.AreEqual(finalColor, _graphic.GeneratedColor);
        }

        [UnityTest]
        public IEnumerator RepeatedBulkWrites_DoNotOverflowAndReachFinalColor()
        {
            var property = BindableProperty.Create(_graphic, GraphicProperties.Color);
            var finalColor = new Color(0.9f, 0.3f, 0.1f, 1f);

            using (var collection = PropertyBindingCollection.Bind(_canvasRoot, new[] { property }))
            using (collection.BulkWriteScope())
            {
                for (int i = 0; i < 64; i++)
                {
                    var color = Color.Lerp(Color.black, finalColor, i / 63f);
                    Assert.IsTrue(collection.TryWrite(property, ValueContainer.From(color)));
                }
            }

            yield return null;
            Assert.AreEqual(finalColor, _graphic.color);
            Assert.AreEqual(finalColor, _graphic.GeneratedColor);
        }

        private sealed class SetterAwareGraphic : MaskableGraphic
        {
            public int ColorSetterCallCount { get; private set; }
            public Color GeneratedColor { get; private set; }

            public override Color color
            {
                get => base.color;
                set
                {
                    ColorSetterCallCount++;
                    GeneratedColor = value;
                    base.color = value;
                }
            }

            protected override void OnPopulateMesh(VertexHelper vertexHelper)
            {
                vertexHelper.Clear();
                var vertex = UIVertex.simpleVert;
                vertex.color = GeneratedColor;
                vertex.position = Vector3.zero;
                vertexHelper.AddVert(vertex);
                vertex.position = Vector3.right;
                vertexHelper.AddVert(vertex);
                vertex.position = Vector3.up;
                vertexHelper.AddVert(vertex);
                vertexHelper.AddTriangle(0, 1, 2);
            }
        }
    }
}
