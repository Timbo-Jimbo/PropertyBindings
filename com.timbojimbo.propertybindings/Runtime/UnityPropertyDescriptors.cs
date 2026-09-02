using UnityEngine;
using UnityEngine.UI;

namespace TimboJimbo.PropertyBindings
{
    public static class GraphicProperties
    {
        public static readonly PropertyDescriptor<Graphic, Color> Color = new(
            "unity.ui.graphic.color", "m_Color", ValueKind.Color, ComponentLayout.Four,
            "m_Color.r", "m_Color.g", "m_Color.b", "m_Color.a");
        public static readonly PropertyDescriptor<Graphic, bool> RaycastTarget = new(
            "unity.ui.graphic.raycastTarget", "m_RaycastTarget", ValueKind.Bool, ComponentLayout.One,
            "m_RaycastTarget");
    }

    public static class TransformProperties
    {
        public static readonly PropertyDescriptor<Transform, Vector3> LocalPosition = new(
            "unity.transform.localPosition", "m_LocalPosition", ValueKind.Vector3, ComponentLayout.Three,
            "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z");
        public static readonly PropertyDescriptor<Transform, Quaternion> LocalRotation = new(
            "unity.transform.localRotation", "m_LocalRotation", ValueKind.Quaternion, ComponentLayout.Four,
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w");
        public static readonly PropertyDescriptor<Transform, Vector3> LocalScale = new(
            "unity.transform.localScale", "m_LocalScale", ValueKind.Vector3, ComponentLayout.Three,
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z");
    }

    public static class RectTransformProperties
    {
        public static readonly PropertyDescriptor<RectTransform, Vector2> AnchoredPosition = new(
            "unity.rectTransform.anchoredPosition", "m_AnchoredPosition", ValueKind.Vector2, ComponentLayout.Two,
            "m_AnchoredPosition.x", "m_AnchoredPosition.y");
    }

    public static class CanvasGroupProperties
    {
        public static readonly PropertyDescriptor<CanvasGroup, float> Alpha = new(
            "unity.canvasGroup.alpha", "m_Alpha", ValueKind.Float, ComponentLayout.One, "m_Alpha");
        public static readonly PropertyDescriptor<CanvasGroup, bool> Interactable = new(
            "unity.canvasGroup.interactable", "m_Interactable", ValueKind.Bool, ComponentLayout.One, "m_Interactable");
    }

    public static class SelectableProperties
    {
        public static readonly PropertyDescriptor<Selectable, bool> Interactable = new(
            "unity.ui.selectable.interactable", "m_Interactable", ValueKind.Bool, ComponentLayout.One, "m_Interactable");
    }

    public static class ImageProperties
    {
        public static readonly PropertyDescriptor<Image, float> FillAmount = new(
            "unity.ui.image.fillAmount", "m_FillAmount", ValueKind.Float, ComponentLayout.One, "m_FillAmount");
    }
}