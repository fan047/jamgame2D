#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class PlannerModeTests
    {
        string directory,original;
        [SetUp] public void Setup()
        {
            BoardView.SkipAdvanceForTests=true;
            directory=Path.GetFullPath("Library/Locus/tmp/planner-mode/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"));
            var levels=new JArray();
            for(int i=0;i<9;i++)
            {
                JObject Block(int c,int r,int bonus,bool target=false)=>new JObject{{"column",c},{"row",r},{"width",1},{"height",1},{"health",1},{"target",target},{"rewardBalls",bonus}};
                levels.Add(new JObject{{"id","s"+i},{"name","Stage "+i},{"initialBlocks",i<8?new JArray(Block(3,2,i+1)):new JArray()},{"incomingRows",new JArray(new JObject{{"blocks",new JArray(Block(3,0,0,true))}})}});
            }
            original=new JObject{{"schemaVersion",2},{"version","planner-fixture"},{"levels",levels}}.ToString();
            File.WriteAllText(Path.Combine(directory,"level.json"),original);SceneManager.sceneLoaded+=Configure;
        }
        void Configure(Scene scene,LoadSceneMode mode)
        {
            var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            if(!f)return;var so=new SerializedObject(f);so.FindProperty("externalConfigDirectory").stringValue=directory;so.FindProperty("plannerTestMode").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown] public void Cleanup(){SceneManager.sceneLoaded-=Configure;Directory.Delete(directory,true);BoardView.SkipAdvanceForTests=false;}
        static IEnumerator Ready(GameFlowController f)
        {
            f.SendMessage("OnApplicationFocus",true);yield return null;yield return null;
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;
            var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();yield return Ready(f);
            Assert.AreEqual(AppPhase.PlannerSelection,f.Phase);Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<PlannerTestView>());
        }
        [UnityTest] public IEnumerator BackgroundUpdatesAcrossGroupsAndReloadWithMissingSlotFallback()
        {
            yield return Load();
            var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var visuals=UnityEngine.Object.FindFirstObjectByType<SceneVisuals>();
            var serialized=new SerializedObject(visuals);
            var theme=(VisualTheme)serialized.FindProperty("theme").objectReferenceValue;
            var renderer=(SpriteRenderer)serialized.FindProperty("background").objectReferenceValue;
            var old1=theme.backgroundStages1To3;var old2=theme.backgroundStages4To6;var old3=theme.backgroundStages7To9;
            var texture=new Texture2D(2,2);var second=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f);var third=Sprite.Create(texture,new Rect(0,0,2,2),Vector2.one*.5f);
            try
            {
                theme.backgroundStages1To3=theme.background;theme.backgroundStages4To6=second;theme.backgroundStages7To9=third;
                foreach(int index in new[]{2,3,5,6,8})
                {
                    f.QueuePlannerTest("s"+index,PlannerBallMode.BaseOnly,1);yield return null;yield return null;
                    Assert.AreSame(index<3?theme.background:index<6?second:third,renderer.sprite);
                    f.ReloadAndRestart();yield return null;yield return null;
                    Assert.AreSame(index<3?theme.background:index<6?second:third,renderer.sprite);
                }
                theme.backgroundStages7To9=null;f.ReloadAndRestart();yield return null;yield return null;Assert.AreSame(theme.background,renderer.sprite);
            }
            finally
            {
                theme.backgroundStages1To3=old1;theme.backgroundStages4To6=old2;theme.backgroundStages7To9=old3;
                UnityEngine.Object.Destroy(second);UnityEngine.Object.Destroy(third);UnityEngine.Object.Destroy(texture);
            }
        }
        [UnityTest] public IEnumerator ModesReloadByStableIdAndManualBallCountDrivesVolley()
        {
            yield return Load();var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            foreach(var mode in new[]{PlannerBallMode.BaseOnly,PlannerBallMode.AllPreviousRewards,PlannerBallMode.Manual})
            {
                f.QueuePlannerTest("s6",mode,27);yield return null;yield return null;
                Assert.AreEqual(6,f.Campaign.StageIndex);Assert.AreEqual(9,f.Campaign.StageCount);Assert.IsFalse(f.Campaign.IsFinalStage);
                Assert.AreEqual(mode==PlannerBallMode.BaseOnly?12:mode==PlannerBallMode.AllPreviousRewards?33:27,f.EntryBalls);
                Assert.IsTrue(f.TryFire(Vector2.down));Assert.AreEqual(f.EntryBalls,f.Session.Pending+f.Session.ActiveCount);
                f.RequestPause();yield return null;
                var settings=JObject.Parse(File.ReadAllText(Path.Combine(directory,"settings.json")));settings["ballsPerRound"]=7;File.WriteAllText(Path.Combine(directory,"settings.json"),settings.ToString());
                var level=JObject.Parse(original);var levels=(JArray)level["levels"];var selected=levels[6];selected.Remove();levels.Insert(3,selected);File.WriteAllText(Path.Combine(directory,"level.json"),level.ToString());
                var session=f.Session.SessionId;f.ReloadAndRestart();yield return null;yield return null;
                Assert.IsNull(f.PlannerError);Assert.AreEqual("s6",f.StageId);Assert.AreEqual(3,f.Campaign.StageIndex);Assert.AreNotEqual(session,f.Session.SessionId);
                Assert.AreEqual(mode==PlannerBallMode.BaseOnly?7:mode==PlannerBallMode.AllPreviousRewards?13:27,f.EntryBalls);
                Assert.AreEqual(0,UnityEngine.Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None).Length);
                Assert.AreEqual(SessionPhase.Aiming,f.Session.Phase);
                // Restore the file snapshot for the next independent mode.
                File.WriteAllText(Path.Combine(directory,"level.json"),original);settings["ballsPerRound"]=12;File.WriteAllText(Path.Combine(directory,"settings.json"),settings.ToString());
                f.ReturnToPlannerSelection();yield return null;yield return null;f.ReloadAndRestart();yield return null;yield return null;
            }
        }
        [UnityTest] public IEnumerator TestWinRetestsSameRealNonFinalStageAndCanReturnToSelection()
        {
            yield return Load();var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();f.QueuePlannerTest("s6",PlannerBallMode.Manual,3);yield return null;yield return null;
            for(int shot=0;shot<2;shot++)
            {
                Assert.IsTrue(f.TryFire(new Vector2(-.5f,shot==0?-6.5f:-8.5f)));float deadline=Time.realtimeSinceStartup+15;
                while(f.Session.Phase==SessionPhase.Volley||f.Session.Phase==SessionPhase.Recalling){Assert.AreEqual(AppPhase.Playing,f.Phase);Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                yield return null;
            }
            Assert.AreEqual(SessionPhase.Won,f.Session.Phase);Assert.AreEqual(7,f.Campaign.PendingBonus);Assert.IsFalse(f.Campaign.IsFinalStage);
            string id=f.StageId;var session=f.Session.SessionId;f.NextStage();Assert.AreEqual(id,f.StageId);Assert.AreEqual(session,f.Session.SessionId);
            f.RetryStage();yield return null;yield return null;Assert.AreEqual(id,f.StageId);Assert.AreNotEqual(session,f.Session.SessionId);Assert.AreEqual(0,f.Campaign.PendingBonus);Assert.AreEqual(3,f.EntryBalls);
            f.QueueCommand(FlowCommand.PlannerSelection);yield return null;yield return null;Assert.AreEqual(AppPhase.PlannerSelection,f.Phase);Assert.IsNull(f.PlannerContext);Assert.IsNull(f.Board);
        }
        [UnityTest] public IEnumerator InvalidMissingOrDeletedConfigPreservesPausedAttemptUntilRepaired()
        {
            yield return Load();var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();f.QueuePlannerTest("s6",PlannerBallMode.Manual,17);yield return null;yield return null;
            Assert.IsTrue(f.TryFire(Vector2.down));f.RequestPause();yield return null;
            var session=f.Session;var board=f.Board;string label=f.ConfigurationLabel;
            for(int kind=0;kind<3;kind++)
            {
                string path=Path.Combine(directory,"level.json");
                if(kind==0)File.WriteAllText(path,"broken json");
                else if(kind==1){File.Delete(path);File.Delete(Path.Combine(directory,"settings.json"));}
                else {File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"),true);var value=JObject.Parse(original);((JArray)value["levels"])[6].Remove();File.WriteAllText(path,value.ToString());}
                f.ReloadAndRestart();yield return null;yield return null;
                Assert.IsNotEmpty(f.PlannerError);Assert.AreSame(session,f.Session);Assert.AreSame(board,f.Board);Assert.AreEqual(label,f.ConfigurationLabel);
                Assert.AreEqual(AppPhase.Paused,f.Phase);Assert.AreEqual(0,Time.timeScale);Assert.AreEqual(17,f.EntryBalls);
            }
            File.WriteAllText(Path.Combine(directory,"level.json"),original);f.ReloadAndRestart();yield return null;yield return null;
            Assert.IsNull(f.PlannerError);Assert.AreNotSame(session,f.Session);Assert.AreEqual(AppPhase.Playing,f.Phase);Assert.AreEqual(17,f.EntryBalls);
        }
    }
}
#endif
