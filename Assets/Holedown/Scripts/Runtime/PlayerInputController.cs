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
        bool blockFire, blockRecall, blockPause, activationBlocked;
        int activationFrame = -1;
        public bool AcceptsCommands => flow.HasFocus && !activationBlocked && Time.frameCount > activationFrame;
        public void ResetGestures()
        {
            blockFire |= Mouse.current?.leftButton.isPressed == true;
            blockRecall |= Keyboard.current?.spaceKey.isPressed == true;
            blockPause |= Keyboard.current?.escapeKey.isPressed == true;
        }
        public void FocusChanged(bool focused)
        {
            activationBlocked = focused; activationFrame = Time.frameCount;
            ResetGestures();
        }
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
        void OnEnable()
        {
            flow.Changed += SyncMaps;
            pause.performed += OnPause;
            SyncMaps(); ResetGestures();
        }
        void OnPause(InputAction.CallbackContext context)
        {
            if(AcceptsCommands && !blockPause) flow.RequestPause();
        }
        void SyncMaps()
        {
            bool playing = flow.CanSimulate;
            var phase = flow.Session?.Phase;
            Enable(aimMap, playing && phase == SessionPhase.Aiming);
            Enable(volleyMap, playing && phase == SessionPhase.Volley);
            Enable(systemMap, playing && (phase == SessionPhase.Aiming || phase == SessionPhase.Volley || phase == SessionPhase.Recalling));
        }
        void Enable(InputActionMap map, bool enabled)
        {
            if (map.enabled == enabled) return;
            if (enabled)
            {
                // A press already held while the map was off (for example the click that
                // pressed "开始游戏") must not read as a fresh fire, recall or pause.
                ResetGestures();
                map.Enable();
            }
            else map.Disable();
        }
        void Update()
        {
            bool left = Mouse.current?.leftButton.isPressed == true;
            bool space = Keyboard.current?.spaceKey.isPressed == true;
            bool escape = Keyboard.current?.escapeKey.isPressed == true;
            if(!left) blockFire = false;
            if(!space) blockRecall = false;
            if(!escape) blockPause = false;
            if(activationBlocked && !left)
            { activationBlocked = false; activationFrame = Time.frameCount; }
            SyncMaps();
            if(!AcceptsCommands) { aim.HidePreview(); return; }
            if (recall.WasPressedThisFrame() && !blockRecall) flow.QueueCommand(FlowCommand.Recall);
            if (aimMap.enabled) aim.SamplePointer(point.ReadValue<Vector2>(), fire.WasPressedThisFrame() && !blockFire);
            else aim.HidePreview();
        }
        void DisableMaps() { aimMap?.Disable(); volleyMap?.Disable(); systemMap?.Disable(); }
        void OnDisable()
        {
            flow.Changed -= SyncMaps;
            if(pause != null) pause.performed -= OnPause;
            DisableMaps(); if (aim) aim.HidePreview();
        }
    }
}
