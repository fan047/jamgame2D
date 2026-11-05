using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class ProgressionTests
    {
        GrowthSeedScope seed;
        [SetUp] public void Setup() { seed = new GrowthSeedScope(); }
        [TearDown] public void Cleanup() { seed.Dispose(); }
        static IEnumerator Load()
        { yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady(); }
        static IEnumerator EmptyRound(GameFlowController flow)
        { Assert.IsTrue(flow.TryFire(Vector2.down)); flow.RequestRecall(); yield return null; yield return null; }
        [UnityTest] public IEnumerator DefeatThenTenRestartsHaveNoResidualState()
        {
            yield return Load();
            for(int cycle=0;cycle<10;cycle++)
            {
                var flow=Object.FindFirstObjectByType<GameFlowController>();
                Assert.AreEqual(11,flow.Board.Blocks.Count); Assert.AreEqual(0,flow.Session.Round);
                var oldId=flow.Session.SessionId;
                for(int i=0;i<5;i++) yield return EmptyRound(flow);
                Assert.AreEqual(SessionPhase.Lost,flow.Session.Phase);
                Assert.IsFalse(flow.TryFire(Vector2.down));
                Assert.IsTrue(GameObject.Find("UI/ResultPanel").activeSelf);
                flow.Restart(); yield return null; yield return null;
                var next=Object.FindFirstObjectByType<GameFlowController>();
                Assert.AreNotEqual(oldId,next.Session.SessionId); Assert.AreEqual(0,next.Session.ActiveCount);
                Assert.AreEqual(0,Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(11,Object.FindObjectsByType<BlockView>(FindObjectsSortMode.None).Length);
            }
        }
        [UnityTest] public IEnumerator DestroyedTargetWinsWithoutDangerAdvance()
        {
            yield return Load(); var flow=Object.FindFirstObjectByType<GameFlowController>();
            for(int i=0;i<4;i++) yield return EmptyRound(flow);
            Assert.AreEqual(4,flow.Board.IncomingCursor);
            Assert.AreEqual(7,flow.Board.Blocks.Values.Max(b=>b.Row));
            var target=flow.Board.Blocks.Values.Single(b=>b.IsTarget);
            // Rule injection isolates settlement priority; physical damage is covered separately.
            while(flow.Board.Blocks.ContainsKey(target.Id)) flow.Board.ApplyHit(target.Id);
            yield return EmptyRound(flow);
            Assert.AreEqual(SessionPhase.Won,flow.Session.Phase);
            Assert.AreEqual(7,flow.Board.Blocks.Values.Max(b=>b.Row));
            Assert.IsFalse(flow.TryFire(Vector2.down));
            flow.Restart(); yield return null; yield return null;
            Assert.AreEqual(SessionPhase.Aiming,Object.FindFirstObjectByType<GameFlowController>().Session.Phase);
        }
    }
}
