using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class LifecycleTests
    {
        [UnityTest] public IEnumerator RecallCancelsPendingAndSettlesOnlyOnce()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(flow.TryFire(Vector2.down)); Assert.IsFalse(flow.TryFire(Vector2.down));
            Assert.Greater(flow.Session.Pending,0); Assert.AreEqual(1,flow.Session.ActiveCount);
            flow.RequestRecall(); flow.RequestRecall();
            yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase);
            Assert.AreEqual(1,flow.Board.IncomingCursor); Assert.AreEqual(0,flow.Session.ActiveCount);
            Assert.AreEqual(0,Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None).Length);
            yield return new WaitForSeconds(0.15f); Assert.AreEqual(1,flow.Board.IncomingCursor);
        }
        [UnityTest] public IEnumerator TimeoutUsesSameRecallPath()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            float original=flow.Settings.recallTimeout;
            try
            {
                flow.Settings.recallTimeout=0.15f; flow.TryFire(Vector2.down);
                yield return new WaitForSeconds(0.35f);
                Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase); Assert.AreEqual(1,flow.Board.IncomingCursor);
                Assert.AreEqual(0,flow.Session.Pending); Assert.AreEqual(0,flow.Session.ActiveCount);
            }
            finally { flow.Settings.recallTimeout=original; }
        }
        [UnityTest] public IEnumerator NaturalVolleyCompletes()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            var flow=Object.FindFirstObjectByType<GameFlowController>(); flow.TryFire(Vector2.down);
            float end=Time.time+32;
            while(flow.Session.Phase==SessionPhase.Volley && Time.time<end) yield return null;
            Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase); Assert.AreEqual(1,flow.Board.IncomingCursor);
            Assert.AreEqual(0,flow.Session.ActiveCount); Assert.AreEqual(0,flow.Session.Pending);
        }
    }
}
