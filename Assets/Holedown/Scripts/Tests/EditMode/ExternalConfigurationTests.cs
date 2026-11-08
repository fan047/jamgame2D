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
            File.Copy("Tests/Fixtures/legacy-level.json",Path.Combine(directory,"level.json"));
            defaults=ScriptableObject.CreateInstance<GameSettings>();
            level=ScriptableObject.CreateInstance<LevelDefinition>();
            level.incomingRows.Add(new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(0,0,1,1,true)}});
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(defaults); UnityEngine.Object.DestroyImmediate(level); Directory.Delete(directory,true); }
        void Replace(string file,string oldValue,string newValue)
        { string p=Path.Combine(directory,file); File.WriteAllText(p,File.ReadAllText(p).Replace(oldValue,newValue)); }
        void SetValue(string file,string key,string literal)
        { string p=Path.Combine(directory,file); File.WriteAllText(p,System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(p),"(\""+key+"\"\\s*:\\s*)[^,\\n}]*",m=>m.Groups[1].Value+literal,System.Text.RegularExpressions.RegexOptions.None,System.TimeSpan.FromSeconds(2))); }
        void RemoveKey(string file,string key)
        { string p=Path.Combine(directory,file); File.WriteAllText(p,System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(p),",?\\s*\""+key+"\"\\s*:\\s*[^,\\n}]*","",System.Text.RegularExpressions.RegexOptions.None,System.TimeSpan.FromSeconds(2))); }
        static string Version(string file)
            => System.Text.RegularExpressions.Regex.Match(File.ReadAllText(file),"\\\"version\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"").Groups[1].Value;
        [Test] public void ExternalPairOverridesWithoutMutatingAssetsOrFiles()
        {
            Replace("settings.json","\"ballsPerRound\": 12","\"ballsPerRound\": 7");
            string original=File.ReadAllText(Path.Combine(directory,"level.json"));
            using(var loaded=ExternalConfiguration.Load(directory,defaults,level))
            {
                Assert.AreEqual(7,loaded.Settings.ballsPerRound); Assert.AreEqual(12,defaults.ballsPerRound);
                Assert.AreEqual(11,loaded.Board.Blocks.Count);
                Assert.AreEqual("S:"+Version(Path.Combine(directory,"settings.json"))+" / L:"+Version(Path.Combine(directory,"level.json")),loaded.Label);
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
        [TestCase("ballsPerRound","0","ballsPerRound")]
        [TestCase("ballsPerRound","-1","ballsPerRound")]
        [TestCase("ballSpeed","\"12\"","ballSpeed")]
        [TestCase("ballSpeed","NaN","ballSpeed")]
        [TestCase("ballRadius","0.5","ballRadius")]
        [TestCase("maxAimAngle","90","maxAimAngle")]
        [TestCase("schemaVersion","2","schemaVersion")]
        [TestCase("version","\"bad version\"","version")]
        public void InvalidSettingsNameTheField(string key,string literal,string expected)
        {
            SetValue("settings.json",key,literal);
            StringAssert.Contains(expected,Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
        }
        // Fractional and missing values are covered separately because they need structural edits.
        [Test] public void FractionalOrMissingSettingsAreRejected()
        {
            SetValue("settings.json","ballsPerRound","1.5");
            StringAssert.Contains("ballsPerRound",Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
            SetValue("settings.json","ballsPerRound","12");
            RemoveKey("settings.json","ballSpeed");
            StringAssert.Contains("ballSpeed",Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
        }
        [Test] public void UnknownSettingsKeyIsRejected()
        {
            Replace("settings.json","\"version\"","\"ballSpead\": 12,\n  \"version\"");
            StringAssert.Contains("ballSpead",Assert.Throws<ArgumentException>(()=>ExternalConfiguration.Load(directory,defaults,level)).Message);
        }
        [Test] public void InvalidLevelReportsExactBlockAndCell()
        {
            Replace("level.json","\"column\": 3","\"column\": 1");
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
