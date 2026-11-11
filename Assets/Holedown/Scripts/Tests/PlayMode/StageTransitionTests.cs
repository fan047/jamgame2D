#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Holedown.Tests
{
    public sealed class StageTransitionTests
    {
        GrowthSeedScope seed;
        GameFlowController flow;
        float oldTimeScale;
        Canvas Curtain => Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(c => c.name == "StageTransitionOverlay");
        [SetUp] public void Setup() { seed = new GrowthSeedScope(); oldTimeScale = Time.timeScale; }
        [TearDown] public void Cleanup()
        {
            var view = Object.FindFirstObjectByType<StageTransitionView>();
            if (view) view.enabled = false;
            if (flow) flow.enabled = false;
            Time.timeScale = oldTimeScale;
            seed.Dispose();
        }

        IEnumerator LoadWonStage()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            yield return null;
            flow = Object.FindFirstObjectByType<GameFlowController>();
            flow.SendMessage("OnApplicationFocus", true);
            yield return null; yield return null; yield return null;
            flow.StartGame(); yield return null; yield return null;
            // Rule fixture: exercise the actual result action without a long physical run.
            while (!flow.Board.Blocks.Values.Any(b => b.IsTarget)) flow.Board.Advance();
            var target = flow.Board.Blocks.Values.Single(b => b.IsTarget);
            while (flow.Board.Blocks.ContainsKey(target.Id)) flow.Board.ApplyHit(target.Id);
            Assert.IsTrue(flow.Session.TryFire(flow.EntryBalls));
            flow.Session.Recall(); Assert.IsTrue(flow.Session.TryBeginSettlement());
            flow.Session.Settle(flow.Board); flow.Campaign.CompleteStage();
            Object.FindFirstObjectByType<ResultView>().SendMessage("Refresh");
            yield return null;
            Assert.AreEqual(SessionPhase.Won, flow.Session.Phase);
        }

        [UnityTest] public IEnumerator NextButtonFadesAndCommitsExactlyOnceOnUnscaledTime()
        {
            yield return LoadWonStage();
            var view = Object.FindFirstObjectByType<StageTransitionView>();
            var button = GameObject.Find("UI/ResultPanel/Card/Advance").GetComponent<Button>();
            int previousStage = flow.Campaign.StageIndex;
            Time.timeScale = 0f;
            button.onClick.Invoke(); button.onClick.Invoke();
            Assert.IsTrue(view.IsPlaying);
            Assert.IsFalse(flow.UserInputAllowed);
            Assert.AreEqual(previousStage, flow.Campaign.StageIndex, "swap happens behind the opaque curtain");
            Assert.IsFalse(flow.TryFire(Vector2.down));
            yield return null; Canvas.ForceUpdateCanvases();
            Assert.IsTrue(Curtain.isRootCanvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, Curtain.renderMode);
            var bounds = Curtain.GetComponent<RectTransform>().rect;
            Assert.GreaterOrEqual(bounds.width * Curtain.scaleFactor, Screen.width - 1f);
            Assert.GreaterOrEqual(bounds.height * Curtain.scaleFactor, Screen.height - 1f);
            yield return new WaitForSecondsRealtime(view.Duration + .15f);
            Assert.IsFalse(view.IsPlaying);
            Assert.AreEqual(previousStage + 1, flow.Campaign.StageIndex);
            Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase);
            Assert.AreEqual(0, flow.Session.ActiveCount);
            Assert.IsFalse(flow.IsTransitioning);
            Assert.IsTrue(flow.UserInputAllowed);
            Assert.IsFalse(Curtain.gameObject.activeSelf);
        }

        [UnityTest] public IEnumerator DisablingDuringFadeReleasesTheInputLock()
        {
            yield return LoadWonStage();
            var view = Object.FindFirstObjectByType<StageTransitionView>();
            int previousStage = flow.Campaign.StageIndex;
            Assert.IsTrue(view.PlayNextStage());
            Assert.IsFalse(flow.UserInputAllowed);
            view.enabled = false;
            yield return null; yield return null; yield return null;
            Assert.IsFalse(view.IsPlaying);
            Assert.IsFalse(flow.IsTransitioning);
            Assert.IsTrue(flow.UserInputAllowed);
            Assert.AreEqual(previousStage, flow.Campaign.StageIndex);
            Assert.IsFalse(Curtain.gameObject.activeSelf);
        }
    }
}
#endif
