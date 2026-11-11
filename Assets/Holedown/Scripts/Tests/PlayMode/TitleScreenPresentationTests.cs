#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Holedown.Tests
{
    public sealed class TitleScreenPresentationTests
    {
        GrowthSeedScope seed;
        [SetUp] public void Setup() => seed = new GrowthSeedScope();
        [TearDown] public void Cleanup() => seed.Dispose();

        [UnityTest] public IEnumerator TitleUsesSafeContentAndDecorationsNeverBlockInput()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            yield return null; yield return new WaitForSecondsRealtime(.2f);
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            flow.SendMessage("OnApplicationFocus", true);
            // Focus restoration intentionally blocks commands across the activation frame.
            yield return null; yield return null; yield return null;
            var ui = GameObject.Find("UI").transform;
            var panel = ui.Find("StartPanel");
            var content = panel.Find("TitleContent");
            Assert.IsNotNull(content);
            var start = content.Find("Start").GetComponent<Button>();
            Assert.AreEqual(-785f, ((RectTransform)start.transform).anchoredPosition.y);
            var hint = content.Find("Controls").GetComponent<TMP_Text>();
            Assert.AreEqual(29f, hint.fontSize); Assert.IsFalse(hint.enableAutoSizing);
            Assert.AreEqual("ChineseUI", hint.font.name);
            Assert.IsFalse(panel.Find("Logo").gameObject.activeInHierarchy);
            Assert.IsFalse(panel.Find("Title").gameObject.activeInHierarchy);
            foreach (var graphic in panel.GetComponentsInChildren<Graphic>().Where(g => g.name == "CrystalGlow" || g.name.StartsWith("CrystalMote")))
                Assert.IsFalse(graphic.raycastTarget);
            Assert.AreEqual(3, panel.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("CrystalMote")));
            var audio = content.Find("AudioSettingsButton").GetComponent<Button>();
            Assert.AreEqual("设置", audio.GetComponentInChildren<TMP_Text>().text);
            Assert.IsNull(content.Find("DifficultySettingsButton"));
            Assert.AreEqual(2, content.GetComponentsInChildren<Button>().Length, "only Start and Settings remain on the title");
            audio.onClick.Invoke();
            Assert.IsTrue(ui.Find("AudioSettingsPanel").gameObject.activeInHierarchy);
            Assert.IsFalse(start.IsInteractable());
            ui.Find("AudioSettingsPanel/Card/Close").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(start.IsInteractable());
            start.onClick.Invoke(); yield return null; yield return null;
            Assert.AreEqual(AppPhase.Playing, flow.Phase);
            Assert.AreEqual(0, flow.Session.ActiveCount);
            Assert.IsFalse(Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(r => r.name == "TitleBackdrop").gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsFalse(ui.Find("TitleStartFlash").gameObject.activeSelf);
        }
    }
}
#endif
