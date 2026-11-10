#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;

namespace Holedown.Tests
{
    // The Android playtest build switches player settings for the package and must put every one
    // of them back afterwards; the project file itself has to stay byte-identical.
    public sealed class AndroidBuildSettingsTests
    {
        const string Path_ = "ProjectSettings/ProjectSettings.asset";
        [Test] public void PlaytestSettingsAreAppliedAndFullyRestored()
        {
            byte[] before = File.ReadAllBytes(Path_);
            string signature = Editor.AndroidPlaytestBuild.SettingsSignature();
            string applied = Editor.AndroidPlaytestBuild.ApplyForTestsAndGetSignature(7);
            StringAssert.Contains("com.fan047.crystalmine.playtest", applied, "the playtest bundle id takes effect");
            StringAssert.Contains("IL2CPP", applied);
            StringAssert.Contains("ARM64", applied);
            StringAssert.Contains("Portrait", applied);
            StringAssert.Contains("|7|", applied, "the version code is the build number");
            Assert.AreEqual(signature, Editor.AndroidPlaytestBuild.SettingsSignature(), "every touched setting is restored");
            CollectionAssert.AreEqual(before, File.ReadAllBytes(Path_), "the project settings file is untouched");
        }
        [Test] public void DeclaredPlaytestConstantsMatchThePlan()
        {
            Assert.AreEqual("com.fan047.crystalmine.playtest", Editor.AndroidPlaytestBuild.BundleId);
            Assert.AreEqual(25, Editor.AndroidPlaytestBuild.MinimumApiLevel);
            Assert.AreEqual(36, Editor.AndroidPlaytestBuild.TargetApiLevel);
            Assert.AreEqual("CrystalMinePlaytest.apk", Editor.AndroidPlaytestBuild.ApkName);
        }
    }
}
#endif
