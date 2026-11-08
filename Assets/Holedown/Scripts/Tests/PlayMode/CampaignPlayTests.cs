#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class CampaignPlayTests
    {
        string directory;
        [SetUp] public void Setup()
        {
            BoardView.SkipAdvanceForTests=true;
            directory=Path.GetFullPath("Library/Locus/tmp/campaign-play/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"));
            string Block(int c,int r,int hp,bool target,int reward)=>$"{{\"column\":{c},\"row\":{r},\"width\":1,\"height\":1,\"health\":{hp},\"target\":{target.ToString().ToLowerInvariant()},\"rewardBalls\":{reward}}}";
            var stages=new string[3];for(int i=0;i<3;i++)stages[i]=$"{{\"id\":\"s{i}\",\"name\":\"Stage {i}\",\"initialBlocks\":[{Block(3,2,1,false,i==0?3:i==1?5:0)},{Block(0,6,1,false,0)}],\"incomingRows\":[{{\"blocks\":[{Block(3,0,1,true,0)}]}}]}}";
            File.WriteAllText(Path.Combine(directory,"level.json"),"{\"schemaVersion\":2,\"version\":\"play-growth\",\"levels\":["+string.Join(",",stages)+"]}");SceneManager.sceneLoaded+=Configure;
        }
        void Configure(Scene scene,LoadSceneMode mode){var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();if(f){var so=new SerializedObject(f);so.FindProperty("externalConfigDirectory").stringValue=directory;so.ApplyModifiedPropertiesWithoutUndo();}}
        [TearDown] public void Cleanup(){SceneManager.sceneLoaded-=Configure;Directory.Delete(directory,true);BoardView.SkipAdvanceForTests=false;}
        static IEnumerator Shoot(GameFlowController f)
        {
            Assert.IsTrue(f.TryFire(new Vector2(-0.5f,-6.5f)));float until=Time.realtimeSinceStartup+10;
            while((f.Session.Phase==SessionPhase.Volley || f.Session.Phase==SessionPhase.Recalling)&&Time.realtimeSinceStartup<until)
            {
                if(f.Phase!=AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(f); continue; }
                Assert.Less(Time.realtimeSinceStartup,until,"Physical volley did not finish");
                yield return null;
            }
            Assert.Less(Time.realtimeSinceStartup,until,"Physical volley did not finish");
        }
        [UnityTest] public IEnumerator PhysicalThreeStageGrowthAndDoubleNextAreCorrect()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            for(int i=0;i<3;i++)
            {
                Assert.AreEqual(i,f.Campaign.StageIndex);Assert.AreEqual(i==0?12:i==1?15:20,f.Campaign.EntryBalls);
                yield return Shoot(f);Assert.AreEqual(i==0?3:i==1?5:0,f.Campaign.PendingBonus);Assert.AreEqual(i==0?12:i==1?15:20,f.Campaign.EntryBalls);
                yield return Shoot(f);Assert.AreEqual(SessionPhase.Won,f.Session.Phase);
                var old=f.Session.SessionId;
                f.NextStage();f.NextStage();
                if(i<2){Assert.AreEqual(i+1,f.Campaign.StageIndex);Assert.AreNotEqual(old,f.Session.SessionId);Assert.AreEqual(0,f.Campaign.PendingBonus);}
                else Assert.AreEqual(old,f.Session.SessionId);
                yield return null;
            }
            Assert.IsTrue(f.Campaign.IsFinalStage);Assert.AreEqual(8,f.Campaign.ConfirmedBonus);
        }
        [UnityTest] public IEnumerator PhysicalNineStageRunConfirmsGrowthAcrossBothGroupBoundaries()
        {
            string Block(int reward,bool target)=>$"{{\"column\":3,\"row\":{(target?0:2)},\"width\":1,\"height\":1,\"health\":1,\"target\":{target.ToString().ToLowerInvariant()},\"rewardBalls\":{reward}}}";
            var stages=new string[9];
            for(int i=0;i<9;i++)stages[i]=$"{{\"id\":\"nine-{i}\",\"name\":\"Fixture {i}\",\"initialBlocks\":[{Block(i<8?i+1:0,false)}],\"incomingRows\":[{{\"blocks\":[{Block(0,true)}]}}]}}";
            File.WriteAllText(Path.Combine(directory,"level.json"),"{\"schemaVersion\":2,\"version\":\"nine-test-only\",\"levels\":["+string.Join(",",stages)+"]}");
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
            var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            for(int i=0;i<9;i++)
            {
                Assert.IsFalse(f.IsPlannerTest);Assert.AreEqual(i,f.Campaign.StageIndex);Assert.AreEqual(9,f.Campaign.StageCount);
                int expected=12+i*(i+1)/2;Assert.AreEqual(expected,f.EntryBalls);
                yield return Shoot(f);Assert.AreEqual(i<8?i+1:0,f.Campaign.PendingBonus);Assert.AreEqual(expected,f.EntryBalls);
                yield return Shoot(f);Assert.AreEqual(SessionPhase.Won,f.Session.Phase);
                var session=f.Session.SessionId;f.NextStage();yield return null;
                if(i<8)Assert.AreNotEqual(session,f.Session.SessionId);else Assert.AreEqual(session,f.Session.SessionId);
            }
            Assert.IsTrue(f.Campaign.IsFinalStage);Assert.AreEqual(36,f.Campaign.ConfirmedBonus);Assert.AreEqual(48,f.EntryBalls);
        }
        [UnityTest] public IEnumerator TenStageTwoRetriesCannotFarmAndUseLoadedSnapshot()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            yield return Shoot(f);yield return Shoot(f);f.NextStage();yield return null;
            File.WriteAllText(Path.Combine(directory,"level.json"),"changed file must not affect retry");
            for(int i=0;i<10;i++)
            {
                yield return Shoot(f);Assert.AreEqual(5,f.Campaign.PendingBonus);f.TryFire(Vector2.down);f.RequestRecall();yield return null;yield return null;
                Assert.AreEqual(SessionPhase.Lost,f.Session.Phase);var id=f.Session.SessionId;f.RetryStage();f.RetryStage();yield return null;
                Assert.AreNotEqual(id,f.Session.SessionId);Assert.AreEqual(15,f.Campaign.EntryBalls);Assert.AreEqual(0,f.Campaign.PendingBonus);Assert.AreEqual(3,f.Campaign.ConfirmedBonus);
                Assert.AreEqual(2,UnityEngine.Object.FindObjectsByType<BlockView>(FindObjectsSortMode.None).Length);
            }
            f.RestartRun();yield return null;Assert.AreEqual(0,f.Campaign.StageIndex);Assert.AreEqual(12,f.Campaign.EntryBalls);Assert.IsNull(f.InitializationError);
        }
        [UnityTest] public IEnumerator FourCustomRewardBlocksUseRealHitsAndDoNotIncreaseCurrentVolley()
        {
            string Block(int c,int reward,bool target=false)=>$"{{\"column\":{c},\"row\":0,\"width\":1,\"height\":1,\"health\":1,\"target\":{target.ToString().ToLowerInvariant()},\"rewardBalls\":{reward}}}";
            string Stage(string id,string initial)=>$"{{\"id\":\"{id}\",\"name\":\"{id}\",\"initialBlocks\":[{initial}],\"incomingRows\":[{{\"blocks\":[{Block(7,0,true)}]}}]}}";
            File.WriteAllText(Path.Combine(directory,"level.json"),"{\"schemaVersion\":2,\"version\":\"multi-reward\",\"levels\":["+Stage("multi",string.Join(",",new[]{Block(0,3),Block(2,5),Block(4,8),Block(6,11)}))+","+Stage("final","")+"]}");
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
            var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            foreach(int reward in new[]{3,5,8,11})
            {
                BlockState next=null;foreach(var b in f.Board.Blocks.Values)if(b.RewardBalls==reward){next=b;break;}
                if(next==null)continue; // A previous volley may physically hit more than one reward.
                var destination=new Vector2(next.Column+.5f,next.Row+.5f);
                Assert.IsTrue(f.TryFire(destination-AimInput.Origin));float until=Time.realtimeSinceStartup+40;
                while(f.Session.Phase==SessionPhase.Volley||f.Session.Phase==SessionPhase.Recalling){if(f.Phase!=AppPhase.Playing){yield return PlaySceneEntry.ResumeAfterFocusLoss(f);continue;}Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                Assert.AreEqual(12,f.Campaign.EntryBalls);yield return null;
            }
            Assert.AreEqual(27,f.Campaign.PendingBonus);
        }
        [UnityTest] public IEnumerator ReloadReadsNewSettingsWhileRestartRunDoesNot()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();var f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            string p=Path.Combine(directory,"settings.json");File.WriteAllText(p,File.ReadAllText(p).Replace("\"ballsPerRound\": 12","\"ballsPerRound\": 7"));
            f.RestartRun();yield return null;Assert.AreEqual(12,f.Campaign.EntryBalls);
            f.ReloadAndRestart();yield return null;yield return null;f=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();Assert.AreEqual(7,f.Campaign.EntryBalls);Assert.AreEqual(0,f.Campaign.StageIndex);
        }
    }
}
#endif
