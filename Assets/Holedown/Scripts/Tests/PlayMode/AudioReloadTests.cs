#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Holedown.Tests
{
    public sealed class AudioReloadTests
    {
        string directory;bool planner;GameFlowController controller;
        [SetUp] public void Setup(){planner=false;directory=Path.GetFullPath("Library/Locus/tmp/audio-reload/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);foreach(string file in new[]{"settings.json","level.json"})File.Copy("Config/"+file,Path.Combine(directory,file));SceneManager.sceneLoaded+=Configure;}
        void Configure(Scene scene,LoadSceneMode mode){controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameFlowController>(true)).Single();var so=new SerializedObject(controller);so.FindProperty("externalConfigDirectory").stringValue=directory;so.FindProperty("plannerTestMode").boolValue=planner;so.ApplyModifiedPropertiesWithoutUndo();}
        [TearDown] public void Cleanup(){SceneManager.sceneLoaded-=Configure;try{if(controller)controller.enabled=false;}finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}}
        IEnumerator Load(){yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;controller.SendMessage("OnApplicationFocus",true);yield return null;yield return null;yield return null;}
        void Write(string json)=>File.WriteAllText(Path.Combine(directory,"audio.json"),json);
        [UnityTest] public IEnumerator ReloadDuringPausedVolleyIsTransactionalAndDoesNotResetAttempt()
        {
            planner=true;Write("{\"schemaVersion\":1,\"version\":\"loud\",\"cues\":{\"volley_launch\":{\"volumePercent\":150}}}");yield return Load();
            var feedback=Object.FindFirstObjectByType<FeedbackPlayer>();Assert.AreEqual("loud",feedback.AudioVersion);controller.QueuePlannerTest(controller.Stages[0].Id,PlannerBallMode.Manual,17);yield return null;yield return null;
            controller.Settings.launchInterval=.5f;Assert.IsTrue(controller.TryFire(Vector2.down));Assert.AreEqual(AudioCue.VolleyLaunch,feedback.LastCue);Assert.AreEqual(.975f,feedback.LastCueGain,.0001f);yield return null;
            controller.RequestPause();yield return null;var session=controller.Session;var board=controller.Board;var campaign=controller.Campaign;int pending=session.Pending,active=session.ActiveCount,round=session.Round,bonus=campaign.PendingBonus;var balls=Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None);var positions=balls.Select(b=>b.transform.position).ToArray();
            var original=feedback.ConfiguredClip(AudioCue.BlockHit);string wav=Convert.ToBase64String(File.ReadAllBytes("Assets/Holedown/Audio/block_break.wav"));
            Write("{\"schemaVersion\":1,\"version\":\"custom\",\"cues\":{\"block_hit\":{\"wavBase64\":\""+wav+"\"},\"volley_launch\":{\"muted\":true}}}");Assert.IsTrue(feedback.ReloadAudio());var replacement=feedback.ConfiguredClip(AudioCue.BlockHit);Assert.AreNotSame(original,replacement);Assert.AreEqual("custom",feedback.AudioVersion);
            Write("broken json");Assert.IsFalse(feedback.ReloadAudio());Assert.AreSame(replacement,feedback.ConfiguredClip(AudioCue.BlockHit));Assert.AreEqual("custom",feedback.AudioVersion);StringAssert.Contains("audio.json",feedback.AudioError);
            yield return null;Assert.AreSame(session,controller.Session);Assert.AreSame(board,controller.Board);Assert.AreSame(campaign,controller.Campaign);Assert.AreEqual(pending,session.Pending);Assert.AreEqual(active,session.ActiveCount);Assert.AreEqual(round,session.Round);Assert.AreEqual(bonus,campaign.PendingBonus);Assert.AreEqual(17,controller.EntryBalls);for(int i=0;i<balls.Length;i++)Assert.AreEqual(positions[i],balls[i].transform.position);
            File.Delete(Path.Combine(directory,"audio.json"));Assert.IsTrue(feedback.ReloadAudio());Assert.AreEqual("内置",feedback.AudioVersion);Assert.AreSame(original,feedback.ConfiguredClip(AudioCue.BlockHit));yield return null;Assert.IsFalse(replacement);Assert.IsTrue(original);Assert.AreEqual(AppPhase.Paused,controller.Phase);
            controller.ContinueGame();yield return null;Assert.IsFalse(feedback.ReloadAudio());controller.RequestRecall();yield return null;yield return null;
        }
        IEnumerator InvalidStartup(bool testMode)
        {
            planner=testMode;Write("broken json");yield return Load();var feedback=Object.FindFirstObjectByType<FeedbackPlayer>();Assert.AreEqual("内置",feedback.AudioVersion);Assert.IsNotEmpty(feedback.AudioError);Assert.IsNull(controller.InitializationError);Assert.AreEqual(testMode?AppPhase.PlannerSelection:AppPhase.Title,controller.Phase);Assert.IsTrue(GameObject.Find("UI/AudioNotice"));
        }
        [UnityTest] public IEnumerator InvalidAudioDoesNotBlockNormalGame(){yield return InvalidStartup(false);}
        [UnityTest] public IEnumerator InvalidAudioDoesNotBlockPlannerSelection(){yield return InvalidStartup(true);}
        [UnityTest] public IEnumerator MutedAndZeroVolumeCuesDoNotPlayOrChangeDefaultClip()
        {
            Write("{\"schemaVersion\":1,\"version\":\"quiet\",\"cues\":{\"volley_launch\":{\"muted\":true},\"manual_recall\":{\"volumePercent\":0}}}");yield return Load();var feedback=Object.FindFirstObjectByType<FeedbackPlayer>();controller.StartGame();yield return null;yield return null;var last=feedback.LastCue;Assert.IsTrue(controller.TryFire(Vector2.down));Assert.AreEqual(last,feedback.LastCue);controller.RequestRecall();Assert.AreEqual(last,feedback.LastCue);yield return null;yield return null;Assert.IsNotNull(feedback.ConfiguredClip(AudioCue.VolleyLaunch));
        }
    }
}
#endif
