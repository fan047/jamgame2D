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
