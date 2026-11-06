using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Holedown.Tests
{
    public sealed class PresentationTests
    {
        GrowthSeedScope seed;
        [SetUp] public void Setup() { seed = new GrowthSeedScope(); }
        [TearDown] public void Cleanup() { seed.Dispose(); }
        [UnityTest] public IEnumerator RecallUIBlocksAimAndInvokesOnlyRecall()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            Canvas.ForceUpdateCanvases();
            var flow=Object.FindFirstObjectByType<GameFlowController>();var aim=Object.FindFirstObjectByType<AimInput>();
            var button=GameObject.Find("UI/Recall").GetComponent<Button>();
            var screen=(Vector2)Camera.main.WorldToScreenPoint(button.transform.position);
            Assert.IsTrue(aim.IsPointerOverUI(screen)); Assert.IsFalse(button.interactable);
            Assert.IsFalse(aim.IsPointerOverUI(Camera.main.WorldToScreenPoint(new Vector3(4,4,0))));
            flow.TryFire(Vector2.down); Assert.IsTrue(button.interactable);
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
            Assert.AreEqual(1,flow.Session.Round); Assert.AreEqual(SessionPhase.Aiming,flow.Session.Phase);
            Assert.IsFalse(button.interactable);
            StringAssert.Contains("待确认奖励",GameObject.Find("UI/Status").GetComponent<TMP_Text>().text);
            StringAssert.DoesNotContain("在场",GameObject.Find("UI/Status").GetComponent<TMP_Text>().text);
        }
        [UnityTest] public IEnumerator VolleyDestroysBlockAndPlaysRateLimitedFeedback()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null; yield return PlaySceneEntry.StartIfReady();
            var flow=Object.FindFirstObjectByType<GameFlowController>();var view=Object.FindFirstObjectByType<BoardView>();
            var victim=view.Views[2];var collider=victim.GetComponent<Collider2D>();
            flow.TryFire(Vector2.down);
            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(flow.Board.Blocks.ContainsKey(2)); Assert.IsTrue(!collider || !collider.enabled);
            var feedback=Object.FindFirstObjectByType<FeedbackPlayer>();
            Assert.Greater(feedback.SoundsPlayed,0); Assert.LessOrEqual(feedback.SoundsPlayed,32);
            Assert.IsFalse(Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Any(c=>c.name=="Piece"));
            flow.RequestRecall(); yield return new WaitForSeconds(0.4f);
            Assert.IsNull(GameObject.Find("Fragments"));
        }
    }
}
