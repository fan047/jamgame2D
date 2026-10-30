using UnityEngine;

namespace Holedown
{
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour
    {
        Camera view;
        void Awake() { view = GetComponent<Camera>(); }
        void LateUpdate()
        {
            view.orthographic = true;
            view.orthographicSize = Mathf.Max(6.2f, 4.7f / view.aspect);
            transform.position = new Vector3(4, 4.8f, -10);
        }
    }
}
