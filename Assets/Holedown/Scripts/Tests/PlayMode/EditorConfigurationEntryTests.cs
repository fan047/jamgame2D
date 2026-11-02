#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class EditorConfigurationEntryTests
    {
        [UnityTest] public IEnumerator DefaultSceneLoadsProjectCampaignAndOffersNextStage()
        {
            float scale=Time.timeScale;
            try {
                Time.timeScale=20;
                // No sceneLoaded override: exercise the user's normal Play button entry.
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
                var flow=Object.FindFirstObjectByType<GameFlowController>();
                Assert.IsNull(flow.InitializationError);
                Assert.AreEqual(Path.GetFullPath("Config"),Path.GetFullPath(flow.ConfigurationDirectory));
                Assert.AreEqual(3,flow.Campaign.StageCount);
                StringAssert.Contains("L:growth-1",flow.ConfigurationLabel);
                for(int shot=0;shot<5;shot++) {
                    Assert.IsTrue(flow.TryFire(new Vector2(-0.5f,-6.5f)));
                    float deadline=Time.realtimeSinceStartup+15;
                    while(flow.Session.Phase==SessionPhase.Volley||flow.Session.Phase==SessionPhase.Recalling) {
                        Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;
                    }
                }
                Assert.AreEqual(SessionPhase.Won,flow.Session.Phase);
                Assert.IsFalse(flow.Campaign.IsFinalStage);
                var button=GameObject.Find("UI/ResultPanel/Advance");Assert.IsNotNull(button);Assert.IsTrue(button.activeInHierarchy);
                Assert.AreEqual("Next Stage",button.GetComponentInChildren<TMPro.TMP_Text>().text);
                flow.NextStage();yield return null;
                Assert.AreEqual(1,flow.Campaign.StageIndex);Assert.AreEqual(15,flow.Campaign.EntryBalls);
            } finally {Time.timeScale=scale;}
        }
    }
}
#endif
