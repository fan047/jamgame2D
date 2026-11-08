using UnityEngine;

namespace Holedown
{
    // Short fade and slight scale for overlay panels; uses unscaled time so pause menus still animate.
    [DisallowMultipleComponent]
    public sealed class PanelTransition : MonoBehaviour
    {
        [SerializeField] float fadeSeconds = 0.14f;
        [SerializeField] float fromScale = 0.96f;
        CanvasGroup group;
        RectTransform rect;
        float elapsed;
        bool shown;
        public float FadeSeconds { get => fadeSeconds; set => fadeSeconds = Mathf.Max(0f, value); }
        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
            rect = transform as RectTransform;
        }
        void OnEnable() { Play(true); }
        void OnDisable() { shown = false; }
        // Called by views when the panel's visibility is driven by state instead of activation.
        public void Play(bool visible)
        {
            if (!group) { group = GetComponent<CanvasGroup>(); if (!group) group = gameObject.AddComponent<CanvasGroup>(); }
            if (!rect) rect = transform as RectTransform;
            shown = visible; elapsed = 0f;
            group.alpha = visible && fadeSeconds > 0f ? 0f : visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            if (rect) rect.localScale = Vector3.one * (visible && fadeSeconds > 0f ? fromScale : 1f);
        }
        void Update()
        {
            if (!group || !shown || fadeSeconds <= 0f) return;
            if (elapsed >= fadeSeconds) { group.alpha = 1f; if (rect) rect.localScale = Vector3.one; return; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeSeconds), eased = 1f - Mathf.Pow(1f - t, 2f);
            group.alpha = eased;
            if (rect) rect.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, eased);
        }
    }
}
