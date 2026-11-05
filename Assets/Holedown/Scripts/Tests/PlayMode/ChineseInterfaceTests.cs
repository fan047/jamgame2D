#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Holedown.Tests
{
    public sealed class ChineseInterfaceTests
    {
        string directory;bool planner;
        [SetUp] public void Setup()
        {
            planner=false;
            directory=Path.GetFullPath("Library/Locus/tmp/chinese-interface/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            foreach(string name in new[]{"settings.json","level.json"})File.Copy("Config/"+name,Path.Combine(directory,name));
            SceneManager.sceneLoaded+=Configure;
        }
        void Configure(Scene scene,LoadSceneMode mode)
        {
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();if(!flow)return;
            var serialized=new SerializedObject(flow);serialized.FindProperty("externalConfigDirectory").stringValue=directory;serialized.FindProperty("plannerTestMode").boolValue=planner;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown] public void Cleanup(){SceneManager.sceneLoaded-=Configure;Directory.Delete(directory,true);}
        static TMP_Text[] Visible()=>UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(t=>t.gameObject.activeInHierarchy).ToArray();
        static Button Button(string label)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<TMP_Text>()?.text==label);
        static void CheckVisible()
        {
            foreach(var text in Visible())
            {
                text.ForceMeshUpdate();
                Assert.IsFalse(Regex.IsMatch(text.text,@"\b(Start|Stage|Balls|Round|Paused|Continue|Recall|Reload|Defeat|PLANNER|Preparing)\b"),text.text);
                foreach(char character in text.text)if(!char.IsWhiteSpace(character))Assert.IsTrue(text.font.HasCharacter(character),"Missing glyph: "+character+" in "+text.text);
                Assert.IsFalse(text.isTextOverflowing,"Overflow: "+text.text);
            }
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();flow.SendMessage("OnApplicationFocus",true);yield return null;yield return null;
        }
        [UnityTest] public IEnumerator TitleHudPauseAndContinueAreChinese()
        {
            yield return Load();var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.AreEqual(AppPhase.Title,flow.Phase);Assert.IsNotNull(Button("开始游戏"));CheckVisible();
            Button("开始游戏").onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual(AppPhase.Playing,flow.Phase);StringAssert.Contains("瞄准中",Visible().Single(t=>t.name=="Status").text);CheckVisible();
            Button("暂停").onClick.Invoke();yield return null;Assert.AreEqual(AppPhase.Paused,flow.Phase);Assert.IsNotNull(Button("继续游戏"));CheckVisible();
            Button("继续游戏").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(AppPhase.Playing,flow.Phase);
        }
        [UnityTest] public IEnumerator PlannerModesInvalidManualAndFailedReloadShowChinese()
        {
            planner=true;yield return Load();var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.AreEqual(AppPhase.PlannerSelection,flow.Phase);CheckVisible();
            Button("切换球数方式").onClick.Invoke();Button("切换球数方式").onClick.Invoke();
            var input=UnityEngine.Object.FindFirstObjectByType<InputField>();input.text="0";
            Assert.IsTrue(Visible().Any(t=>t.text.Contains("手填球数必须")));CheckVisible();
            input.text="17";Button("开始测试本关").onClick.Invoke();yield return null;yield return null;
            Assert.AreEqual(17,flow.EntryBalls);CheckVisible();flow.RequestPause();yield return null;
            var session=flow.Session;File.WriteAllText(Path.Combine(directory,"level.json"),"broken json");flow.ReloadAndRestart();yield return null;yield return null;
            Assert.AreSame(session,flow.Session);Assert.AreEqual(AppPhase.Paused,flow.Phase);Assert.IsNotEmpty(flow.PlannerError);
            Assert.IsTrue(Visible().Any(t=>t.text.Contains("JSON 格式错误")));CheckVisible();
        }
        // Isolate result presentation using rule transitions; physical victory remains covered by campaign tests.
        static void WinForPresentation(GameFlowController flow)
        {
            while(!flow.Board.Blocks.Values.Any(b=>b.IsTarget))flow.Board.Advance();
            var target=flow.Board.Blocks.Values.Single(b=>b.IsTarget);
            while(flow.Board.Blocks.ContainsKey(target.Id))flow.Board.ApplyHit(target.Id);
            Assert.IsTrue(flow.Session.TryFire(flow.EntryBalls));flow.Session.Recall();Assert.IsTrue(flow.Session.TryBeginSettlement());flow.Session.Settle(flow.Board);flow.Campaign.CompleteStage();
            UnityEngine.Object.FindFirstObjectByType<ResultView>().SendMessage("Refresh");
        }
        [UnityTest] public IEnumerator FormalLossStageClearAndRunCompletePanelsUseChinese()
        {
            yield return Load();var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();flow.StartGame();yield return null;yield return null;
            while(flow.Session.Phase!=SessionPhase.Lost)
            {
                flow.Session.TryFire(flow.EntryBalls);flow.Session.Recall();Assert.IsTrue(flow.Session.TryBeginSettlement());flow.Session.Settle(flow.Board);
            }
            UnityEngine.Object.FindFirstObjectByType<ResultView>().SendMessage("Refresh");
            Assert.IsTrue(Visible().Any(t=>t.text=="挑战失败"));Assert.IsNotNull(Button("重试本关"));CheckVisible();
            flow.RestartRun();yield return null;yield return null;
            for(int i=0;i<3;i++)
            {
                WinForPresentation(flow);CheckVisible();
                Assert.IsTrue(Visible().Any(t=>t.text==(i<2?"本关通关":"全程通关")));
                if(i<2){Assert.IsNotNull(Button("进入下一关"));flow.NextStage();yield return null;yield return null;}
                else Assert.IsNotNull(Button("重新开始全程"));
            }
        }
        [UnityTest] public IEnumerator PlannerResultRetestsInsteadOfAdvancingFormalRun()
        {
            planner=true;yield return Load();var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();flow.QueuePlannerTest("stage-1",PlannerBallMode.Manual,17);yield return null;yield return null;
            WinForPresentation(flow);CheckVisible();Assert.IsTrue(Visible().Any(t=>t.text=="测试通关"));Assert.IsNotNull(Button("重测本关"));Assert.IsTrue(GameObject.Find("UI/ResultPanel").GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<TMP_Text>()?.text=="返回选关"));
        }
        [UnityTest] public IEnumerator InitialConfigErrorHasChineseRecoveryButton()
        {
            File.WriteAllText(Path.Combine(directory,"level.json"),"broken json");LogAssert.Expect(LogType.Error,new Regex("Initialization failed:"));
            yield return Load();var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();Assert.AreEqual(AppPhase.ConfigError,flow.Phase);
            Assert.IsNotNull(Button("重新读取配置"));Assert.IsTrue(Visible().Any(t=>t.text=="配置错误"));Assert.IsTrue(Visible().Any(t=>t.text.Contains("JSON 格式错误")));CheckVisible();
        }
    }
}
#endif
