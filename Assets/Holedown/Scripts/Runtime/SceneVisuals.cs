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
        void Start()
        {
            if(!theme) return;
            background.sprite = theme.background; background.enabled = theme.background;
            if(theme.launcher) { launcher.sprite = theme.launcher; launcher.color = Color.white; SpriteSizing.Fit(launcher, Vector2.one * 0.3f); }
            if(startScreen && theme.titleLogo)
            {
                startScreen.Logo.sprite = theme.titleLogo; startScreen.Logo.enabled = true;
                var title = startScreen.Logo.transform.parent.Find("Title"); if(title) title.gameObject.SetActive(false);
            }
        }
        void LateUpdate()
        {
            if(!background || !background.sprite) return;
            var position = gameCamera.transform.position; position.z = 1;
            background.transform.position = position;
            SpriteSizing.Cover(background, new Vector2(gameCamera.orthographicSize * 2 * gameCamera.aspect, gameCamera.orthographicSize * 2));
        }
    }
}
