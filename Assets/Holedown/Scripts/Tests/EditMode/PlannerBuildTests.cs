using System;
using System.IO;
using NUnit.Framework;

namespace Holedown.Tests
{
    public sealed class PlannerBuildTests
    {
        [Test] public void StageCreatesEditableDeliveryAndPreservesExistingPlannerValues()
        {
            string output=Path.GetFullPath("Library/Locus/tmp/planner-build-tests/"+Guid.NewGuid().ToString("N"));
            try
            {
                Holedown.Editor.WindowsBuild.PreparePlannerFiles(output);
                foreach(string name in new[]{"settings.json","level.json"}) Assert.IsTrue(File.Exists(Path.Combine(output,"Config",name)));
                Assert.IsTrue(File.Exists(Path.Combine(output,"PlannerEditor","index.html")));
                foreach(string name in new[]{"index.html","config-model.js","defaults.js","editor-state.js","editor.js","styles.css","audio-model.js","audio-defaults.js","audio-editor.js"})
                    Assert.AreEqual(File.ReadAllText(Path.Combine("Tools/PlannerEditor",name)),File.ReadAllText(Path.Combine(output,"PlannerEditor",name)),name);
                StringAssert.Contains("整关总览",File.ReadAllText(Path.Combine(output,"策划快速上手.txt")));
                StringAssert.Contains("跳到布局",File.ReadAllText(Path.Combine(output,"策划快速上手.txt")));
                StringAssert.Contains("\"schemaVersion\": 2",File.ReadAllText(Path.Combine(output,"Config","level.json")));
                Assert.IsTrue(File.Exists(Path.Combine(output,"策划快速上手.txt")));
                StringAssert.Contains("SIL OPEN FONT LICENSE",File.ReadAllText(Path.Combine(output,"ThirdPartyNotices","NotoSansCJK-OFL.txt")));
                Assert.IsTrue(File.Exists(Path.Combine(output,"ThirdPartyNotices","NotoSansCJK-Source.txt")));
                StringAssert.Contains("--planner-test",File.ReadAllText(Path.Combine(output,"策划测试.cmd")));
                StringAssert.Contains("音效",File.ReadAllText(Path.Combine(output,"策划快速上手.txt")));
                string example=Path.Combine(output,"Config","Examples","level-vertical.json");
                Assert.AreEqual(File.ReadAllText("Config/Examples/level-vertical.json"),File.ReadAllText(example));
                File.WriteAllText(example,"custom example must survive rebuild");
                string settings=Path.Combine(output,"Config","settings.json");
                File.WriteAllText(settings,"planner edits must survive rebuild");
                File.WriteAllText(Path.Combine(output,"PlannerEditor","editor.js"),"old tool");
                Holedown.Editor.WindowsBuild.PreparePlannerFiles(output);
                Assert.AreEqual("planner edits must survive rebuild",File.ReadAllText(settings));
                Assert.AreEqual("custom example must survive rebuild",File.ReadAllText(example));
                Assert.AreEqual(File.ReadAllText("Tools/PlannerEditor/editor.js"),File.ReadAllText(Path.Combine(output,"PlannerEditor","editor.js")));
                Assert.IsFalse(Directory.Exists(Path.Combine(output,"PlannerEditor","tests")));
            }
            finally { if(Directory.Exists(output))Directory.Delete(output,true); }
        }
    }
}
