#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Holedown.Tests
{
    public sealed class MidStageUiTests
    {
        string directory;bool planner;
        GameFlowController controller;
        [SetUp] public void Setup(){controller=null;planner=false;directory=Path.GetFullPath("Library/Locus/tmp/mid-ui/"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,"settings.json"),File.ReadAllText("Config/settings.json").Replace("demo-1",new string('S',32)));File.WriteAllText(Path.Combine(directory,"level.json"),File.ReadAllText("Config/level.json").Replace("growth-1",new string('L',32)));SceneManager.sceneLoaded+=Configure;}
        void Configure(Scene scene,LoadSceneMode mode){controller=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameFlowController>(true)).Single();var so=new SerializedObject(controller);so.FindProperty("externalConfigDirectory").stringValue=directory;so.FindProperty("plannerTestMode").boolValue=planner;so.ApplyModifiedPropertiesWithoutUndo();}
        [TearDown] public void Cleanup()
        {
            SceneManager.sceneLoaded-=Configure;
            try
            {
                // Always release this fixture's pause, including when a test assertion throws.
                // OnDisable owns restoration of the saved time scale and audio pause state.
                if(controller)controller.enabled=false;
            }
            finally
            {
                controller=null;
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        IEnumerator Load(){yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;var f=Object.FindFirstObjectByType<GameFlowController>();f.SendMessage("OnApplicationFocus",true);yield return null;yield return null;yield return null;}
        static void Fits(){Canvas.ForceUpdateCanvases();foreach(var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)){t.ForceMeshUpdate();Assert.IsFalse(t.isTextOverflowing,t.name+": "+t.text);if(t.GetComponentInParent<Canvas>())Assert.IsFalse(t.raycastTarget,t.name);}}
        [UnityTest] public IEnumerator FormalHudIsMinimalAndPauseOwnsReload()
        {
            yield return Load();var f=Object.FindFirstObjectByType<GameFlowController>();var ui=GameObject.Find("UI").transform;Fits();
            Assert.IsTrue(ui.Find("StartPanel/Controls").gameObject.activeInHierarchy);f.StartGame();yield return null;yield return null;
            var status=ui.Find("Status").GetComponent<TMP_Text>();StringAssert.Contains("待确认奖励",status.text);StringAssert.DoesNotContain("在场",status.text);StringAssert.DoesNotContain("配置：",status.text);Assert.IsFalse(ui.Find("ReloadConfig").gameObject.activeInHierarchy);
            Assert.AreEqual(0,ui.Find("Recall").GetComponent<RectTransform>().anchoredPosition.x);Assert.IsFalse(ui.Find("Recall").GetComponent<Button>().interactable);Fits();
            Assert.IsTrue(f.TryFire(Vector2.down));Assert.IsTrue(ui.Find("Recall").GetComponent<Button>().interactable);f.RequestPause();yield return null;
            Assert.IsTrue(ui.Find("PausePanel").gameObject.activeInHierarchy);Assert.IsFalse(status.gameObject.activeInHierarchy);Assert.IsFalse(ui.Find("Recall").gameObject.activeInHierarchy);Assert.IsFalse(ui.Find("Pause").gameObject.activeInHierarchy);Assert.IsTrue(ui.Find("PausePanel").GetComponent<Image>().raycastTarget);Fits();
            f.ContinueGame();yield return null;Assert.IsTrue(status.gameObject.activeInHierarchy);f.RequestRecall();yield return null;yield return null;
        }
        [UnityTest] public IEnumerator PlannerHudRetainsDiagnosticsAndLongVersions()
        {
            planner=true;yield return Load();var f=Object.FindFirstObjectByType<GameFlowController>();Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<TMP_Text>()?.text=="开始测试本关").onClick.Invoke();yield return null;yield return null;
            var status=GameObject.Find("UI/Status").GetComponent<TMP_Text>();StringAssert.Contains("在场",status.text);StringAssert.Contains(new string('S',32),status.text);StringAssert.Contains(new string('L',32),status.text);Fits();
            f.RequestPause();yield return null;Fits();Assert.IsTrue(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<TMP_Text>()?.text=="重载当前关"));
        }
    }
}
#endif
