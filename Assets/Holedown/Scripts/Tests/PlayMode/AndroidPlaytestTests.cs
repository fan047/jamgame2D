#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    // Android playtest acceptance: the packaged configuration loads from StreamingAssets through
    // the strict parser, a broken or short bundle shows the configuration error instead of a
    // single stage, touch aiming fires once per valid gesture, and backgrounding or focus loss
    // always ends in a paused run that only an explicit continue resumes.
    public sealed class AndroidPlaytestTests
    {
        const UnityEngine.InputSystem.TouchPhase Began = UnityEngine.InputSystem.TouchPhase.Began;
        const UnityEngine.InputSystem.TouchPhase Moved = UnityEngine.InputSystem.TouchPhase.Moved;
        const UnityEngine.InputSystem.TouchPhase Ended = UnityEngine.InputSystem.TouchPhase.Ended;
        const UnityEngine.InputSystem.TouchPhase Canceled = UnityEngine.InputSystem.TouchPhase.Canceled;
        string directory;
        GameFlowController flow;
        PlayerInputController input;
        AimInput aim;

        [SetUp] public void Setup()
        {
            directory = Path.GetFullPath("Library/Locus/tmp/android-tests/" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            flow = null; input = null; aim = null;
            SceneManager.sceneLoaded += Configure;
        }
        [TearDown] public void Cleanup()
        {
            SceneManager.sceneLoaded -= Configure;
            BundledConfiguration.RootOverrideForTests = null;
            if (flow) flow.enabled = false;
            if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 0 });
            Time.timeScale = 1f;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        void Configure(Scene scene, LoadSceneMode mode)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            if (!controller) return;
            var serialized = new UnityEditor.SerializedObject(controller);
            serialized.FindProperty("configSource").enumValueIndex = (int)ConfigSource.BundledStreamingAssets;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        // Writes the packaged bundle the phone would carry: the project's own settings plus a
        // level file that is valid, broken, or deliberately short.
        void PrepareBundle(bool brokenLevel = false, int stages = 9)
        {
            File.Copy("Config/settings.json", Path.Combine(directory, "settings.json"), true);
            string levelPath = Path.Combine(directory, "level.json");
            if (brokenLevel) { File.WriteAllText(levelPath, "{\"schemaVersion\":2,\"version\":\"broken\","); }
            else if (stages == 9) { File.Copy("Config/level.json", levelPath, true); }
            else
            {
                var root = JObject.Parse(File.ReadAllText("Config/level.json"));
                var levels = (JArray)root["levels"];
                while (levels.Count > stages) levels.RemoveAt(levels.Count - 1);
                // The last stage carries the single target and no rewards, so the file itself is
                // valid and only the nine-stage requirement can reject it.
                levels[levels.Count - 1] = JObject.Parse(
                    "{\"id\":\"stage-short\",\"name\":\"Short\",\"initialBlocks\":[]," +
                    "\"incomingRows\":[{\"blocks\":[{\"column\":3,\"row\":0,\"width\":1,\"height\":1,\"health\":1,\"target\":true,\"rewardBalls\":0}]}]}");
                File.WriteAllText(levelPath, root.ToString());
            }
            BundledConfiguration.RootOverrideForTests = directory;
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            flow.SendMessage("OnApplicationFocus", true);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (flow.Phase == AppPhase.Loading && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
        }
        IEnumerator StartTouching()
        {
            PrepareBundle();
            yield return Load();
            Assert.AreEqual(AppPhase.Title, flow.Phase, "the bundled bundle must load first");
            // The activation frame after a focus change must pass before the UI takes commands.
            float ready = Time.realtimeSinceStartup + 3f;
            while (!flow.UserInputAllowed && Time.realtimeSinceStartup < ready) yield return null;
            if (flow.Phase == AppPhase.Title) { flow.StartGame(); yield return null; yield return null; }
            if (flow.Phase != AppPhase.Playing) { flow.StartGame(); yield return null; yield return null; }
            Assert.AreEqual(AppPhase.Playing, flow.Phase, $"start failed: focus={flow.HasFocus} input={flow.UserInputAllowed} scale={Time.timeScale}");
            input = UnityEngine.Object.FindFirstObjectByType<PlayerInputController>();
            aim = UnityEngine.Object.FindFirstObjectByType<AimInput>();
            var serialized = new UnityEditor.SerializedObject(input);
            serialized.FindProperty("inputMode").enumValueIndex = (int)PlayerInputController.InputMode.Touch;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(input.UsesTouch, "the touch path is active for this test");
            Assert.AreEqual(AppPhase.Playing, flow.Phase);
        }
        static Vector2 BoardPoint() => Camera.main.WorldToScreenPoint(new Vector3(4f, 5f, 0f));
        static Vector2 MarginPoint() => Camera.main.WorldToScreenPoint(new Vector3(-0.6f, 5f, 0f));
        static Vector2 UiPoint() => Camera.main.WorldToScreenPoint(((RectTransform)GameObject.Find("UI/Pause").transform).position);
        IEnumerator Gesture(Vector2 from, Vector2 to, int id = 1, UnityEngine.InputSystem.TouchPhase release = Ended)
        {
            input.ProcessTouch(Began, from, 1, id);
            yield return null;
            if (to != from) { input.ProcessTouch(Moved, to, 1, id); yield return null; }
            input.ProcessTouch(release, to, release == Ended ? 0 : 1, id);
            yield return null; yield return null;
        }
        [UnityTest] public IEnumerator BundledSourceLoadsThePackagedNineStageCampaign()
        {
            PrepareBundle();
            yield return Load();
            Assert.AreEqual(AppPhase.Title, flow.Phase, "the bundled config decides the phase");
            Assert.IsNull(flow.InitializationError);
            Assert.IsNull(flow.ConfigurationDirectory, "a packaged build has no external directory");
            Assert.IsTrue(flow.UsesBundledConfiguration);
            Assert.AreEqual(GameFlowController.BundledStageCount, flow.Campaign.StageCount);
            Assert.AreEqual("Bundled", flow.ConfigurationLabel);
            flow.StartGame(); yield return null; yield return null;
            Assert.AreEqual(AppPhase.Playing, flow.Phase);
            Assert.IsTrue(flow.CanSimulate);
        }
        [UnityTest] public IEnumerator BrokenBundleShowsTheConfigurationErrorAndNoSingleStage()
        {
            PrepareBundle(brokenLevel: true);
            LogAssert.Expect(LogType.Error, new Regex("Initialization failed"));
            yield return Load();
            Assert.AreEqual(AppPhase.ConfigError, flow.Phase);
            Assert.IsNotEmpty(flow.InitializationError);
            Assert.IsNull(flow.Campaign, "a broken bundle must not fall back to a built-in stage");
            Assert.IsNull(flow.Board);
            Assert.IsFalse(flow.TryFire(Vector2.down));
        }
        [UnityTest] public IEnumerator ShortBundleIsRejectedBecauseNineStagesAreRequired()
        {
            PrepareBundle(stages: 4);
            LogAssert.Expect(LogType.Error, new Regex("expected 9 stages"));
            yield return Load();
            Assert.AreEqual(AppPhase.ConfigError, flow.Phase);
            StringAssert.Contains("9", flow.InitializationError);
            Assert.IsNull(flow.Campaign);
        }
        [UnityTest] public IEnumerator BrokenBundleCanBeRereadAfterTheErrorPage()
        {
            PrepareBundle(brokenLevel: true);
            LogAssert.Expect(LogType.Error, new Regex("Initialization failed"));
            yield return Load();
            Assert.AreEqual(AppPhase.ConfigError, flow.Phase);
            PrepareBundle();
            flow.ReloadAndRestart();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (flow.Phase == AppPhase.Loading && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(AppPhase.Title, flow.Phase, "重新读取 recovers from the error page");
            Assert.IsNull(flow.InitializationError);
            Assert.AreEqual(GameFlowController.BundledStageCount, flow.Campaign.StageCount);
        }
        [UnityTest] public IEnumerator AValidGestureFiresExactlyOneVolley()
        {
            yield return StartTouching();
            yield return Gesture(BoardPoint(), BoardPoint(), 11);
            Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase, "releasing inside the board starts one volley");
            Assert.AreEqual(1, flow.Session.VolleyId, "one gesture is one volley");
            flow.RequestRecall(); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator GesturesThatStartOnUiOrMarginNeverFire()
        {
            yield return StartTouching();
            yield return Gesture(UiPoint(), UiPoint(), 12);
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a gesture on the UI never fires");
            yield return Gesture(MarginPoint(), MarginPoint(), 13);
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a gesture on the decorative margin never fires");
        }
        [UnityTest] public IEnumerator CanceledOrForeignFingersNeverFire()
        {
            yield return StartTouching();
            yield return Gesture(BoardPoint(), BoardPoint(), 21, Canceled);
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a canceled touch never fires");
            input.ProcessTouch(Began, BoardPoint(), 1, 22); yield return null;
            input.ProcessTouch(Ended, BoardPoint(), 0, 23); yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a different finger cannot finish the gesture");
            Assert.IsFalse(aim.PreviewVisible, "the aimed preview is cleared as well");
        }
        [UnityTest] public IEnumerator SlidingOutNeverResumesAndTheNextPressIsFresh()
        {
            yield return StartTouching();
            input.ProcessTouch(Began, BoardPoint(), 1, 31); yield return null;
            Assert.IsTrue(aim.PreviewVisible, $"a valid press shows the aim preview: canSim={flow.CanSimulate} session={flow.Session.Phase} canAim={aim.CanAim} accepts={input.AcceptsCommands} overUi={aim.IsPointerOverUI(BoardPoint())}");
            input.ProcessTouch(Moved, MarginPoint(), 1, 31); yield return null;
            Assert.IsFalse(aim.PreviewVisible, "leaving the board clears the preview");
            input.ProcessTouch(Moved, BoardPoint(), 1, 31); yield return null;
            Assert.IsFalse(aim.PreviewVisible, "sliding back in does not resume the preview");
            input.ProcessTouch(Ended, BoardPoint(), 0, 31); yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "an invalidated gesture cannot fire");
            yield return Gesture(BoardPoint(), BoardPoint(), 32);
            Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase, "a new press still fires normally");
            flow.RequestRecall(); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator TwoFingersCancelAndWaitForAFullRelease()
        {
            yield return StartTouching();
            input.ProcessTouch(Began, BoardPoint(), 2, 41); yield return null; yield return null;
            input.ProcessTouch(Ended, BoardPoint(), 1, 41); yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a two finger start never fires");
            input.ProcessTouch(Began, BoardPoint(), 1, 42); yield return null;
            input.ProcessTouch(Ended, BoardPoint(), 0, 42); yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "the next gesture waits for a full release");
            input.ProcessTouch(UnityEngine.InputSystem.TouchPhase.None, BoardPoint(), 0, 43); yield return null;
            yield return Gesture(BoardPoint(), BoardPoint(), 44);
            Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase, "after everything is released a fresh press fires");
            flow.RequestRecall(); yield return null; yield return null;
        }
        // Device-level path: a real (simulated) Touchscreen receives the events, so the phase and
        // touch id that a phone reports are the ones the gesture machine reads.
        IEnumerator MouseAt(Mouse mouse, Vector2 position, bool down)
        {
            InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = position, buttons = (ushort)(down ? 1 : 0) });
            yield return null; yield return null;
        }
        [UnityTest] public IEnumerator DeviceTouchFiresOneVolleyAndCanceledNeverFires()
        {
            yield return StartTouching();
            var mouse = InputSystem.AddDevice<Mouse>();
            // Mouse-to-touch mirroring only runs while the Game view owns input. In a full
            // suite the focused window varies, so pin the behaviour for this test only.
            var previousBackground = InputSystem.settings.backgroundBehavior;
            var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                TouchSimulation.Enable();
                yield return null; yield return null;
                var touchscreen = TouchSimulation.instance.simulatedTouchscreen;
                Assert.IsNotNull(touchscreen, "the simulated touch device drives the phone path");
                var start = BoardPoint();
                var end = BoardPoint() + new Vector2(10f, 0f);
                yield return MouseAt(mouse, start, down: true);
                // The simulated touch device mirrors the pointer with a frame of latency.
                float pressed = Time.realtimeSinceStartup + 1f;
                while (!touchscreen.primaryTouch.press.isPressed && Time.realtimeSinceStartup < pressed) yield return null;
                Assert.IsTrue(touchscreen.primaryTouch.press.isPressed, "the device reports a pressed touch");
                yield return MouseAt(mouse, end, down: true);
                yield return MouseAt(mouse, end, down: false);
                Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase, "Began, drag and Ended fire exactly one volley");
                Assert.AreEqual(1, flow.Session.VolleyId, "the device path never fires twice");
                flow.RequestRecall(); yield return null; yield return null;
                // Losing the pointing device mid-touch is a cancellation at the device level.
                yield return MouseAt(mouse, start, down: true);
                InputSystem.RemoveDevice(mouse);
                yield return null; yield return null;
                Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a canceled device touch never fires");
            }
            finally
            {
                TouchSimulation.Disable();
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            }
        }
        [UnityTest] public IEnumerator TouchModeIgnoresTheMousePress()
        {
            yield return StartTouching();
            InputSystem.QueueStateEvent(Mouse.current ?? InputSystem.AddDevice<Mouse>(), new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1 });
            yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "a mouse press cannot fire while touch aiming is active");
            // Release again so the activation gate of the next test is not blocked by a held button.
            InputSystem.QueueStateEvent(Mouse.current, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 0 });
            yield return null; yield return null;
        }
        [UnityTest] public IEnumerator BackgroundingPausesAndReturningNeedsAContinue()
        {
            yield return StartTouching();
            flow.SendMessage("OnApplicationPause", true);
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            Assert.AreEqual(0f, Time.timeScale);
            flow.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(AppPhase.Paused, flow.Phase, "returning to the foreground never resumes by itself");
            Assert.IsTrue(flow.HasFocus, "the continue button must be usable again");
            Assert.IsFalse(flow.CanSimulate);
            yield return null; yield return null;
            Assert.IsTrue(flow.UserInputAllowed, "input is available again after the activation frame");
            flow.ContinueGame();
            Assert.AreEqual(AppPhase.Playing, flow.Phase, "continue is what resumes the run");
            Assert.AreEqual(1f, Time.timeScale);
        }
        [UnityTest] public IEnumerator FocusAndPauseEventsInEitherOrderStayPausedUntilContinue()
        {
            yield return StartTouching();
            flow.SendMessage("OnApplicationPause", true);
            flow.SendMessage("OnApplicationFocus", false);
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            flow.SendMessage("OnApplicationFocus", true);
            flow.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            Assert.IsTrue(flow.HasFocus);
            yield return null; yield return null;
            Assert.IsTrue(flow.UserInputAllowed);
            flow.ContinueGame();
            Assert.AreEqual(AppPhase.Playing, flow.Phase);
            flow.SendMessage("OnApplicationFocus", false);
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            flow.SendMessage("OnApplicationPause", true);
            flow.SendMessage("OnApplicationPause", false);
            flow.SendMessage("OnApplicationFocus", true);
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            Assert.IsTrue(flow.HasFocus);
            yield return null; yield return null;
            flow.ContinueGame();
            Assert.AreEqual(AppPhase.Playing, flow.Phase);
        }
    }
}
#endif
