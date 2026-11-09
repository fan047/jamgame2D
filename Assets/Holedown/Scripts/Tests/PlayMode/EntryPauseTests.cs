using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class EntryPauseTests
    {
        [UnityTest]
        public IEnumerator TitleBlocksFireAndStartIsSingleTransition()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            flow.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
            Assert.AreEqual(AppPhase.Title, flow.Phase); Assert.IsFalse(flow.TryFire(Vector2.down));
            flow.StartGame(); var id = flow.Session.SessionId; flow.StartGame();
            Assert.AreEqual(id, flow.Session.SessionId); Assert.AreEqual(AppPhase.Playing, flow.Phase);
            Assert.IsFalse(flow.TryFire(Vector2.down), "Start frame cannot fire");
            // The opening board is presented at once; only the waiting row fades in below it.
            Assert.IsFalse(flow.IsAdvancing);
            yield return null; Assert.IsTrue(flow.TryFire(Vector2.down)); flow.RequestRecall();
        }
        [UnityTest]
        public IEnumerator PausePreservesVolleyAndRestoresOriginalScale()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            float original = Time.timeScale;
            try
            {
                Time.timeScale = 20;
                Assert.IsTrue(flow.TryFire(Vector2.down));
                var ball = Object.FindFirstObjectByType<BallActor>();
                var position = ball.transform.position; var velocity = ball.Velocity;
                var id = flow.Session.SessionId; int pending = flow.Session.Pending;
                flow.RequestPause(); flow.RequestPause();
                Assert.AreEqual(0, Time.timeScale); Assert.AreEqual(AppPhase.Paused, flow.Phase);
                flow.RequestRecall(); Assert.AreEqual(SessionPhase.Volley, flow.Session.Phase);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.AreEqual(pending, flow.Session.Pending); Assert.AreEqual(position, ball.transform.position); Assert.AreEqual(velocity, ball.Velocity);
                Assert.IsFalse(flow.TryFire(Vector2.down));
                flow.ContinueGame(); Assert.AreEqual(20, Time.timeScale); Assert.AreEqual(id, flow.Session.SessionId);
                yield return null; flow.RequestRecall(); yield return null;
            }
            finally { flow.enabled = false; Time.timeScale = original; }
        }
        [UnityTest]
        public IEnumerator FocusRegainStaysPausedAndDoesNotConsumeActivationAsContinue()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var id = flow.Session.SessionId;
            flow.SendMessage("OnApplicationFocus", false);
            Assert.AreEqual(AppPhase.Paused, flow.Phase); Assert.IsFalse(flow.HasFocus);
            flow.ContinueGame(); Assert.AreEqual(AppPhase.Paused, flow.Phase);
            flow.SendMessage("OnApplicationFocus", true); flow.ContinueGame();
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            yield return null; yield return null;
            flow.ContinueGame(); Assert.AreEqual(AppPhase.Playing, flow.Phase); Assert.AreEqual(id, flow.Session.SessionId);
        }
        [UnityTest]
        public IEnumerator PausedReloadStaysInSceneAndReleasesPause()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var id = flow.Session.SessionId; float scale = Time.timeScale;
            flow.RequestPause(); flow.ReloadAndRestart();
            Assert.AreEqual(scale, Time.timeScale); Assert.AreEqual(AppPhase.Playing, flow.Phase);
            Assert.AreEqual(flow, Object.FindFirstObjectByType<GameFlowController>());
            Assert.AreNotEqual(id, flow.Session.SessionId); Assert.AreEqual(0, flow.Campaign.StageIndex);
            yield return null;
        }
    }
}
