#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Holedown.Tests
{
    // The click that presses "开始游戏" must not become the first volley, and the
    // same rule applies when a map is re-enabled while a key is already held.
    public sealed class StartClickInputTests
    {
        GrowthSeedScope seed;
        [SetUp] public void Setup() => seed = new GrowthSeedScope();
        [TearDown] public void Cleanup() => seed.Dispose();
        [UnityTest] public IEnumerator HeldStartPressDoesNotLaunchTheFirstVolley()
        {
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            InputActionAsset actions = null;
            UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputDevice>? previousDevices = null;
            try
            {
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
                actions = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().actionsAsset;
                var previous = actions.devices; actions.devices = new InputDevice[] { mouse, keyboard };
                previousDevices = previous;
                mouse.MakeCurrent(); keyboard.MakeCurrent();
                var flow = Object.FindFirstObjectByType<GameFlowController>();
                flow.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
                Assert.AreEqual(AppPhase.Title, flow.Phase);
                // Aim below the board so nothing but the click itself could fire.
                var aim = (Vector2)Camera.main.WorldToScreenPoint(new Vector3(4, 2.5f, 0));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = aim }.WithButton(MouseButton.Left));
                yield return null;
                // The title button's click is what starts the run; the press is still held.
                flow.StartGame(); yield return null; yield return null; yield return null;
                Assert.AreEqual(AppPhase.Playing, flow.Phase, $"focus={flow.HasFocus}");
                Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase);
                Assert.AreEqual(0, flow.Session.ActiveCount, "a held start click must not fire");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = aim });
                yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = aim }.WithButton(MouseButton.Left));
                yield return null; yield return null;
                Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase, "a fresh press still fires");
                flow.RequestRecall(); yield return null; yield return null;
                yield return null;
            }
            finally
            {
                if (actions != null) { actions.devices = previousDevices; }
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            }
        }
    }
}
#endif
