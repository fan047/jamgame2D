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
            view.orthographicSize = Mathf.Max(size.y * 0.5f, size.x * 0.5f / view.aspect);
            transform.position = new Vector3(StageCenter.x, StageCenter.y, -10);
        }
    }
}
