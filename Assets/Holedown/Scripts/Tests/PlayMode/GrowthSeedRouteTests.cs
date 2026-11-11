#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
        // Fixed approved seed routes, not the planner's later production difficulty revisions.
    public sealed class GrowthSeedRouteTests
    {
        float oldScale;
        GrowthSeedScope seed;
        [SetUp] public void Setup() { oldScale=Time.timeScale; Time.timeScale=20; PlayerPrefs.DeleteKey("Holedown.Difficulty"); seed=new GrowthSeedScope(); }
        [TearDown] public void Cleanup() { BallActor.IgnoreRampForTests=false; Time.timeScale=oldScale; seed.Dispose(); }
        static IEnumerator Shoot(GameFlowController flow,float angle)
        {
            float a=angle*Mathf.Deg2Rad;
            Assert.IsTrue(flow.TryFire(new Vector2(Mathf.Sin(a),-Mathf.Cos(a))));
            float deadline=Time.realtimeSinceStartup+15;
            while(flow.Session.Phase==SessionPhase.Volley||flow.Session.Phase==SessionPhase.Recalling) {
                Assert.Less(Time.realtimeSinceStartup,deadline,"Physical volley timeout");yield return null;
            }
        }
        static IEnumerator Route(bool first,bool second,float stage2Early,float stage2Late,float stage3Late,int finalRounds)
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;yield return PlaySceneEntry.StartIfReady();
            var flow=UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            // These approved routes were shot at one constant ball speed. The per-round ramp is
            // pinned off exactly (not merely slowed) so the reward accounting stays the subject.
            BallActor.IgnoreRampForTests=true;
            Assert.AreEqual(3,flow.Campaign.StageCount);
            for(int stage=0;stage<3;stage++) {
                int expectedBalls=stage==0?12:stage==1?12+(first?3:0):12+(first?3:0)+(second?5:0);
                Assert.AreEqual(expectedBalls,flow.Campaign.EntryBalls);
                for(int shot=0;shot<(stage==2?finalRounds:5);shot++) {
                    float angle=stage==0?(first?-4.4f:shot<4?10:0):stage==1?(shot<4?stage2Early:stage2Late):(shot<4?-4.4f:stage3Late);
                    yield return Shoot(flow,angle);
                    Assert.AreEqual(expectedBalls,flow.Campaign.EntryBalls,"Reward must not increase current-stage volley");
                }
                Assert.AreEqual(SessionPhase.Won,flow.Session.Phase,$"Stage {stage+1}");
                Assert.AreEqual(stage==0?(first?3:0):stage==1?(second?5:0):0,flow.Campaign.PendingBonus);
                Debug.Log($"Growth seed route {first}/{second}: stage {stage+1}, {expectedBalls} balls, {flow.Session.Round} rounds, confirmed {flow.Campaign.ConfirmedBonus}");
                if(stage<2){flow.NextStage();yield return null;}
            }
            Assert.AreEqual((first?3:0)+(second?5:0),flow.Campaign.ConfirmedBonus);
            Assert.IsTrue(flow.Campaign.IsFinalStage);
        }
        [UnityTest] public IEnumerator NeitherReward() { yield return Route(false,false,0,-4.4f,4.4f,6); }
        [UnityTest] public IEnumerator OnlyFirstReward() { yield return Route(true,false,-4.4f,0,4.4f,6); }
        [UnityTest] public IEnumerator OnlySecondReward() { yield return Route(false,true,-4.4f,4.4f,4.4f,5); }
        [UnityTest] public IEnumerator BothRewards() { yield return Route(true,true,-4.4f,4.4f,0,5); }
    }
}
#endif
