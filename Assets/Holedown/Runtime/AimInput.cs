using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Holedown
{
    public sealed class AimInput : MonoBehaviour
    {
        [SerializeField] Camera gameCamera;
        [SerializeField] GameSettings settings;
        [SerializeField] LineRenderer preview;
        [SerializeField] LayerMask collisionMask;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData pointer;
        EventSystem pointerSystem;
        public static readonly Vector2 Origin = new Vector2(4, 9);
        public bool CanAim { get; set; }
        public event Action<Vector2> FireRequested;
        public bool IsPointerOverUI(Vector2 screen)
        {
            if (!EventSystem.current) return false;
            if (pointer == null || pointerSystem != EventSystem.current)
            { pointerSystem = EventSystem.current; pointer = new PointerEventData(pointerSystem); }
            pointer.position = screen; uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, uiHits);
            return uiHits.Count > 0;
        }
        public static bool TryDirection(Vector2 position, float angleLimit, out Vector2 direction)
        {
            direction = position - Origin;
            if (position.x < 0 || position.x > 8 || position.y < 0 || position.y >= Origin.y || direction.sqrMagnitude < 0.0001f) return false;
            float angle = Mathf.Clamp(Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg, -angleLimit, angleLimit) * Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            return true;
        }
        public Vector2 PreviewEnd(Vector2 direction)
        {
            var hit = Physics2D.CircleCast(Origin, settings.ballRadius, direction, 30, collisionMask);
            return hit.collider ? hit.centroid : Origin + direction * 30;
        }
        void Update()
        {
            preview.enabled = false;
            if (!CanAim || Mouse.current == null) return;
            Vector2 screen = Mouse.current.position.ReadValue();
            if (!gameCamera.pixelRect.Contains(screen) || IsPointerOverUI(screen)) return;
            var world = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -gameCamera.transform.position.z));
            if (!TryDirection(world, settings.maxAimAngle, out var direction)) return;
            preview.enabled = true;
            preview.SetPosition(0, Origin); preview.SetPosition(1, PreviewEnd(direction));
            if (Mouse.current.leftButton.wasPressedThisFrame) FireRequested?.Invoke(direction);
        }
        void OnDisable() { if (preview) preview.enabled = false; }
    }
}
