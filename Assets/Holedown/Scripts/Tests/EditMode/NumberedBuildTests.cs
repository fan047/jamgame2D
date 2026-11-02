using System;
using System.IO;
using NUnit.Framework;

namespace Holedown.Tests
{
    public sealed class NumberedBuildTests
    {
        string root;
        [SetUp] public void Setup(){root=Path.GetFullPath("Library/Locus/tmp/numbered-build-tests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
        [TearDown] public void Cleanup(){Directory.Delete(root,true);}
        [Test] public void EveryBuildGetsNewTwoDigitFolderAndPreviousContentSurvives()
        {
            var first=Holedown.Editor.WindowsBuild.CreateBuildDirectory(root);Assert.AreEqual("01",Path.GetFileName(first));File.WriteAllText(Path.Combine(first,"planner.txt"),"keep");
            var second=Holedown.Editor.WindowsBuild.CreateBuildDirectory(root);Assert.AreEqual("02",Path.GetFileName(second));Assert.AreNotEqual(first,second);Assert.AreEqual("keep",File.ReadAllText(Path.Combine(first,"planner.txt")));
        }
        [Test] public void HighestExistingNumberWinsWithoutReusingGapsOrTouchingLegacyBuilds()
        {
            foreach(string name in new[]{"01","09","Windows","Windows-Growth"})Directory.CreateDirectory(Path.Combine(root,name));
            File.WriteAllText(Path.Combine(root,"Windows","keep.txt"),"legacy");
            Assert.AreEqual("10",Path.GetFileName(Holedown.Editor.WindowsBuild.CreateBuildDirectory(root)));
            Assert.AreEqual("legacy",File.ReadAllText(Path.Combine(root,"Windows","keep.txt")));
        }
        [Test] public void NumberedFilesAreNotOverwrittenAndNumberingContinuesPastNinetyNine()
        {
            File.WriteAllText(Path.Combine(root,"99"),"file");
            Assert.AreEqual("100",Path.GetFileName(Holedown.Editor.WindowsBuild.CreateBuildDirectory(root)));Assert.AreEqual("file",File.ReadAllText(Path.Combine(root,"99")));
        }
    }
}
