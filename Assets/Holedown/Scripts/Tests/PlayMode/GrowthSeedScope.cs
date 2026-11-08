#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Holedown.Tests
{
    // Tests of fixed shot routes/counts must not depend on the planner's editable production layouts.
    internal sealed class GrowthSeedScope : IDisposable
    {
        readonly string directory;
        public GrowthSeedScope()
        {
            directory=Path.GetFullPath("Library/Locus/tmp/growth-seed/"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"));
            File.Copy("Tests/Fixtures/growth-level.json",Path.Combine(directory,"level.json"));
            SceneManager.sceneLoaded+=Configure;
            BoardView.SkipAdvanceForTests=true;
        }
        void Configure(Scene scene,LoadSceneMode mode)
        {
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            if(!flow)return;
            var serialized=new SerializedObject(flow);
            serialized.FindProperty("externalConfigDirectory").stringValue=directory;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        public void Dispose(){SceneManager.sceneLoaded-=Configure;Directory.Delete(directory,true);BoardView.SkipAdvanceForTests=false;}
    }
}
#endif
