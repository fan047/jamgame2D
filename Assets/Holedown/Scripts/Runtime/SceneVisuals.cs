using UnityEngine;

namespace Holedown
{
    [DefaultExecutionOrder(100)]
    public sealed class SceneVisuals : MonoBehaviour
    {
        [SerializeField] VisualTheme theme;
        [SerializeField] Camera gameCamera;
        [SerializeField] SpriteRenderer background;
        [SerializeField] SpriteRenderer launcher;
        [SerializeField] StartScreenView startScreen;
        [SerializeField] RectTransform stageCanvas;
        GameFlowController flow;
        void RefreshBackground()
        {
            if (!theme || !background) return;
            var selected = theme.Background(flow && flow.Campaign != null ? flow.Campaign.StageIndex : -1);
            if (background.sprite != selected) background.sprite = selected;
            background.enabled = selected;
        }
        void OnDestroy() { if (flow) flow.Changed -= RefreshBackground; }
        void Start()
        {
            if(!theme) return;
            flow = GetComponent<GameFlowController>();
            if (flow) flow.Changed += RefreshBackground;
            var cameraRig = gameCamera.GetComponent<BoardCamera>();
            if(cameraRig) cameraRig.ConfigureTheme(theme);
            if(theme.fixedStage && stageCanvas)
            {
                stageCanvas.sizeDelta = new Vector2(theme.stageSize.x / stageCanvas.lossyScale.x, theme.stageSize.y / stageCanvas.lossyScale.y);
                var position = stageCanvas.position;
                stageCanvas.position = new Vector3(BoardCamera.StageCenter.x,BoardCamera.StageCenter.y,position.z);
            }
            RefreshBackground();
            if(theme.launcher) { launcher.sprite = theme.launcher; launcher.color = Color.white; SpriteSizing.Fit(launcher, Vector2.one * 0.3f); }
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
        }
    }
}
