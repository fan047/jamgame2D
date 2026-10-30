using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class ExternalConfigurationTests
    {
        string directory;
        GameSettings defaults;
        LevelDefinition level;
        [SetUp] public void Setup()
        {
            directory=Path.GetFullPath("Library/Locus/tmp/config-tests/"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json",Path.Combine(directory,"settings.json"));
            File.Copy("Config/level.json",Path.Combine(directory,"level.json"));
            defaults=ScriptableObject.CreateInstance<GameSettings>();
            level=ScriptableObject.CreateInstance<LevelDefinition>();
            level.incomingRows.Add(new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(0,0,1,1,true)}});
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(defaults); UnityEngine.Object.DestroyImmediate(level); Directory.Delete(directory,true); }
        void Replace(string file,string oldValue,string newValue)
        { string p=Path.Combine(directory,file); File.WriteAllText(p,File.ReadAllText(p).Replace(oldValue,newValue)); }
        [Test] public void ExternalPairOverridesWithoutMutatingAssetsOrFiles()
        {
            Replace("settings.json","\"ballsPerRound\": 12","\"ballsPerRound\": 7");
            string original=File.ReadAllText(Path.Combine(directory,"level.json"));
            using(var loaded=ExternalConfiguration.Load(directory,defaults,level))
            {
                Assert.AreEqual(7,loaded.Settings.ballsPerRound); Assert.AreEqual(12,defaults.ballsPerRound);
                Assert.AreEqual(11,loaded.Board.Blocks.Count); Assert.AreEqual("S:demo-1 / L:demo-1",loaded.Label);
                loaded.Board.ApplyHit(1); loaded.Board.Advance();
            }
            Assert.AreEqual(original,File.ReadAllText(Path.Combine(directory,"level.json")));
        }
        [Test] public void MissingBothUsesBuiltInButMissingOneIsError()
        {
            File.Delete(Path.Combine(directory,"level.json"));
            StringAssert.Contains("Missing:",Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
            File.Delete(Path.Combine(directory,"settings.json"));
            using(var loaded=ExternalConfiguration.Load(directory,defaults,level)) Assert.AreEqual("Built-in",loaded.Label);
        }
        [TestCase("\"ballsPerRound\": 12","\"ballsPerRound\": 0","ballsPerRound")]
        [TestCase("\"ballsPerRound\": 12","\"ballsPerRound\": 1.5","ballsPerRound")]
        [TestCase("\"ballSpeed\": 12","\"ballSpeed\": \"12\"","ballSpeed")]
        [TestCase("\"ballSpeed\": 12","\"ballSpeed\": NaN","ballSpeed")]
        [TestCase("\"ballSpeed\": 12,","","ballSpeed")]
        [TestCase("\"ballSpeed\": 12","\"ballSpead\": 12","ballSpead")]
        [TestCase("\"ballRadius\": 0.12","\"ballRadius\": 0.5","ballRadius")]
        [TestCase("\"maxAimAngle\": 75","\"maxAimAngle\": 90","maxAimAngle")]
        [TestCase("\"schemaVersion\": 1","\"schemaVersion\": 2","schemaVersion")]
        [TestCase("\"version\": \"demo-1\"","\"version\": \"bad version\"","version")]
        public void InvalidSettingsNameTheField(string before,string after,string expected)
        {
            Replace("settings.json",before,after);
            StringAssert.Contains(expected,Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
        }
        [Test] public void InvalidLevelReportsExactBlockAndCell()
        {
            Replace("level.json","\"column\": 3, \"row\": 0, \"width\": 2, \"health\": 4","\"column\": 1, \"row\": 0, \"width\": 2, \"health\": 4");
            var error=Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level));
            StringAssert.Contains("initialBlocks[1]",error.Message); StringAssert.Contains("overlaps",error.Message);
        }
        [Test] public void DuplicateAndMalformedJsonAreRejected()
        {
            Replace("settings.json","\"ballSpeed\": 12","\"ballSpeed\": 12, \"ballSpeed\": 5");
            StringAssert.Contains("settings.json",Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
            File.WriteAllText(Path.Combine(directory,"settings.json"),"{ nope");
            StringAssert.Contains("settings.json",Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
        }
        [Test] public void ReloadReadsNewFilesAndOldSnapshotIsStable()
        {
            using(var first=ExternalConfiguration.Load(directory,defaults,level))
            {
                Replace("settings.json","\"ballSpeed\": 12","\"ballSpeed\": 18");
                using(var second=ExternalConfiguration.Load(directory,defaults,level)) Assert.AreEqual(18,second.Settings.ballSpeed);
                Assert.AreEqual(12,first.Settings.ballSpeed);
            }
        }
    }
}
