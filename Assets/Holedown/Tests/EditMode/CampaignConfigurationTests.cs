using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class CampaignConfigurationTests
    {
        string directory;GameSettings defaults;LevelDefinition fallback;
        [SetUp] public void Setup()
        {
            directory=Path.GetFullPath("Library/Locus/tmp/campaign-config/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"));File.Copy("Tests/Fixtures/growth-level.json",Path.Combine(directory,"level.json"));
            defaults=ScriptableObject.CreateInstance<GameSettings>();fallback=ScriptableObject.CreateInstance<LevelDefinition>();
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(defaults);UnityEngine.Object.DestroyImmediate(fallback);Directory.Delete(directory,true);}
        [Test] public void NewStagesAreCopiedAndEveryBoardIsIndependent()
        {
            using(var loaded=ExternalConfiguration.Load(directory,defaults,fallback))
            {
                Assert.AreEqual(3,loaded.Campaign.Stages.Count);Assert.AreEqual(3,loaded.Campaign.Stages[0].PossibleReward);Assert.AreEqual(5,loaded.Campaign.Stages[1].PossibleReward);
                var b=loaded.CreateBoard(0);b.ApplyHit(1);b.Advance();
                Assert.AreEqual(5,loaded.CreateBoard(0).Blocks[1].Health);Assert.AreEqual(0,loaded.CreateBoard(0).Blocks[1].Row);
                File.WriteAllText(Path.Combine(directory,"level.json"),"corrupt after load");
                Assert.AreEqual(11,loaded.CreateBoard(2).Blocks.Count);
            }
        }
        [Test] public void LegacyRemainsOneStageWithoutRewards()
        {
            File.Copy("Tests/Fixtures/legacy-level.json",Path.Combine(directory,"level.json"),true);
            using(var loaded=ExternalConfiguration.Load(directory,defaults,fallback)){Assert.AreEqual(1,loaded.Campaign.Stages.Count);Assert.AreEqual(0,loaded.Campaign.Stages[0].PossibleReward);}
        }
        [TestCase(false)] [TestCase(true)] public void VerticalV2ConfigurationPreservesShapeAndMaximumHealthAcrossAttempts(bool shippedExample)
        {
            var json=JObject.Parse(File.ReadAllText(Path.Combine(directory,"level.json")));
            var block=(JObject)json.SelectToken("levels[0].initialBlocks[0]");
            block["column"]=4;block["row"]=4;block["width"]=1;block["height"]=2;block["health"]=6;
            File.WriteAllText(Path.Combine(directory,"level.json"),json.ToString());
            if(shippedExample)File.Copy("Config/Examples/level-vertical.json",Path.Combine(directory,"level.json"),true);
            using(var loaded=ExternalConfiguration.Load(directory,defaults,fallback)) {
                var board=loaded.CreateBoard(0);board.ApplyHit(1);board.Advance();
                Assert.AreEqual(2,board.Blocks[1].Height);Assert.AreEqual(6,board.Blocks[1].MaxHealth);
                Assert.AreEqual(5,board.Blocks[1].Health);Assert.AreEqual(6,loaded.CreateBoard(0).Blocks[1].Health);
            }
        }
        [Test] public void SharedNegativeFixturesHaveSameContractAsEditor()
        {
            string settings=File.ReadAllText("Config/settings.json"),level=File.ReadAllText("Tests/Fixtures/growth-level.json");
            foreach(var c in JArray.Parse(File.ReadAllText("Tests/Fixtures/config-cases.json")))
            {
                var s=JObject.Parse(settings);var l=JObject.Parse(level);var root=(string)c["file"]=="settings"?s:l;
                var token=root.SelectToken((string)c["path"]);
                if((bool?)c["remove"]==true)((JProperty)token.Parent).Remove();
                else if(c["addKey"]!=null)((JObject)token)[(string)c["addKey"]]=c["value"].DeepClone();
                else token.Replace(c["value"].DeepClone());
                File.WriteAllText(Path.Combine(directory,"settings.json"),s.ToString());File.WriteAllText(Path.Combine(directory,"level.json"),l.ToString());
                var ex=Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,fallback),(string)c["name"]);
                StringAssert.Contains((string)c["error"],ex.Message,(string)c["name"]);
            }
        }
    }
}
