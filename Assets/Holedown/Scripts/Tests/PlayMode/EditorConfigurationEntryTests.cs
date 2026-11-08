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
        [UnityTest] public IEnumerator DefaultSceneLoadsVerifiedProjectCampaignLayout()
        {
            float scale=Time.timeScale;
            try {
                Time.timeScale=20;
                // No sceneLoaded override: exercise the user's normal Play button entry.
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
                var flow=Object.FindFirstObjectByType<GameFlowController>();
                Assert.IsNull(flow.InitializationError);
                Assert.AreEqual(Path.GetFullPath("Config"),Path.GetFullPath(flow.ConfigurationDirectory));
                var expected=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Config/level.json"));
                Assert.AreEqual(((Newtonsoft.Json.Linq.JArray)expected["levels"]).Count,flow.Campaign.StageCount);
                StringAssert.Contains("L:"+(string)expected["version"],flow.ConfigurationLabel);
                var stage=expected["levels"][0];
                Assert.AreEqual((string)stage["id"],flow.StageId);
                var blocks=(Newtonsoft.Json.Linq.JArray)stage["initialBlocks"];
                Assert.AreEqual(blocks.Count,flow.Board.Blocks.Count);
                foreach(var actual in flow.Board.Blocks.Values)
                {
                    var source=System.Linq.Enumerable.Single(blocks,b=>(int)b["column"]==actual.Column&&(int)b["row"]==actual.Row);
                    Assert.AreEqual((int)source["health"],actual.Health);
                    Assert.AreEqual((int)source["width"],actual.Width);
                    Assert.AreEqual((int)source["height"],actual.Height);
                    Assert.AreEqual((int)source["rewardBalls"],actual.RewardBalls);
                }
                Assert.IsTrue(flow.TryFire(Vector2.down));
                Assert.AreEqual(SessionPhase.Volley,flow.Session.Phase);
                Assert.IsFalse(flow.Campaign.IsFinalStage);
            } finally {Time.timeScale=scale;}
        }
    }
}
#endif
