using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimboJimbo.PropertyBindings
{
    public static class GameObjectProperties
    {
        public static readonly PropertyDescriptor<GameObject, bool> ActiveSelf = new(
            "unity.gameObject.activeSelf", "m_IsActive", ValueKind.Bool, ComponentLayout.One, "m_IsActive");
    }

    public static class BehaviourProperties
    {
        public static readonly PropertyDescriptor<Behaviour, bool> Enabled = new(
            "unity.behaviour.enabled", "m_Enabled", ValueKind.Bool, ComponentLayout.One, "m_Enabled");
    }

    public static class TransformProperties
    {
        public static readonly PropertyDescriptor<Transform, Vector3> LocalPosition = Vector3Descriptor(
            "unity.transform.localPosition", "m_LocalPosition");
        public static readonly PropertyDescriptor<Transform, Quaternion> LocalRotation = QuaternionDescriptor(
            "unity.transform.localRotation", "m_LocalRotation");
        public static readonly PropertyDescriptor<Transform, Vector3> LocalScale = Vector3Descriptor(
            "unity.transform.localScale", "m_LocalScale");

        private static PropertyDescriptor<Transform, Vector3> Vector3Descriptor(string id, string path) => new(
            id, path, ValueKind.Vector3, ComponentLayout.Three, $"{path}.x", $"{path}.y", $"{path}.z");
        private static PropertyDescriptor<Transform, Quaternion> QuaternionDescriptor(string id, string path) => new(
            id, path, ValueKind.Quaternion, ComponentLayout.Four,
            $"{path}.x", $"{path}.y", $"{path}.z", $"{path}.w");
    }

    public static class RectTransformProperties
    {
        public static readonly PropertyDescriptor<RectTransform, Vector2> AnchorMin = Vector2Descriptor(
            "unity.rectTransform.anchorMin", "m_AnchorMin");
        public static readonly PropertyDescriptor<RectTransform, Vector2> AnchorMax = Vector2Descriptor(
            "unity.rectTransform.anchorMax", "m_AnchorMax");
        public static readonly PropertyDescriptor<RectTransform, Vector2> AnchoredPosition = Vector2Descriptor(
            "unity.rectTransform.anchoredPosition", "m_AnchoredPosition");
        public static readonly PropertyDescriptor<RectTransform, Vector2> SizeDelta = Vector2Descriptor(
            "unity.rectTransform.sizeDelta", "m_SizeDelta");
        public static readonly PropertyDescriptor<RectTransform, Vector2> Pivot = Vector2Descriptor(
            "unity.rectTransform.pivot", "m_Pivot");

        private static PropertyDescriptor<RectTransform, Vector2> Vector2Descriptor(string id, string path) => new(
            id, path, ValueKind.Vector2, ComponentLayout.Two, $"{path}.x", $"{path}.y");
    }

    public static class CanvasGroupProperties
    {
        public static readonly PropertyDescriptor<CanvasGroup, float> Alpha = Scalar<float>(
            "unity.canvasGroup.alpha", "m_Alpha", ValueKind.Float);
        public static readonly PropertyDescriptor<CanvasGroup, bool> Interactable = Scalar<bool>(
            "unity.canvasGroup.interactable", "m_Interactable", ValueKind.Bool);
        public static readonly PropertyDescriptor<CanvasGroup, bool> BlocksRaycasts = Scalar<bool>(
            "unity.canvasGroup.blocksRaycasts", "m_BlocksRaycasts", ValueKind.Bool);
        public static readonly PropertyDescriptor<CanvasGroup, bool> IgnoreParentGroups = Scalar<bool>(
            "unity.canvasGroup.ignoreParentGroups", "m_IgnoreParentGroups", ValueKind.Bool);

        private static PropertyDescriptor<CanvasGroup, T> Scalar<T>(string id, string path, ValueKind kind) =>
            new(id, path, kind, ComponentLayout.One, path);
    }

    public static class GraphicProperties
    {
        public static readonly PropertyDescriptor<Graphic, Color> Color = new(
            "unity.ui.graphic.color", "m_Color", ValueKind.Color, ComponentLayout.Four,
            "m_Color.r", "m_Color.g", "m_Color.b", "m_Color.a");
        public static readonly PropertyDescriptor<Graphic, bool> RaycastTarget = new(
            "unity.ui.graphic.raycastTarget", "m_RaycastTarget", ValueKind.Bool, ComponentLayout.One, "m_RaycastTarget");
        public static readonly PropertyDescriptor<Graphic, Vector4> RaycastPadding = new(
            "unity.ui.graphic.raycastPadding", "m_RaycastPadding", ValueKind.Vector4, ComponentLayout.Four,
            "m_RaycastPadding.x", "m_RaycastPadding.y", "m_RaycastPadding.z", "m_RaycastPadding.w");
    }

    public static class SelectableProperties
    {
        public static readonly PropertyDescriptor<Selectable, bool> Interactable = new(
            "unity.ui.selectable.interactable", "m_Interactable", ValueKind.Bool, ComponentLayout.One, "m_Interactable");
    }

    public static class ImageProperties
    {
        public static readonly PropertyDescriptor<Image, float> FillAmount = Scalar<float>(
            "unity.ui.image.fillAmount", "m_FillAmount", ValueKind.Float);
        public static readonly PropertyDescriptor<Image, bool> FillClockwise = Scalar<bool>(
            "unity.ui.image.fillClockwise", "m_FillClockwise", ValueKind.Bool);
        public static readonly PropertyDescriptor<Image, bool> PreserveAspect = Scalar<bool>(
            "unity.ui.image.preserveAspect", "m_PreserveAspect", ValueKind.Bool);
        public static readonly PropertyDescriptor<Image, bool> FillCenter = Scalar<bool>(
            "unity.ui.image.fillCenter", "m_FillCenter", ValueKind.Bool);
        public static readonly PropertyDescriptor<Image, float> PixelsPerUnitMultiplier = Scalar<float>(
            "unity.ui.image.pixelsPerUnitMultiplier", "m_PixelsPerUnitMultiplier", ValueKind.Float);
        public static readonly PropertyDescriptor<Image, Image.Type> Type = Scalar<Image.Type>(
            "unity.ui.image.type", "m_Type", ValueKind.Enum);
        public static readonly PropertyDescriptor<Image, Image.FillMethod> FillMethod = Scalar<Image.FillMethod>(
            "unity.ui.image.fillMethod", "m_FillMethod", ValueKind.Enum);
        public static readonly PropertyDescriptor<Image, int> FillOrigin = Scalar<int>(
            "unity.ui.image.fillOrigin", "m_FillOrigin", ValueKind.Int);
        public static readonly PropertyDescriptor<Image, Sprite> Sprite = Scalar<Sprite>(
            "unity.ui.image.sprite", "m_Sprite", ValueKind.Reference);

        private static PropertyDescriptor<Image, T> Scalar<T>(string id, string path, ValueKind kind) =>
            new(id, path, kind, ComponentLayout.One, path);
    }

    public static class SpriteRendererProperties
    {
        public static readonly PropertyDescriptor<SpriteRenderer, Color> Color = new(
            "unity.spriteRenderer.color", "m_Color", ValueKind.Color, ComponentLayout.Four,
            "m_Color.r", "m_Color.g", "m_Color.b", "m_Color.a");
        public static readonly PropertyDescriptor<SpriteRenderer, Vector2> Size = new(
            "unity.spriteRenderer.size", "m_Size", ValueKind.Vector2, ComponentLayout.Two, "m_Size.x", "m_Size.y");
        public static readonly PropertyDescriptor<SpriteRenderer, bool> FlipX = Scalar<bool>(
            "unity.spriteRenderer.flipX", "m_FlipX", ValueKind.Bool);
        public static readonly PropertyDescriptor<SpriteRenderer, bool> FlipY = Scalar<bool>(
            "unity.spriteRenderer.flipY", "m_FlipY", ValueKind.Bool);

        private static PropertyDescriptor<SpriteRenderer, T> Scalar<T>(string id, string path, ValueKind kind) =>
            new(id, path, kind, ComponentLayout.One, path);
    }

    public static class CameraProperties
    {
        public static readonly PropertyDescriptor<Camera, float> FieldOfView = Scalar<float>(
            "unity.camera.fieldOfView", "field of view", ValueKind.Float);
        public static readonly PropertyDescriptor<Camera, float> OrthographicSize = Scalar<float>(
            "unity.camera.orthographicSize", "orthographic size", ValueKind.Float);
        public static readonly PropertyDescriptor<Camera, Color> BackgroundColor = new(
            "unity.camera.backgroundColor", "m_BackGroundColor", ValueKind.Color, ComponentLayout.Four,
            "m_BackGroundColor.r", "m_BackGroundColor.g", "m_BackGroundColor.b", "m_BackGroundColor.a");
        public static readonly PropertyDescriptor<Camera, float> NearClipPlane = Scalar<float>(
            "unity.camera.nearClipPlane", "near clip plane", ValueKind.Float);
        public static readonly PropertyDescriptor<Camera, float> FarClipPlane = Scalar<float>(
            "unity.camera.farClipPlane", "far clip plane", ValueKind.Float);

        private static PropertyDescriptor<Camera, T> Scalar<T>(string id, string path, ValueKind kind) =>
            new(id, path, kind, ComponentLayout.One, path);
    }

    internal static class UnityPropertyDescriptors
    {
        public static void RegisterAll(Action<IPropertyDescriptor> register)
        {
            register(GameObjectProperties.ActiveSelf);
            register(BehaviourProperties.Enabled);
            register(TransformProperties.LocalPosition);
            register(TransformProperties.LocalRotation);
            register(TransformProperties.LocalScale);
            register(RectTransformProperties.AnchorMin);
            register(RectTransformProperties.AnchorMax);
            register(RectTransformProperties.AnchoredPosition);
            register(RectTransformProperties.SizeDelta);
            register(RectTransformProperties.Pivot);
            register(CanvasGroupProperties.Alpha);
            register(CanvasGroupProperties.Interactable);
            register(CanvasGroupProperties.BlocksRaycasts);
            register(CanvasGroupProperties.IgnoreParentGroups);
            register(GraphicProperties.Color);
            register(GraphicProperties.RaycastTarget);
            register(GraphicProperties.RaycastPadding);
            register(SelectableProperties.Interactable);
            register(ImageProperties.FillAmount);
            register(ImageProperties.FillClockwise);
            register(ImageProperties.PreserveAspect);
            register(ImageProperties.FillCenter);
            register(ImageProperties.PixelsPerUnitMultiplier);
            register(ImageProperties.Type);
            register(ImageProperties.FillMethod);
            register(ImageProperties.FillOrigin);
            register(ImageProperties.Sprite);
            register(SpriteRendererProperties.Color);
            register(SpriteRendererProperties.Size);
            register(SpriteRendererProperties.FlipX);
            register(SpriteRendererProperties.FlipY);
            register(CameraProperties.FieldOfView);
            register(CameraProperties.OrthographicSize);
            register(CameraProperties.BackgroundColor);
            register(CameraProperties.NearClipPlane);
            register(CameraProperties.FarClipPlane);
        }
    }
}
