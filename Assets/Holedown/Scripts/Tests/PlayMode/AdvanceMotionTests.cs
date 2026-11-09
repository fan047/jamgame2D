#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Holedown.Tests
{
    // Covers the mid-stage advance: the waiting row hands over continuously into row 0,
    // plus gating, pause and cancel. The opening board is no longer raised.
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
        [UnityTest] public IEnumerator OpeningBoardSitsOnItsRuleRowsAndOnlyTheWaitingRowFades()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;
            var flow=Object.FindFirstObjectByType<GameFlowController>();var view=Object.FindFirstObjectByType<BoardView>();
            flow.SendMessage("OnApplicationFocus",true);yield return null;yield return null;
            var preview=Object.FindFirstObjectByType<IncomingRowPreview>();
            Assert.IsNotNull(preview,"the waiting row needs a display-only presenter");
            flow.StartGame();
            Assert.IsFalse(view.IsAdvancing,"the opening board is shown, not raised");
            Assert.IsFalse(flow.IsAdvancing);
            Assert.IsTrue(flow.CanSimulate,"simulation is available as soon as the board is shown");
            Assert.AreEqual(0.2f,view.AdvanceSeconds,0.0001f);
            foreach(var block in view.Views.Values)
            {
                var rule=flow.Board.Blocks[block.BlockId];
                Assert.AreEqual(rule.Column+rule.Width*0.5f,block.transform.position.x,0.0001f);
                Assert.AreEqual(rule.Row+rule.Height*0.5f,block.transform.position.y,0.0001f,"blocks start on their rule rows");
                Assert.IsTrue(block.GetComponentInChildren<TMP_Text>(true).enabled);
            }
            Assert.Greater(preview.CellCount,0,"the next row waits below the floor line");
            foreach(var cell in preview.Cells.Where(c=>c&&c.gameObject.activeSelf))
                Assert.Less(cell.transform.position.y,0f,"nothing waits inside the play field");
        }
        [UnityTest] public IEnumerator AdvanceHandsTheWaitingRowOverWithoutAJump()
        {
            yield return Load();var view=Object.FindFirstObjectByType<BoardView>();var preview=view.Preview;
            Assert.IsNotNull(preview);
            Assert.IsTrue(controller.Board.TryPeekIncomingRow(out var waiting));
            var waitingX=waiting.Select(b=>b.Column+b.Width*0.5f).OrderBy(x=>x).ToArray();
            float waitingY=preview.CentreY;
            int cursor=controller.Board.IncomingCursor;
            Assert.IsTrue(controller.TryFire(Vector2.down));controller.RequestRecall();
            // Freeze the clock so the hand-over pose can be read exactly: the arriving row
            // must start on the preview line, not above it.
            float saved=Time.timeScale;
            try
            {
                Time.timeScale=0f;
                yield return null;yield return null;
                Assert.IsTrue(controller.IsAdvancing,"the advance starts after the volley settles");
                Assert.AreEqual(cursor+1,controller.Board.IncomingCursor);
                Assert.IsFalse(controller.CanSimulate);Assert.IsFalse(controller.TryFire(Vector2.down),"clicking during the advance must not fire");
                var arriving=view.Views.Values.Where(v=>v&&v.IsEntering).OrderBy(v=>v.transform.position.x).ToArray();
                Assert.AreEqual(waiting.Count,arriving.Length,"one arriving view per previewed column");
                CollectionAssert.AreEqual(waitingX,arriving.Select(v=>v.transform.position.x).ToArray(),"same columns");
                foreach(var block in arriving)
                {
                    Assert.AreEqual(waitingY,block.transform.position.y,0.0001f,"the arriving row starts on the preview line");
                    Assert.IsFalse(block.GetComponentInChildren<TMP_Text>(true).enabled,"numbers stay hidden until the row lands");
                }
                Assert.AreEqual(0,preview.CellCount,"the preview yields to the real row");
                yield return null;
                foreach(var block in arriving)
                    Assert.AreEqual(waitingY,block.transform.position.y,0.0001f,"the frozen row holds the preview line");
            }
            finally { Time.timeScale=saved; }
            Assert.AreEqual(1f,Time.timeScale);
            float deadline=Time.realtimeSinceStartup+2f;
            while(controller.IsAdvancing&&Time.realtimeSinceStartup<deadline){if(controller.Phase!=AppPhase.Playing){yield return PlaySceneEntry.ResumeAfterFocusLoss(controller);continue;}yield return null;}
            Assert.IsFalse(controller.IsAdvancing,"advance must finish");Assert.IsTrue(controller.CanSimulate);
            foreach(var block in view.Views.Values)
            {
                var rule=controller.Board.Blocks[block.BlockId];
                Assert.AreEqual(rule.Column+rule.Width*0.5f,block.transform.position.x,0.0001f);
                Assert.AreEqual(rule.Row+rule.Height*0.5f,block.transform.position.y,0.0001f);
                Assert.IsTrue(block.GetComponentInChildren<TMP_Text>(true).enabled,"numbers return once the row lands");
            }
            // The next waiting row replaced the one that arrived: no duplicated row.
            Assert.IsTrue(controller.Board.TryPeekIncomingRow(out var next));
            Assert.AreEqual(next.Count,preview.CellCount);
            Assert.IsTrue(controller.TryFire(Vector2.down),"aiming resumes after the advance");
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
            while(controller.IsAdvancing&&Time.realtimeSinceStartup<deadline){if(controller.Phase!=AppPhase.Playing){yield return PlaySceneEntry.ResumeAfterFocusLoss(controller);continue;}yield return null;}
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
            foreach(var block in view.Views.Values)Assert.IsFalse(block.IsEntering,"a cancelled advance must not leave a preview silhouette");
            var leftover=view.Preview.Cells.Where(c=>c&&c.gameObject.activeSelf).ToArray();
            Assert.IsTrue(controller.Board.TryPeekIncomingRow(out var waiting));
            Assert.AreEqual(waiting.Count,leftover.Length,"the fresh attempt shows exactly its own waiting row");
            Assert.IsTrue(controller.CanSimulate);Assert.IsTrue(controller.TryFire(Vector2.down),"retry leaves a playable board");
        }
        [UnityTest] public IEnumerator ZeroDurationKeepsRulesImmediate()
        {
            yield return Load();var view=Object.FindFirstObjectByType<BoardView>();
            var so=new UnityEditor.SerializedObject(view);so.FindProperty("advanceSeconds").floatValue=0f;so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(controller.TryFire(Vector2.down));controller.RequestRecall();yield return null;yield return null;
            Assert.IsFalse(controller.IsAdvancing);Assert.AreEqual(SessionPhase.Aiming,controller.Session.Phase);Assert.IsTrue(controller.TryFire(Vector2.down));
            Assert.IsTrue(controller.Board.TryPeekIncomingRow(out var waiting));
            Assert.AreEqual(waiting.Count,view.Preview.CellCount,"a zero-duration advance still refreshes the waiting row");
        }
    }
}
#endif
