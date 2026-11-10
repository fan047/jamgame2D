using UnityEngine;

namespace Holedown
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour
    {
        [SerializeField] VisualTheme theme;
        public static readonly Vector2 StageCenter = new Vector2(4, 4.8f);
        Camera view;
        void Awake() { view = GetComponent<Camera>(); Synchronize(); }
        void Update() => Synchronize();
        void LateUpdate() => Synchronize();
        public void ConfigureTheme(VisualTheme value) { theme = value; Synchronize(); }
        public void Synchronize()
        {
            if(!view) view = GetComponent<Camera>();
            view.orthographic = true;
            var size = theme && theme.fixedStage ? theme.stageSize : new Vector2(9.4f,12.4f);
            // The usable viewport is the safe area: notch, rounded corners and the gesture bar
            // stay outside it, and the stage plus the world-space UI are fitted inside it.
            float screenWidth = Mathf.Max(1f, Screen.width), screenHeight = Mathf.Max(1f, Screen.height);
            var safe = Screen.safeArea;
            float usableX = Mathf.Clamp01(safe.width / screenWidth), usableY = Mathf.Clamp01(safe.height / screenHeight);
            if (usableX <= 0f || usableY <= 0f) { usableX = usableY = 1f; }
            view.orthographicSize = Mathf.Max(size.y * 0.5f / usableY, size.x * 0.5f / (view.aspect * usableX));
            var center = new Vector3(StageCenter.x, StageCenter.y, -10);
            if (usableX < 0.999f || usableY < 0.999f)
            {
                // Move the view so the stage sits in the middle of the usable area; pointer
                // conversion follows the same camera, so aiming stays under the finger.
                float halfHeight = view.orthographicSize;
                float worldPerPixelX = 2f * halfHeight * view.aspect / screenWidth;
                float worldPerPixelY = 2f * halfHeight / screenHeight;
                center.x -= (safe.center.x - screenWidth * 0.5f) * worldPerPixelX;
                center.y -= (safe.center.y - screenHeight * 0.5f) * worldPerPixelY;
            }
            transform.position = center;
        }
    }
}
