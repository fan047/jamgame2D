#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    // The packaged Android configuration: the project's own Config texts go through the same
    // strict parser, a broken or short bundle is an error, and there is no built-in fallback.
    public sealed class BundledConfigurationTests
    {
        static string Settings => File.ReadAllText("Config/settings.json");
        static string Level => File.ReadAllText("Config/level.json");
        static GameSettings Template()
        {
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            return settings;
        }
        static LevelDefinition LevelTemplate() => ScriptableObject.CreateInstance<LevelDefinition>();

        [Test] public void ShippedConfigTextsParseIntoTheFullCampaign()
        {
            var settings = Template(); var level = LevelTemplate();
            try
            {
                using var loaded = ExternalConfiguration.Parse(Settings, Level, settings, level, "Bundled");
                Assert.AreEqual(9, loaded.Campaign.Stages.Count, "the package always carries nine stages");
                Assert.AreEqual("stage-1", loaded.Campaign.Stages[0].Id);
                Assert.AreEqual("stage-9", loaded.Campaign.Stages[8].Id);
                Assert.AreEqual(12, loaded.Settings.ballsPerRound);
                Assert.AreEqual(10f, loaded.Settings.ballSpeedRampSeconds, 0.0001f, "the ramp duration keeps its default");
            }
            finally { Object.DestroyImmediate(settings); Object.DestroyImmediate(level); }
        }
        [Test] public void MissingEitherFileIsAnErrorInsteadOfASingleStageFallback()
        {
            var settings = Template(); var level = LevelTemplate();
            try
            {
                var missingSettings = Assert.Throws<System.ArgumentException>(() => ExternalConfiguration.Parse(null, Level, settings, level, "Bundled"));
                StringAssert.Contains("settings.json", missingSettings.Message);
                var missingLevel = Assert.Throws<System.ArgumentException>(() => ExternalConfiguration.Parse(Settings, "", settings, level, "Bundled"));
                StringAssert.Contains("level.json", missingLevel.Message);
            }
            finally { Object.DestroyImmediate(settings); Object.DestroyImmediate(level); }
        }
        [Test] public void BrokenXmlOrUnknownFieldsStillFail()
        {
            var settings = Template(); var level = LevelTemplate();
            try
            {
                Assert.Throws<System.ArgumentException>(() => ExternalConfiguration.Parse("{", Level, settings, level, "Bundled"));
                Assert.Throws<System.ArgumentException>(() => ExternalConfiguration.Parse(Settings, "{\"schemaVersion\":2,\"version\":\"x\",\"levels\":[]}", settings, level, "Bundled"));
                Assert.Throws<System.ArgumentException>(() => ExternalConfiguration.Parse(
                    Settings.Replace("\"version\"", "\"unknownField\":1,\"version\""), Level, settings, level, "Bundled"));
                Assert.Throws<System.ArgumentException>(() => ExternalConfiguration.Parse(
                    Settings.Replace("\"ballSpeed\": 12", "\"ballSpeed\": -4"), Level, settings, level, "Bundled"));
            }
            finally { Object.DestroyImmediate(settings); Object.DestroyImmediate(level); }
        }
        [Test] public void NineStagesAreRequiredForThePackagedBuild()
        {
            Assert.AreEqual(9, GameFlowController.BundledStageCount);
            var settings = Template(); var level = LevelTemplate();
            try
            {
                using var loaded = ExternalConfiguration.Parse(Settings, Level, settings, level, "Bundled");
                Assert.AreEqual(GameFlowController.BundledStageCount, loaded.Campaign.Stages.Count);
            }
            finally { Object.DestroyImmediate(settings); Object.DestroyImmediate(level); }
        }
        [Test] public void BundledFileNamesAndFolderAreFixed()
        {
            Assert.AreEqual("Config", BundledConfiguration.Folder);
            Assert.AreEqual("settings.json", BundledConfiguration.SettingsFile);
            Assert.AreEqual("level.json", BundledConfiguration.LevelFile);
        }
    }
}
#endif
