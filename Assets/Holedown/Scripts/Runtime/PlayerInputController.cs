using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Holedown
{
    // Owns only the Holedown maps. The UI module owns the UI map.
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;
        [SerializeField] GameFlowController flow;
        [SerializeField] AimInput aim;
        InputActionMap aimMap, volleyMap, systemMap;
        InputAction point, fire, recall, pause;
        public event Action PauseRequested;
        void Awake()
        {
            aimMap = actions.FindActionMap("HoledownAim", true);
            volleyMap = actions.FindActionMap("HoledownVolley", true);
            systemMap = actions.FindActionMap("HoledownSystem", true);
            point = aimMap.FindAction("Point", true);
            fire = aimMap.FindAction("Fire", true);
            recall = volleyMap.FindAction("Recall", true);
            pause = systemMap.FindAction("Pause", true);
            DisableMaps();
        }
        static void Enable(InputActionMap map, bool enabled)
        {
            if (map.enabled == enabled) return;
            if (enabled) map.Enable(); else map.Disable();
        }
        void Update()
        {
            bool focused = Application.isFocused;
            var phase = flow.Session?.Phase;
            Enable(aimMap, focused && phase == SessionPhase.Aiming);
            Enable(volleyMap, focused && phase == SessionPhase.Volley);
            Enable(systemMap, focused && (phase == SessionPhase.Aiming || phase == SessionPhase.Volley || phase == SessionPhase.Recalling));
            if (pause.WasPressedThisFrame()) { PauseRequested?.Invoke(); aim.HidePreview(); return; }
            if (recall.WasPressedThisFrame()) flow.RequestRecall();
            if (aimMap.enabled) aim.SamplePointer(point.ReadValue<Vector2>(), fire.WasPressedThisFrame());
            else aim.HidePreview();
        }
        void DisableMaps() { aimMap?.Disable(); volleyMap?.Disable(); systemMap?.Disable(); }
        void OnDisable() { DisableMaps(); if (aim) aim.HidePreview(); }
    }
}
