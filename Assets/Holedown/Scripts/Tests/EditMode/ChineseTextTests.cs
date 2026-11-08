using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class ChineseTextTests
    {
        [TestCase(SessionPhase.Initializing,"准备中")]
        [TestCase(SessionPhase.Aiming,"瞄准中")]
        [TestCase(SessionPhase.Volley,"发射中")]
        [TestCase(SessionPhase.Recalling,"召回中")]
        [TestCase(SessionPhase.Settling,"结算中")]
        [TestCase(SessionPhase.Won,"已通关")]
        [TestCase(SessionPhase.Lost,"挑战失败")]
        public void EverySessionPhaseHasChineseLabel(SessionPhase phase,string expected)
            => Assert.AreEqual(expected,ChineseText.Session(phase));

        [TestCase("manual balls: expected positive Int32 integer.","手填球数")]
        [TestCase("level.json.levels.id: tested stage 'stable-7' no longer exists.","stable-7")]
        [TestCase("level.json.levels[2].incomingRows[6].blocks[0].health: expected positive integer.","level.json.levels[2].incomingRows[6].blocks[0].health：需为")]
        [TestCase("Config requires both settings.json and level.json. Missing: D:/Game/Config/level.json","level.json：文件缺失")]
        [TestCase("Config/settings.json: required file is missing; current test preserved.","settings.json：文件缺失")]
        [TestCase("level.json: Unexpected character encountered while parsing value. Path '', line 0, position 0.","JSON 格式错误")]
        [TestCase("level.json.levels[1]: final stage cannot contain reward blocks.","最终关不能")]
        [TestCase("level.json: unknown field.","未知字段")]
        [TestCase("level.json: arbitrary unexpected diagnostic in English","读取失败")]
        public void DiagnosticsAreChineseAndRetainUsefulLocation(string original,string expected)
        {
            string result=ChineseText.Error(original);
            StringAssert.Contains(expected,result);
            StringAssert.DoesNotContain("expected",result);
            StringAssert.DoesNotContain("Unexpected character",result);
        }
        [Test] public void ConfigVersionsArePreservedWhileLabelsAreChinese()
        {
            Assert.AreEqual("球参：demo-1 / 关卡：growth-1",ChineseText.Configuration("S:demo-1 / L:growth-1"));
            Assert.AreEqual("内置配置",ChineseText.Configuration("Built-in"));
        }
        [Test] public void SceneAndBlockUseEmbeddedChineseFontWithAllStaticCharacters()
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Holedown/Fonts/NotoSansCJK/ChineseUI.asset");
            Assert.IsNotNull(font);Assert.IsNotNull(font.sourceFontFile);Assert.AreEqual(AtlasPopulationMode.Dynamic,font.atlasPopulationMode);
            var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
            try
            {
                int count=0;
                foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.AreSame(font,text.font,text.name);
                    bool runtimeOnly=text.name=="Status" || text.name=="RewardSummary" || text.name=="NextBalls" || text.name=="CompletedCount" || text.name=="Subtitle";
                    bool numeric=text.name=="StageIndex" || text.name=="RewardValue";
                    if(runtimeOnly)Assert.IsTrue(string.IsNullOrEmpty(text.text),text.name);
                    else if(numeric)Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(text.text,@"^[0-9+ /]+$"),text.name);
                    else Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(text.text,"[\\u4e00-\\u9fff]"),text.text);
                    Assert.IsFalse(text.raycastTarget,text.name);
                    string visible=System.Text.RegularExpressions.Regex.Replace(text.text,"<[^>]+>","");
                    foreach(char character in visible)if(!char.IsWhiteSpace(character))Assert.IsTrue(font.HasCharacter(character,true,true),"Missing glyph: "+character);
                    count++;
                }
                Assert.AreEqual(34,count);
                var block=AssetDatabase.LoadAssetAtPath<BlockView>("Assets/Holedown/Prefabs/Block.prefab");
                Assert.AreSame(font,block.GetComponentInChildren<TMP_Text>(true).font);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
