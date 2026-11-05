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
                Assert.AreEqual(File.ReadAllText("Tools/PlannerEditor/editor-state.js"),File.ReadAllText(Path.Combine(output,"PlannerEditor","editor-state.js")));
                StringAssert.Contains("\"schemaVersion\": 2",File.ReadAllText(Path.Combine(output,"Config","level.json")));
                Assert.IsTrue(File.Exists(Path.Combine(output,"策划快速上手.txt")));
                StringAssert.Contains("--planner-test",File.ReadAllText(Path.Combine(output,"策划测试.cmd")));
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
