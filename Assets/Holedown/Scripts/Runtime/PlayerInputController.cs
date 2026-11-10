using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Holedown
{
    // Owns only the Holedown maps. The UI module owns the UI map.
    public sealed class PlayerInputController : MonoBehaviour
    {
        public enum InputMode { Automatic, Mouse, Touch }
        [SerializeField] InputActionAsset actions;
        [SerializeField] GameFlowController flow;
        [SerializeField] AimInput aim;
        [SerializeField, Tooltip("Automatic uses touch on mobile and the mouse everywhere else.")]
        InputMode inputMode = InputMode.Automatic;
        InputActionMap aimMap, volleyMap, systemMap;
        InputAction point, fire, recall, pause;
        bool blockFire, blockRecall, blockPause, activationBlocked;
        int activationFrame = -1;
        bool touching, touchValid, awaitingTouchRelease;
        int touchId = -1;
        public bool UsesTouch => inputMode == InputMode.Touch || (inputMode == InputMode.Automatic && Application.isMobilePlatform);
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
            CancelTouch();
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
        // A touch gesture never survives a pause, a phase change, a second finger or losing the
        // board; the player has to press again before anything can fire.
        void CancelTouch(bool waitForRelease = false)
        {
            touching = false; touchValid = false; touchId = -1; awaitingTouchRelease = waitForRelease;
            if (aim) aim.HidePreview();
        }
        void UpdateTouch()
        {
            var touchscreen = Touchscreen.current;
            // No touch device at all (Editor, desktop): nothing to read, and a test driving
            // ProcessTouch directly must not be cancelled by the device layer.
            if (touchscreen == null) return;
            int pressed = 0;
            foreach (var touch in touchscreen.touches)
                if (touch.press.isPressed) pressed++;
            var primary = touchscreen.primaryTouch;
            var reported = primary.phase.ReadValue();
            if (pressed == 0 && reported == UnityEngine.InputSystem.TouchPhase.None && touching) { CancelTouch(); return; }
            var phase = reported == UnityEngine.InputSystem.TouchPhase.Canceled ? UnityEngine.InputSystem.TouchPhase.Canceled
                : reported == UnityEngine.InputSystem.TouchPhase.Ended ? UnityEngine.InputSystem.TouchPhase.Ended
                : primary.press.wasPressedThisFrame ? UnityEngine.InputSystem.TouchPhase.Began
                : UnityEngine.InputSystem.TouchPhase.Moved;
            ProcessTouch(phase, primary.position.ReadValue(), pressed, primary.touchId.ReadValue());
        }
        // One gesture. Only a real Ended phase on the finger that started inside the board fires;
        // Canceled, a different finger, a second finger or leaving the board ends it without a
        // shot, and sliding back in never resumes it.
        public void ProcessTouch(UnityEngine.InputSystem.TouchPhase phase, Vector2 position, int pressedTouches = 1, int id = 1)
        {
            if (awaitingTouchRelease)
            {
                if (pressedTouches <= 0) awaitingTouchRelease = false;
                return;
            }
            if (!aimMap.enabled || !AcceptsCommands) { CancelTouch(); return; }
            if (touching)
            {
                if (pressedTouches > 1 || id != touchId) { CancelTouch(waitForRelease: true); return; }
                if (phase == UnityEngine.InputSystem.TouchPhase.Canceled) { CancelTouch(waitForRelease: true); return; }
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    bool fire = touchValid && aim.TryPointerDirection(position, out _);
                    CancelTouch();
                    if (fire) aim.SamplePointer(position, true);
                    return;
                }
                if (!touchValid) return;
                if (!aim.TryPointerDirection(position, out _)) { touchValid = false; aim.HidePreview(); return; }
                aim.SamplePointer(position, false);
                return;
            }
            if (phase != UnityEngine.InputSystem.TouchPhase.Began) return;
            // A gesture is exclusive: a press that already has two fingers never starts one.
            if (pressedTouches > 1) { CancelTouch(waitForRelease: true); return; }
            if (!aim.TryPointerDirection(position, out _)) return;
            touching = true; touchValid = true; touchId = id;
            aim.SamplePointer(position, false);
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
            if(!AcceptsCommands) { CancelTouch(); aim.HidePreview(); return; }
            if (recall.WasPressedThisFrame() && !blockRecall) flow.QueueCommand(FlowCommand.Recall);
            if (UsesTouch) { UpdateTouch(); return; }
            if (aimMap.enabled) aim.SamplePointer(point.ReadValue<Vector2>(), fire.WasPressedThisFrame() && !blockFire);
            else aim.HidePreview();
        }
        void DisableMaps() { aimMap?.Disable(); volleyMap?.Disable(); systemMap?.Disable(); }
        void OnDisable()
        {
            flow.Changed -= SyncMaps;
            if(pause != null) pause.performed -= OnPause;
            DisableMaps(); CancelTouch(); if (aim) aim.HidePreview();
        }
    }
}
