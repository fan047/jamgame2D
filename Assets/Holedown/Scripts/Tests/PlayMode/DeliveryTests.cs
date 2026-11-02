using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class DeliveryTests
    {
        [UnityTest] public IEnumerator DefaultLevelCanBeWonThroughPhysicalVolleys()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            for(int round=0;round<5;round++)
            {
                Assert.IsTrue(flow.TryFire(Vector2.down));
                float deadline=Time.time+32;
                while(flow.Session.Phase==SessionPhase.Volley && Time.time<deadline) yield return null;
                Assert.AreNotEqual(SessionPhase.Lost,flow.Session.Phase);
            }
            Assert.AreEqual(SessionPhase.Won,flow.Session.Phase);
            Assert.IsTrue(flow.Board.TargetDestroyed); Assert.AreEqual(4,flow.Board.IncomingCursor);
            Assert.IsTrue(GameObject.Find("UI/ResultPanel").activeSelf);
            for(int i=0;i<10;i++)
            {
                var id=flow.Session.SessionId;
                flow.Restart(); yield return null; yield return null;
                flow=Object.FindFirstObjectByType<GameFlowController>();
                Assert.AreNotEqual(id,flow.Session.SessionId);
                Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase);
                Assert.AreEqual(11,flow.Board.Blocks.Count);
                Assert.AreEqual(0,Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None).Length);
            }
        }
        [UnityTest] public IEnumerator BoardAndUiFitPortraitAndLandscape()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            var camera=Camera.main;
            foreach(float aspect in new[]{720f/1280f,1280f/720f})
            {
                camera.aspect=aspect; yield return null; yield return null;
                foreach(var point in new[]{new Vector3(-0.5f,-1.3f,0),new Vector3(8.5f,10.7f,0)})
                {
                    var viewport=camera.WorldToViewportPoint(point);
                    Assert.That(viewport.x,Is.InRange(0f,1f));Assert.That(viewport.y,Is.InRange(0f,1f));
                }
                var world=new Vector3(3,5,0);var screen=camera.WorldToScreenPoint(world);
                var back=camera.ScreenToWorldPoint(screen);
                Assert.Less(Vector3.Distance(world,back),0.001f);
            }
            camera.ResetAspect();
        }
    }
}
