using UnityEngine;

namespace Holedown
{
    [DefaultExecutionOrder(100)]
    public sealed class SceneVisuals : MonoBehaviour
    {
        [SerializeField] VisualTheme theme;
        [SerializeField] Camera gameCamera;
        [SerializeField] SpriteRenderer background;
        [SerializeField] SpriteRenderer foreground;
        [SerializeField] SpriteRenderer launcher;
        [SerializeField] StartScreenView startScreen;
        [SerializeField] BoardView boardView;        [SerializeField] RectTransform stageCanvas;
        [SerializeField, Tooltip("Launcher pulse on the first ball of a volley; 0 disables it.")]
        [Min(0f)] float launchPulseSeconds = 0.12f;
        [SerializeField, Range(1f, 1.5f)] float launchPulseScale = 1.12f;
        GameFlowController flow;
        Vector3 launcherBaseScale = Vector3.one;
        Coroutine pulse;
        void RefreshBackground()
        {
            if (!theme || !background) return;
            int stage = flow && flow.Campaign != null ? flow.Campaign.StageIndex : -1;
            // Background and foreground are one decision: a group's in-place decorations
            // only ever belong to that group's cave.
            var selected = theme.Background(stage);
            if (background.sprite != selected) background.sprite = selected;
            background.enabled = selected;
            if (foreground)
            {
                var layer = theme.Foreground(stage);
                if (foreground.sprite != layer) foreground.sprite = layer;
                foreground.enabled = layer;
            }
        }
        void OnDestroy() { if (flow) { flow.Changed -= RefreshBackground; flow.VolleyLaunched -= PlayLaunchPulse; } }
        void PlayLaunchPulse()
        {
            if (!launcher || launchPulseSeconds <= 0f || !isActiveAndEnabled) return;
            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(Pulse());
        }
        System.Collections.IEnumerator Pulse()
        {
            float elapsed = 0; launcher.transform.localScale = launcherBaseScale;
            while (elapsed < launchPulseSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / launchPulseSeconds);
                // Out and back: peak at the midpoint of the short pulse.
                float factor = t < 0.5f ? Mathf.Lerp(1f, launchPulseScale, t * 2f) : Mathf.Lerp(launchPulseScale, 1f, (t - 0.5f) * 2f);
                launcher.transform.localScale = launcherBaseScale * factor;
                yield return null;
            }
            launcher.transform.localScale = launcherBaseScale; pulse = null;
        }
        void Start()
        {
            if(!theme) return;
            flow = GetComponent<GameFlowController>();
            if (flow) { flow.Changed += RefreshBackground; flow.VolleyLaunched += PlayLaunchPulse; }
            var cameraRig = gameCamera.GetComponent<BoardCamera>();
            if(cameraRig) cameraRig.ConfigureTheme(theme);
            if(theme.fixedStage && stageCanvas)
            {
                stageCanvas.sizeDelta = new Vector2(theme.stageSize.x / stageCanvas.lossyScale.x, theme.stageSize.y / stageCanvas.lossyScale.y);
                var position = stageCanvas.position;
                stageCanvas.position = new Vector3(BoardCamera.StageCenter.x,BoardCamera.StageCenter.y,position.z);
            }
            RefreshBackground();
            if(foreground) { foreground.color = Color.white; }
            // A failed configuration load leaves Campaign null; the error page must survive.
            if(boardView) boardView.ConfigureStage(flow && flow.Campaign != null ? flow.Campaign.StageIndex : -1);
            if(boardView && boardView.Preview) boardView.Preview.Configure(theme);
            AlignForeground();
            if(theme.launcher) { launcher.sprite = theme.launcher; launcher.color = Color.white; SpriteSizing.Fit(launcher, Vector2.one * 0.3f); launcherBaseScale = launcher.transform.localScale; }
            if(startScreen && theme.titleLogo)
            {
                startScreen.Logo.sprite = theme.titleLogo; startScreen.Logo.enabled = true;
                var title = startScreen.Logo.transform.parent.Find("Title"); if(title) title.gameObject.SetActive(!theme.logoContainsTitle);
            }
        }
        void LateUpdate()
        {
            if(!background || !background.sprite) return;
            var position = theme && theme.fixedStage ? new Vector3(BoardCamera.StageCenter.x,BoardCamera.StageCenter.y,1) : gameCamera.transform.position; position.z = 1;
            background.transform.position = position;
            if(theme && theme.fixedStage)
            {
                var native = background.sprite.bounds.size;
                float scale = Mathf.Min(theme.stageSize.x/native.x, theme.stageSize.y/native.y);
                background.transform.localScale = Vector3.one * scale;
            }
            else SpriteSizing.Cover(background, new Vector2(gameCamera.orthographicSize * 2 * gameCamera.aspect, gameCamera.orthographicSize * 2));
            AlignForeground();
        }
        // The foreground shares the background's exact stage rect, so every extracted
        // rock and crystal pixel stays aligned with the art it came from.
        void AlignForeground()
        {
            if(!foreground || !background) return;
            foreground.transform.position = background.transform.position;
            foreground.transform.localScale = background.transform.localScale;
            foreground.transform.rotation = background.transform.rotation;
        }
    }
}
