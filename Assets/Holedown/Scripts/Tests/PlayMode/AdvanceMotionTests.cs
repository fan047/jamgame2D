#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Holedown.Tests
{
    // Covers the mid-stage advance animation: gating, arrival, pause and cancel.
    public sealed class AdvanceMotionTests
    {
        GameFlowController controller;
        [SetUp] public void Setup() { controller=null;BoardView.SkipAdvanceForTests=false; }
        [TearDown] public void Cleanup() { if(controller)controller.enabled=false;BoardView.SkipAdvanceForTests=false; }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;
            controller=Object.FindFirstObjectByType<GameFlowController>();
            controller.SendMessage("OnApplicationFocus",true);yield return null;yield return PlaySceneEntry.StartIfReady();
        }
        [UnityTest] public IEnumerator AdvanceBlocksFireUntilTheRowArrives()
        {
            yield return Load();var view=Object.FindFirstObjectByType<BoardView>();Assert.AreEqual(0.2f,view.AdvanceSeconds,0.0001f);
            int cursor=controller.Board.IncomingCursor;var outgoing=controller.Board.Blocks.Values.First();var outgoingId=outgoing.Id;int outgoingRow=outgoing.Row;
            Assert.IsTrue(controller.TryFire(Vector2.down));controller.RequestRecall();yield return null;yield return null;
            Assert.IsTrue(controller.IsAdvancing,"advance should start after the volley settles");
            Assert.AreEqual(cursor+1,controller.Board.IncomingCursor);
            Assert.IsFalse(controller.CanSimulate);Assert.IsFalse(controller.TryFire(Vector2.down),"clicking during advance must not fire");
            var risen=controller.Board.Blocks.Values.First(b=>b.Id==outgoingId);
            Assert.AreEqual(outgoingRow+1,risen.Row,"existing blocks rise exactly one row");
            var entering=view.Views.Values.Where(v=>v&&v.transform.position.y<0).ToArray();
            // The incoming row starts below the bottom line, so its label stays hidden until arrival.
            foreach(var block in entering)Assert.Less(block.transform.position.y,0f,"incoming blocks start below the bottom line");
            float deadline=Time.realtimeSinceStartup+2f;
            while(controller.IsAdvancing&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(controller.IsAdvancing,"advance must finish");Assert.IsTrue(controller.CanSimulate);
            Assert.IsTrue(controller.TryFire(Vector2.down),"aiming resumes after the advance completes");
        }
        [UnityTest] public IEnumerator AdvancingSupportsPauseAndFreezesOnPause()
        {
            yield return Load();
            Assert.IsTrue(controller.TryFire(Vector2.down));controller.RequestRecall();yield return null;yield return null;
            Assert.IsTrue(controller.IsAdvancing);
            controller.RequestPause();yield return null;
            Assert.AreEqual(AppPhase.Paused,controller.Phase);Assert.AreEqual(0f,Time.timeScale);
            var frozen=Object.FindFirstObjectByType<BoardView>().Views.Values.Select(v=>v.transform.position.y).ToArray();
            yield return new WaitForSecondsRealtime(0.1f);
            CollectionAssert.AreEqual(frozen,Object.FindFirstObjectByType<BoardView>().Views.Values.Select(v=>v.transform.position.y).ToArray());
            controller.ContinueGame();yield return null;
            Assert.AreEqual(AppPhase.Playing,controller.Phase);Assert.AreEqual(1f,Time.timeScale);
            float deadline=Time.realtimeSinceStartup+2f;
            while(controller.IsAdvancing&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(controller.IsAdvancing);Assert.IsTrue(controller.CanSimulate);
        }
        [UnityTest] public IEnumerator RetryAndStageChangeCancelTheAdvance()
        {
            yield return Load();
            Assert.IsTrue(controller.TryFire(Vector2.down));controller.RequestRecall();yield return null;yield return null;
            Assert.IsTrue(controller.IsAdvancing);
            controller.RestartRun();yield return null;yield return null;
            Assert.IsFalse(controller.IsAdvancing);Assert.AreEqual(SessionPhase.Aiming,controller.Session.Phase);
            var view=Object.FindFirstObjectByType<BoardView>();
            foreach(var block in view.Views.Values)Assert.IsTrue(block.GetComponentInChildren<TMPro.TMP_Text>(true).enabled,"a cancelled advance must not leave hidden labels");
            Assert.IsTrue(controller.CanSimulate);Assert.IsTrue(controller.TryFire(Vector2.down),"retry leaves a playable board");
        }
        [UnityTest] public IEnumerator ZeroDurationKeepsRulesImmediate()
        {
            yield return Load();var view=Object.FindFirstObjectByType<BoardView>();
            var so=new UnityEditor.SerializedObject(view);so.FindProperty("advanceSeconds").floatValue=0f;so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(controller.TryFire(Vector2.down));controller.RequestRecall();yield return null;yield return null;
            Assert.IsFalse(controller.IsAdvancing);Assert.AreEqual(SessionPhase.Aiming,controller.Session.Phase);Assert.IsTrue(controller.TryFire(Vector2.down));
        }
    }
}
#endif
