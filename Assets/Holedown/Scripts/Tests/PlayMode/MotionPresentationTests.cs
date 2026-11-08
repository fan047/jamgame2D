#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;
namespace Holedown.Tests
{
    // Launch pulse, ball trail and the fragment cap from the mid-stage motion plan.
    public sealed class MotionPresentationTests
    {
        GameFlowController flow;
        [SetUp] public void Setup() { flow=null;BoardView.SkipAdvanceForTests=true; }
        [TearDown] public void Cleanup() { if(flow)flow.enabled=false;BoardView.SkipAdvanceForTests=false; }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;
            flow=Object.FindFirstObjectByType<GameFlowController>();flow.SendMessage("OnApplicationFocus",true);
            yield return null;yield return PlaySceneEntry.StartIfReady();
        }
        [UnityTest] public IEnumerator FirstBallPulsesTheLauncherAndBallsCarryAShortTrail()
        {
            yield return Load();
            var visuals=Object.FindFirstObjectByType<SceneVisuals>();
            var launcher=(SpriteRenderer)new SerializedObject(visuals).FindProperty("launcher").objectReferenceValue;
            Assert.IsNotNull(launcher);var rest=launcher.transform.localScale;
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return null;yield return null;
            Assert.Greater(launcher.transform.localScale.x,rest.x*1.01f,"the launcher should pulse on the first ball");
            var ball=Object.FindFirstObjectByType<BallActor>();Assert.IsNotNull(ball);
            var trail=ball.GetComponent<TrailRenderer>();Assert.IsNotNull(trail,"balls should carry a trailing renderer");
            Assert.AreEqual(flow.Settings.ballRadius*2f,trail.widthMultiplier,0.001f);
            Assert.Less(trail.time,0.1f,"trail length stays near two diameters");
            float deadline=Time.realtimeSinceStartup+2f;
            while(Time.realtimeSinceStartup<deadline&&launcher.transform.localScale!=rest)yield return null;
            Assert.AreEqual(rest,launcher.transform.localScale,"the launcher returns to its resting size");
        }
        [UnityTest] public IEnumerator DenseBreaksCapSimultaneousFragments()
        {
            yield return Load();
            var feedback=Object.FindFirstObjectByType<FeedbackPlayer>();
            var so=new SerializedObject(feedback);var cap=so.FindProperty("maxFragmentBursts");
            cap.intValue=1;so.ApplyModifiedPropertiesWithoutUndo();
            var burst=typeof(FeedbackPlayer).GetMethod("Fragments",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(burst);
            int before=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Fragments");
            for(int i=0;i<4;i++)feedback.StartCoroutine((IEnumerator)burst.Invoke(feedback,new object[]{Vector3.zero,Color.white}));
            yield return null;
            int after=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Fragments");
            Assert.LessOrEqual(after-before,2,"dense breaks must cap bursts, got "+(after-before));
        }
        [UnityTest] public IEnumerator PausePanelFadesInWithUnscaledTimeWhilePaused()
        {
            yield return Load();
            Assert.IsTrue(flow.TryFire(Vector2.down));flow.RequestRecall();yield return null;yield return null;
            flow.RequestPause();yield return null;
            var panel=GameObject.Find("UI/PausePanel");
            Assert.IsNotNull(panel);
            var group=panel.GetComponent<CanvasGroup>();
            Assert.IsNotNull(group,"pause panel gets a CanvasGroup for the fade");
            Assert.IsTrue(group.blocksRaycasts,"the fading panel must block clicks immediately");
            Assert.AreEqual(0f,Time.timeScale,"panel motion runs while the game is paused");
            float deadline=Time.realtimeSinceStartup+1f;
            while(group.alpha<0.999f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(1f,group.alpha,0.001f,"fade completes during pause");
            Assert.AreEqual(1f,panel.transform.localScale.x,0.001f);
            flow.ContinueGame();yield return null;
        }
        [UnityTest] public IEnumerator ResultPanelAndButtonsAnimateAndStayUsable()
        {
            yield return Load();
            var button=GameObject.Find("UI/Recall").GetComponent<Button>();
            var motion=button.GetComponent<ButtonPressMotion>();
            Assert.IsNotNull(motion,"buttons carry the press motion");
            var rest=button.transform.localScale;
            motion.OnPointerDown(null);Assert.Less(button.transform.localScale.x,rest.x);
            motion.OnPointerUp(null);Assert.AreEqual(rest,button.transform.localScale);
            var result=GameObject.Find("UI").transform.Find("ResultPanel").gameObject;
            var transition=result.GetComponent<PanelTransition>();
            Assert.IsNotNull(transition,"result panel carries the fade transition");
            Assert.AreEqual(0.14f,transition.FadeSeconds,0.0001f);
        }
        [UnityTest] public IEnumerator RewardChangesBounceThePendingNumber()
        {
            yield return Load();
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return null;yield return null;
            var ball=Object.FindFirstObjectByType<BallActor>();Assert.IsNotNull(ball);
            var reward=flow.Board.Blocks.Values.First(b=>b.RewardBalls>0);
            var hit=typeof(GameFlowController).GetMethod("OnHit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            while(flow.Board.Blocks.ContainsKey(reward.Id))hit.Invoke(flow,new object[]{ball,reward.Id});
            Assert.Greater(flow.Campaign.PendingBonus,0);
            var status=GameObject.Find("UI/HudHeader/PendingReward/RewardValue").GetComponent<TMP_Text>();
            yield return null;
            Assert.Greater(status.rectTransform.localScale.x,1.005f,"only the pending reward number bounces");
            Assert.AreEqual(Vector3.one,GameObject.Find("UI/HudHeader/BallCount").transform.localScale);
            Assert.AreEqual(Vector3.one,GameObject.Find("UI/HudHeader/StageIndex").transform.localScale);
            flow.RequestRecall();yield return null;yield return null;
        }
    }
}
#endif
