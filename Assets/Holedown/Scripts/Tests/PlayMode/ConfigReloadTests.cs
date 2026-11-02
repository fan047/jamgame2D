#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Holedown.Tests
{
    public sealed class ConfigReloadTests
    {
        string directory;
        [SetUp] public void Setup()
        {
            directory=Path.GetFullPath("Library/Locus/tmp/config-playtests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"));File.Copy("Tests/Fixtures/legacy-level.json",Path.Combine(directory,"level.json"));
            SceneManager.sceneLoaded+=Configure;
        }
        void Configure(Scene scene,LoadSceneMode mode)
        {
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            if(!flow)return;
            var so=new SerializedObject(flow);so.FindProperty("externalConfigDirectory").stringValue=directory;so.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown] public void Cleanup() { SceneManager.sceneLoaded-=Configure;Directory.Delete(directory,true); }
        void Change(string from,string to)
        {string p=Path.Combine(directory,"settings.json");File.WriteAllText(p,File.ReadAllText(p).Replace(from,to));}
        static void ReloadClick()
        {
            var button=GameObject.Find("UI/ReloadConfig");
            ExecuteEvents.Execute(button,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator ReloadDiscardsVolleyAndAppliesNewSnapshotToAimAndBalls()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();var oldId=flow.Session.SessionId;
            Assert.AreEqual("S:demo-1 / L:demo-1",flow.ConfigurationLabel);flow.TryFire(Vector2.down);
            Change("\"ballsPerRound\": 12","\"ballsPerRound\": 7");Change("\"ballRadius\": 0.12","\"ballRadius\": 0.2");Change("demo-1","test-2");
            Assert.AreEqual(12,flow.Settings.ballsPerRound);Assert.AreEqual(0.12f,flow.Settings.ballRadius,0.001f);
            flow.QueueCommand(FlowCommand.Reload);yield return null;yield return null;
            flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.AreNotEqual(oldId,flow.Session.SessionId);Assert.AreEqual(7,flow.Settings.ballsPerRound);Assert.AreEqual("S:test-2 / L:demo-1",flow.ConfigurationLabel);
            Assert.AreEqual(0,UnityEngine.Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None).Length);
            var aim=UnityEngine.Object.FindFirstObjectByType<AimInput>();Assert.AreEqual(1.15f,aim.PreviewEnd(Vector2.down).y,0.03f);
            flow.TryFire(Vector2.down);Assert.AreEqual(6,flow.Session.Pending);
            var ball=UnityEngine.Object.FindFirstObjectByType<BallActor>();Assert.AreEqual(0.2f,ball.GetComponent<CircleCollider2D>().radius,0.001f);
            flow.RequestRecall();yield return null;
        }
        [UnityTest] public IEnumerator InvalidFileStopsPlayAndCanBeFixedWithoutRelaunchingPlayer()
        {
            Change("\"ballSpeed\": 12","\"ballSpeed\": -1");
            LogAssert.Expect(LogType.Error,new Regex("Initialization failed: settings.json.ballSpeed"));
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            StringAssert.Contains("settings.json.ballSpeed",flow.InitializationError);Assert.IsFalse(flow.TryFire(Vector2.down));
            Assert.IsTrue(GameObject.Find("UI/ResultPanel").activeSelf);
            Change("\"ballSpeed\": -1","\"ballSpeed\": 18");flow.QueueCommand(FlowCommand.Reload);yield return null;yield return null;
            flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();Assert.IsNull(flow.InitializationError);Assert.AreEqual(18,flow.Settings.ballSpeed);Assert.AreEqual(AppPhase.Title,flow.Phase);
        }
    }
}
#endif
