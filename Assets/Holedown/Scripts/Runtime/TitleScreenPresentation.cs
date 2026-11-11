using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    // Presentation only: the flow controller remains the sole owner of starting a game.
    public sealed class TitleScreenPresentation : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] RectTransform panel, content;
        [SerializeField] Button start;
        [SerializeField] TMP_Text hint;
        [SerializeField] Image glow, flash;
        [SerializeField] RectTransform[] particles;
        [SerializeField] SpriteRenderer backdrop;
        Camera view;
        PlayerInputController input;
        Vector2[] particleOrigins;
        bool titleWasVisible;
        float flashRemaining;
        Rect lastSafe;
        Vector2Int lastScreen;
        float lastCameraSize;

        void Awake()
        {
            view = Camera.main;
            input = FindFirstObjectByType<PlayerInputController>();
            particleOrigins = new Vector2[particles.Length];
            for (int i = 0; i < particles.Length; i++) particleOrigins[i] = particles[i].anchoredPosition;
            titleWasVisible = flow.Phase == AppPhase.Title;
            flash.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            bool title = flow.Phase == AppPhase.Title;
            if (titleWasVisible && !title && flow.Phase == AppPhase.Playing) flashRemaining = .2f;
            titleWasVisible = title;
            backdrop.gameObject.SetActive(title);
            if (title)
            {
                UpdateLayout();
                float time = Time.unscaledTime;
                var color = glow.color;
                color.a = .055f + .025f * Mathf.Sin(time * (2f * Mathf.PI / 2.8f));
                glow.color = color;
                for (int i = 0; i < particles.Length; i++)
                    particles[i].anchoredPosition = particleOrigins[i] + new Vector2(Mathf.Sin(time * .35f + i * 2f) * 9f, Mathf.Sin(time * .48f + i * 1.7f) * 16f);
            }
            bool flashing = flashRemaining > 0f;
            flash.gameObject.SetActive(flashing);
            if (flashing)
            {
                flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
                var color = flash.color;
                color.a = .09f * (flashRemaining / .2f);
                flash.color = color;
            }
        }

        void UpdateLayout()
        {
            if (!view) return;
            var safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == lastSafe && screen == lastScreen && Mathf.Approximately(view.orthographicSize, lastCameraSize)) return;
            lastSafe = safe; lastScreen = screen; lastCameraSize = view.orthographicSize;
            // BoardCamera already fits the stage into the safe area. Intersect rather than
            // applying the same inset twice, and keep key art outside the interactive container.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, safe.min, view, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, safe.max, view, out var max);
            var bounds = panel.rect;
            content.offsetMin = new Vector2(Mathf.Max(0, min.x - bounds.xMin), Mathf.Max(0, min.y - bounds.yMin));
            content.offsetMax = new Vector2(Mathf.Min(0, max.x - bounds.xMax), Mathf.Min(0, max.y - bounds.yMax));
            bool touch = Application.isMobilePlatform || (input && input.UsesTouch);
            hint.text = touch ? "拖动瞄准 · 松手发射 · 点击召回" : "移动鼠标瞄准 · 左键发射 · 空格召回 · Esc 暂停";
            // A negative raycast padding extends the target without enlarging the artwork.
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            float targetPixels = 56f * dpi / 160f;
            float unitsPerPixel = 2f * view.orthographicSize / Mathf.Max(1, Screen.height) / panel.lossyScale.y;
            float extra = Mathf.Max(0f, (targetPixels * unitsPerPixel - start.GetComponent<RectTransform>().rect.height) * .5f);
            start.targetGraphic.raycastPadding = new Vector4(-extra, -extra, -extra, -extra);
            backdrop.transform.position = new Vector3(view.transform.position.x, view.transform.position.y, 2f);
            var spriteSize = backdrop.sprite.bounds.size;
            backdrop.transform.localScale = new Vector3(2f * view.orthographicSize * view.aspect / spriteSize.x, 2f * view.orthographicSize / spriteSize.y, 1f);
        }

        void OnDisable()
        {
            if (backdrop) backdrop.gameObject.SetActive(false);
            if (flash) flash.gameObject.SetActive(false);
        }
    }
}
