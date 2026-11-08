using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Holedown
{
    // Presses shrink slightly and release restores, matching the placeholder UI motion plan.
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.8f, 1f)] float pressedScale = 0.96f;
        Vector3 rest;
        bool captured;
        void Awake() { rest = transform.localScale; captured = true; }
        public void OnPointerDown(PointerEventData eventData) => transform.localScale = rest * pressedScale;
        public void OnPointerUp(PointerEventData eventData) => transform.localScale = rest;
        public void OnPointerExit(PointerEventData eventData) => transform.localScale = rest;
        void OnDisable() { if (captured) transform.localScale = rest; }
    }
}
