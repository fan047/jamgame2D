using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEditor.SceneManagement;

namespace Holedown.Tests
{
    public sealed class PlayerActionTests
    {
        [Test]
        public void NamedActionsHaveExpectedBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Assert.AreEqual("<Mouse>/leftButton", asset.FindAction("HoledownAim/Fire", true).bindings[0].path);
            Assert.AreEqual("<Keyboard>/space", asset.FindAction("HoledownVolley/Recall", true).bindings[0].path);
            Assert.AreEqual("<Keyboard>/escape", asset.FindAction("HoledownSystem/Pause", true).bindings[0].path);
        }
        [Test]
        public void SceneUiUsesProjectAssetAndEscapeHasOneConsumer()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
            try
            {
                var module = System.Array.Find(scene.GetRootGameObjects(), g => g.name == "EventSystem").GetComponent<InputSystemUIInputModule>();
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                Assert.AreEqual(asset, module.actionsAsset);
                Assert.AreEqual(asset.FindAction("UI/Click"), module.leftClick.action);
                Assert.AreEqual(asset.FindAction("UI/Submit"), module.submit.action);
                Assert.IsNull(module.cancel);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
