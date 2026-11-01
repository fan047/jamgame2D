using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Holedown.Editor
{
    public static class WindowsBuild
    {
        [MenuItem("Holedown/Build Windows Growth")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new BuildFailedException("Exit Play Mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
                throw new BuildFailedException("Windows x64 build support is missing.");
            var temp=Path.GetFullPath("Library/Locus/tmp/build-temp");
            Directory.CreateDirectory(temp);
            string oldTemp=Environment.GetEnvironmentVariable("TEMP"),oldTmp=Environment.GetEnvironmentVariable("TMP");
            try
            {
                // Burst's native linker requires a writable temporary directory on this workstation.
                // This affects only the Editor process and its build children, never system settings.
                Environment.SetEnvironmentVariable("TEMP",temp);
                Environment.SetEnvironmentVariable("TMP",temp);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes=new[]{"Assets/Scenes/SampleScene.unity"},
                    locationPathName="Builds/Windows-Growth/HoledownGrowth.exe",
                    target=BuildTarget.StandaloneWindows64,
                    options=BuildOptions.None
                });
                var summary=report.summary;
                var message=$"result={summary.result}\nerrors={summary.totalErrors}\nwarnings={summary.totalWarnings}\nbytes={summary.totalSize}\nduration={summary.totalTime}\npath={summary.outputPath}";
                File.WriteAllText("Library/Locus/tmp/windows-growth-build-final.txt",message);
                if(summary.result!=BuildResult.Succeeded) throw new BuildFailedException(message);
                PreparePlannerFiles("Builds/Windows-Growth");
                Debug.Log(message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TEMP",oldTemp);
                Environment.SetEnvironmentVariable("TMP",oldTmp);
            }
        }
        public static void PreparePlannerFiles(string outputDirectory)
        {
            string config=Path.Combine(outputDirectory,"Config"),editor=Path.Combine(outputDirectory,"PlannerEditor");
            Directory.CreateDirectory(config); Directory.CreateDirectory(editor);
            foreach(string name in new[]{"settings.json","level.json"})
            {
                string destination=Path.Combine(config,name);
                // Rebuilds must not overwrite the planner's work.
                if(!File.Exists(destination)) File.Copy(Path.Combine("Config",name),destination);
            }
            string examples=Path.Combine(config,"Examples");Directory.CreateDirectory(examples);
            string verticalExample=Path.Combine(examples,"level-vertical.json");
            if(!File.Exists(verticalExample))File.Copy("Config/Examples/level-vertical.json",verticalExample);
            foreach(string name in new[]{"index.html","config-model.js","defaults.js","editor-state.js","editor.js","styles.css"})
                File.Copy(Path.Combine("Tools/PlannerEditor",name),Path.Combine(editor,name),true);
            File.Copy("Tools/PlannerEditor/快速上手.txt",Path.Combine(outputDirectory,"策划快速上手.txt"),true);
        }
    }
}
