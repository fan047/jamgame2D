using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class PlayerDeviceTests
    {
        [UnityTest]
        public IEnumerator RealPressHoldRecallAndPauseRespectFlowGates()
        {
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            InputActionAsset sceneActions = null;
            UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? previousDevices = null;
            try
            {
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
                sceneActions = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().actionsAsset;
                previousDevices = sceneActions.devices; sceneActions.devices = new InputDevice[] { mouse, keyboard };
                mouse.MakeCurrent(); keyboard.MakeCurrent();
                yield return PlaySceneEntry.StartIfReady();
                var flow = Object.FindFirstObjectByType<GameFlowController>();
                var position = (Vector2)Camera.main.WorldToScreenPoint(new Vector3(4,4,0));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
                yield return null; yield return null;
                Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null; yield return null; yield return null;
                Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase);
                int round = flow.Session.Round;
                yield return null; Assert.AreEqual(round, flow.Session.Round, "Held mouse must not fire next round");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                // Escape and a new Fire in the same update must pause without starting a volley.
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null; yield return null;
                Assert.AreEqual(AppPhase.Paused, flow.Phase); Assert.AreEqual(round, flow.Session.Round);
                Assert.AreEqual(0, flow.Session.ActiveCount);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                flow.ContinueGame(); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
                yield return null; yield return null;
                Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase);
            }
            finally
            {
                var flow = Object.FindFirstObjectByType<GameFlowController>(); if(flow) flow.enabled = false;
                if(sceneActions) sceneActions.devices = previousDevices;
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = oldBackground; InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            }
        }
        [UnityTest]
        public IEnumerator NamedActionsObserveDevicePressHoldRelease()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            yield return null;
            var input = Object.FindFirstObjectByType<PlayerInputController>();
            input.enabled = false;
            var module = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var asset = Object.Instantiate(module.actionsAsset);
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                asset.devices = new InputDevice[] { mouse, keyboard };
                var fire = asset.FindAction("HoledownAim/Fire", true);
                var recall = asset.FindAction("HoledownVolley/Recall", true);
                var pause = asset.FindAction("HoledownSystem/Pause", true);
                int fires=0, recalls=0, pauses=0;
                fire.performed += _ => fires++; recall.performed += _ => recalls++; pause.performed += _ => pauses++;
                fire.Enable(); recall.Enable(); pause.Enable(); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space, Key.Escape));
                yield return null;
                Assert.AreEqual(1, fires); Assert.AreEqual(1, recalls); Assert.AreEqual(1, pauses);
                yield return null;
                Assert.AreEqual(1, fires); Assert.AreEqual(1, recalls); Assert.AreEqual(1, pauses);
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); yield return null;
                Assert.AreEqual(2, fires);
                asset.Disable(); Assert.IsFalse(fire.enabled); Assert.IsFalse(recall.enabled); Assert.IsFalse(pause.enabled);
            }
            finally { Object.Destroy(asset); InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = oldBackground; InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor; }
        }
    }
}
