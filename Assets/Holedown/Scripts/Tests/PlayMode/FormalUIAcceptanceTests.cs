#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Holedown.Tests
{
    // Presentation fixtures exercise real views/commands. Direct rule setup for
    // result screenshots is not a claim of completing the physical campaign.
    public sealed class FormalUIAcceptanceTests
    {
        string directory;bool planner;GameFlowController flow;
        const string Evidence="Library/Locus/tmp/ui-formal";
        [SetUp] public void Setup(){planner=false;flow=null;directory=Path.GetFullPath("Library/Locus/tmp/formal-ui/"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);foreach(var n in new[]{"settings","level"})File.Copy("Config/"+n+".json",Path.Combine(directory,n+".json"));SceneManager.sceneLoaded+=Configure;}
        void Configure(Scene scene,LoadSceneMode mode){flow=Object.FindFirstObjectByType<GameFlowController>();var so=new SerializedObject(flow);so.FindProperty("externalConfigDirectory").stringValue=directory;so.FindProperty("plannerTestMode").boolValue=planner;so.ApplyModifiedPropertiesWithoutUndo();}
        [TearDown] public void Cleanup(){SceneManager.sceneLoaded-=Configure;try{if(flow)flow.enabled=false;}finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}}
        IEnumerator Load(){yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;flow.SendMessage("OnApplicationFocus",true);yield return null;yield return null;yield return null;}
        static Button Button(string text)=>Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<TMP_Text>()?.text==text);
        static Rect Bounds(Component component){var corners=new Vector3[4];((RectTransform)component.transform).GetWorldCorners(corners);return new Rect(corners[0],corners[2]-corners[0]);}
        Transform UI=>GameObject.Find("UI").transform;
        TMP_Text Text(string path)=>UI.Find(path).GetComponent<TMP_Text>();
        void Refresh(){Object.FindFirstObjectByType<HudView>().SendMessage("Refresh");Object.FindFirstObjectByType<ResultView>().SendMessage("Refresh");var tool=Object.FindFirstObjectByType<PlannerTestView>();if(tool)tool.SendMessage("Refresh");}
        static void Capture(string name)
        {
            Directory.CreateDirectory(Evidence);var cam=Camera.main;
            foreach(var size in new[]{new Vector2Int(540,960),new Vector2Int(1280,720)})
            {
                var target=cam.targetTexture;var aspect=cam.aspect;var ortho=cam.orthographicSize;var active=RenderTexture.active;var rt=new RenderTexture(size.x,size.y,24);rt.Create();Texture2D tex=null;
                try{cam.targetTexture=rt;cam.aspect=(float)size.x/size.y;cam.GetComponent<BoardCamera>().SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,size.x,size.y),0,0);tex.Apply();File.WriteAllBytes($"{Evidence}/{name}-{size.x}x{size.y}.png",tex.EncodeToPNG());}
                finally{cam.targetTexture=target;cam.aspect=aspect;cam.orthographicSize=ortho;RenderTexture.active=active;if(tex)Object.Destroy(tex);rt.Release();Object.Destroy(rt);}
            }
        }
        [UnityTest] public IEnumerator TitleHudRecallRewardAndExplicitResume()
        {
            yield return Load();yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(GameObject.Find("Game/Launcher").GetComponent<SpriteRenderer>().enabled);Assert.IsFalse(GameObject.Find("Game/DangerLine").GetComponent<LineRenderer>().enabled);
            Assert.IsFalse(UI.Find("StartPanel/Subtitle").gameObject.activeInHierarchy);Capture("title");
            Button("开始游戏").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase);Assert.AreEqual(0,flow.Session.ActiveCount);
            Assert.IsTrue(GameObject.Find("Game/Launcher").GetComponent<SpriteRenderer>().enabled);Assert.IsFalse(UI.Find("HudHeader/PendingReward").gameObject.activeInHierarchy);            Assert.IsFalse(Button("召回").interactable);Capture("hud-zero");
            // Borderless recall: the hit graphic is invisible, the backing is a single
            // fading quad, and both labels stay inside the softer central pad.
            var scrim=UI.Find("RecallScrim").GetComponent<SoftScrim>();
            Assert.IsNotNull(scrim);Assert.IsFalse(scrim.raycastTarget);Assert.Greater(scrim.BottomAlpha,scrim.TopAlpha);
            Assert.AreEqual(0f,Button("召回").targetGraphic.color.a,"recall draws no box");
            Assert.IsTrue(Bounds(Text("RecallHint").transform).width <= Bounds(scrim.transform).width);
            Assert.IsTrue(Bounds(scrim.transform).Contains(Bounds(Button("召回").transform).min));
            Assert.IsTrue(flow.TryFire(Vector2.down));Assert.IsTrue(Button("召回").interactable);StringAssert.Contains(flow.EntryBalls.ToString(),Text("HudHeader/BallCount").text);Capture("volley");
            Button("召回").onClick.Invoke();yield return null;yield return null;Assert.IsFalse(Button("召回").interactable);
            foreach(int bonus in new[]{3,5}){Assert.IsTrue(flow.Campaign.RecordHit(900+bonus,new HitResult(true,0,true,false,bonus)));Refresh();yield return null;Assert.AreEqual(Vector3.one,Text("HudHeader/BallCount").transform.localScale);Assert.AreEqual(Vector3.one,Text("HudHeader/StageIndex").transform.localScale);StringAssert.Contains("+"+flow.Campaign.PendingBonus,Text("HudHeader/PendingReward/RewardValue").text);}
            yield return new WaitForSeconds(.2f);Capture("hud-reward");
            flow.SendMessage("OnApplicationFocus",false);yield return null;Assert.AreEqual(AppPhase.Paused,flow.Phase);flow.SendMessage("OnApplicationFocus",true);yield return null;yield return null;Assert.AreEqual(AppPhase.Paused,flow.Phase);yield return new WaitForSecondsRealtime(.2f);Capture("pause");
            var aim=Object.FindFirstObjectByType<AimInput>();Assert.IsTrue(aim.IsPointerOverUI(Camera.main.WorldToScreenPoint(new Vector3(4,4,0))));Assert.IsFalse(UI.Find("HudHeader").gameObject.activeInHierarchy);
            Assert.AreEqual(MineUIStyle.Ink,Button("继续游戏").GetComponentInChildren<TMP_Text>().color);
            Button("继续游戏").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(AppPhase.Playing,flow.Phase);Assert.AreEqual(0,flow.Session.ActiveCount);
            flow.RequestPause();yield return null;Button("重载并重开").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(0,flow.Campaign.StageIndex);Assert.AreEqual(0,flow.Campaign.PendingBonus);Assert.IsFalse(UI.Find("HudHeader/PendingReward").gameObject.activeInHierarchy);
        }
        [UnityTest] public IEnumerator LongChineseErrorScrollKeepsRecoveryVisible()
        {
            yield return Load();flow.StartGame();yield return null;yield return null;
            string longError="level.json."+string.Concat(Enumerable.Repeat("第4关第2列方块与其他方块重叠请修正后重新导出。",45))+": overlaps another block";
            typeof(GameFlowController).GetProperty("InitializationError").SetValue(flow,longError);Refresh();yield return null;yield return new WaitForSecondsRealtime(.2f);
            var body=UI.Find("ResultPanel/Card/BodyScroll").GetComponent<ScrollRect>();Assert.Greater(body.content.rect.height,body.viewport.rect.height*2);Assert.IsFalse(body.content.GetComponent<TMP_Text>().enableAutoSizing);
            var recovery=Button("重新读取配置");Assert.AreEqual(MineUIStyle.Ivory,recovery.targetGraphic.color);var position=recovery.transform.position;Capture("error-top");
            body.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-80)});yield return null;Assert.Less(body.verticalNormalizedPosition,.95f);Assert.AreEqual(position,recovery.transform.position);Capture("error-scrolled");
            recovery.onClick.Invoke();yield return null;yield return null;Assert.IsNull(flow.InitializationError);Assert.AreEqual(AppPhase.Playing,flow.Phase);
        }
        void Win()
        {
            while(!flow.Board.Blocks.Values.Any(b=>b.IsTarget))flow.Board.Advance();var target=flow.Board.Blocks.Values.Single(b=>b.IsTarget);
            while(flow.Board.Blocks.ContainsKey(target.Id))flow.Board.ApplyHit(target.Id);
            Assert.IsTrue(flow.Session.TryFire(flow.EntryBalls));flow.Session.Recall();Assert.IsTrue(flow.Session.TryBeginSettlement());flow.Session.Settle(flow.Board);flow.Campaign.CompleteStage();Refresh();
        }
        [UnityTest] public IEnumerator ThreeResultVariantsUseFixedActions()
        {
            yield return Load();flow.StartGame();yield return null;yield return null;
            while(flow.Session.Phase!=SessionPhase.Lost){flow.Session.TryFire(flow.EntryBalls);flow.Session.Recall();Assert.IsTrue(flow.Session.TryBeginSettlement());flow.Session.Settle(flow.Board);}flow.Campaign.FailStage();Refresh();yield return new WaitForSecondsRealtime(.2f);Capture("loss");
            Button("重试本关").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase);
            for(int i=0;i<flow.Campaign.StageCount;i++){Win();yield return new WaitForSecondsRealtime(.2f);if(!flow.Campaign.IsFinalStage){if(i==0)Capture("stage-clear");Button("进入下一关").onClick.Invoke();yield return null;yield return null;}else{Capture("run-complete");Assert.IsFalse(UI.Find("ResultPanel/Card/Advance").gameObject.activeInHierarchy);}}
            Button("重新开始全程").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(0,flow.Campaign.StageIndex);
        }
        [UnityTest] public IEnumerator MaximumPlannerBallCountStillFitsWithoutShrinkingTheHud()
        {
            planner=true;yield return Load();Button("切换球数方式").onClick.Invoke();Button("切换球数方式").onClick.Invoke();Object.FindFirstObjectByType<InputField>().text=int.MaxValue.ToString();Button("开始测试本关").onClick.Invoke();yield return null;yield return null;
            var count=Text("HudHeader/BallCount");count.ForceMeshUpdate();Assert.AreEqual(int.MaxValue,flow.EntryBalls);Assert.IsFalse(count.isTextOverflowing);Assert.AreEqual(Vector3.one,count.transform.localScale);Assert.AreEqual(Vector3.one,Text("HudHeader/StageIndex").transform.localScale);
        }
        [UnityTest] public IEnumerator PlannerControlsAndAudioFailureUseSharedStyle()
        {
            planner=true;
            foreach(var n in new[]{"settings","level"}){var p=Path.Combine(directory,n+".json");var json=File.ReadAllText(p);json=System.Text.RegularExpressions.Regex.Replace(json,"\"version\"\\s*:\\s*\"[^\"]+\"","\"version\": \""+new string(n[0],32)+"\"",System.Text.RegularExpressions.RegexOptions.None);File.WriteAllText(p,json);}
            yield return Load();Capture("planner-selection");var start=Button("开始测试本关");Assert.IsInstanceOf<CutCornerImage>(start.targetGraphic);start.onClick.Invoke();yield return null;yield return null;
            Assert.IsTrue(UI.Find("Status").gameObject.activeInHierarchy);StringAssert.Contains(new string('s',32),Text("Status").text);
            Assert.IsFalse(Bounds(Button("返回选关")).Overlaps(Bounds(Text("HudHeader/BallCount"))),"planner shortcut must not cover the main ball count");
            Assert.IsFalse(Bounds(Button("返回选关")).Overlaps(Bounds(Button("召回"))),"bottom actions remain separate");Capture("planner-hud");
            flow.RequestPause();yield return new WaitForSecondsRealtime(.2f);var session=flow.Session;File.WriteAllText(Path.Combine(directory,"audio.json"),"broken json");Button("重载音效").onClick.Invoke();yield return null;Assert.IsNotEmpty(Object.FindFirstObjectByType<FeedbackPlayer>().AudioError);Assert.AreSame(session,flow.Session);Capture("planner-audio-error");
            File.Delete(Path.Combine(directory,"audio.json"));Button("重载音效").onClick.Invoke();yield return null;Assert.IsNull(Object.FindFirstObjectByType<FeedbackPlayer>().AudioError);
            Button("重载当前关").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(AppPhase.Playing,flow.Phase);Assert.AreNotSame(session,flow.Session);Win();yield return new WaitForSecondsRealtime(.2f);Capture("planner-result");Button("返回选关").onClick.Invoke();yield return null;yield return null;Assert.AreEqual(AppPhase.PlannerSelection,flow.Phase);
        }
    }
}
#endif
