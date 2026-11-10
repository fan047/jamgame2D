#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    // Scene-level acceptance for the per-round speed ramp: the first ball starts at the base
    // speed, later balls of the same round start at the current multiplier, pause freezes the
    // curve, the next round resets it, and full speed keeps bounces and hits intact.
    public sealed class BallSpeedRampPlayTests
    {
        GameFlowController flow;
        float savedRamp, savedSpeed;
        [SetUp] public void Setup() { flow = null; BoardView.SkipAdvanceForTests = false; }
        [TearDown] public void Cleanup()
        {
            if (flow)
            {
                if (flow.Settings) { flow.Settings.ballSpeedRampSeconds = savedRamp; flow.Settings.ballSpeed = savedSpeed; }
                flow.enabled = false;
            }
            BoardView.SkipAdvanceForTests = false;
            Time.timeScale = 1f;
        }
        IEnumerator Load(float rampSeconds)
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            flow = Object.FindFirstObjectByType<GameFlowController>();
            savedRamp = flow.Settings.ballSpeedRampSeconds; savedSpeed = flow.Settings.ballSpeed;
            flow.Settings.ballSpeedRampSeconds = rampSeconds;
            flow.SendMessage("OnApplicationFocus", true); yield return null;
            yield return PlaySceneEntry.StartIfReady();
        }
        static BallActor[] Balls() => Object.FindObjectsByType<BallActor>(FindObjectsSortMode.None)
            .OrderBy(ball => ball.BallId).ToArray();
        [UnityTest] public IEnumerator FirstBallStartsAtBaseAndLaterBallsUseTheCurrentMultiplier()
        {
            yield return Load(0.6f);
            float baseSpeed = BallActor.SafeBaseSpeed(flow.Settings.ballSpeed);
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return new WaitForSeconds(0.03f);
            var first = Balls().First();
            Assert.AreEqual(baseSpeed, first.CurrentSpeed, baseSpeed * 0.1f, "the first ball leaves at the base speed");
            yield return new WaitForSeconds(0.36f);
            var alive = Balls();
            Assert.Greater(alive.Length, 1, "the round keeps launching later balls");
            var latest = alive.Last();
            Assert.Greater(latest.CurrentSpeed, baseSpeed * 1.15f, "a later ball starts at the current multiplier");
            foreach (var ball in alive)
                Assert.AreEqual(ball.CurrentSpeed, ball.Velocity.magnitude, 0.05f, "the applied speed is the curve value");
            flow.RequestRecall();
            yield return null;
        }
        [UnityTest] public IEnumerator PauseFreezesTheRampAndContinueResumesIt()
        {
            yield return Load(0.8f);
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return new WaitForSeconds(0.25f);
            var ball = Balls().First();
            float before = ball.CurrentSpeed;
            flow.RequestPause(); yield return null;
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            Assert.AreEqual(0f, Time.timeScale);
            var frozen = Balls().Select(b => b.CurrentSpeed).ToArray();
            yield return new WaitForSecondsRealtime(0.3f);
            CollectionAssert.AreEqual(frozen, Balls().Select(b => b.CurrentSpeed).ToArray(), "a pause freezes the curve");
            flow.ContinueGame(); yield return null;
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(ball.CurrentSpeed, before, "continuing resumes the same round curve");
            flow.RequestRecall(); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator NextRoundStartsFromTheBaseSpeedAgain()
        {
            yield return Load(0.3f);
            float baseSpeed = BallActor.SafeBaseSpeed(flow.Settings.ballSpeed);
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(Balls().Last().CurrentSpeed, baseSpeed * 1.5f, "the round has ramped up");
            Assert.IsTrue(flow.CanSimulate, "the volley is still running when the test recalls it");
            flow.RequestRecall();
            // Recall itself is immediate; the settle and the row advance run in the next frames.
            yield return null; yield return null;
            float deadline = Time.realtimeSinceStartup + 8f;
            string timeline = "";
            while (!flow.CanSimulate && Time.realtimeSinceStartup < deadline)
            {
                timeline += $"[{Time.time:0.00} {flow.Phase}/{flow.Session.Phase} adv={flow.IsAdvancing} focus={flow.HasFocus}] ";
                if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                yield return null;
            }
            yield return null;
            Assert.IsTrue(flow.CanSimulate, $"round two must be ready: phase={flow.Phase}, session={flow.Session.Phase}, advancing={flow.IsAdvancing}, scale={Time.timeScale} :: {timeline}");
            Assert.IsTrue(flow.TryFire(Vector2.down), "the next round can fire");
            yield return new WaitForSeconds(0.03f);
            Assert.AreEqual(baseSpeed, Balls().First().CurrentSpeed, baseSpeed * 0.1f, "a new round restarts at the base speed");
            flow.RequestRecall(); yield return null;
        }
        [UnityTest] public IEnumerator FullSpeedKeepsFloorWallAndNarrowGapCollisions()
        {
            yield return Load(0.05f);
            // Base 50 is the capped half of the 100 limit, so the round runs at the physics cap.
            flow.Settings.ballSpeed = 50f;
            flow.enabled = false;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab");
            var board = Object.FindFirstObjectByType<BoardView>();
            board.Clear();
            // A one-column gap between two blocks, with a target block behind the gap and a
            // tall block on the right: the full-speed ball must pass the gap and still hit.
            var state = new BoardState(new[]
            {
                new BlockDefinition(0, 0, 2, 5),
                new BlockDefinition(3, 2, 1, 3),
                new BlockDefinition(6, 0, 1, 5, false, 0, 2)
            }, new[] { new RowDefinition { blocks = new System.Collections.Generic.List<BlockDefinition> { new BlockDefinition(7, 0, 1, 1, true) } } });
            board.Sync(state);
            Physics2D.SyncTransforms();
            int hits = 0; bool returned = false; bool floorBounce = false, wallBounce = false;
            float topSpeed = 0f;
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            try
            {
                settings.ballSpeed = 50f; settings.ballSpeedRampSeconds = 0.05f; settings.ballRadius = 0.12f;
                var ball = Object.Instantiate(prefab, AimInput.Origin, Quaternion.identity);
                ball.Hit += (b, id) => { var result = state.ApplyHit(id); board.ApplyHit(id, result); hits++; };
                ball.Returned += b => returned = true;
                // Straight down through the gap column, then it keeps bouncing at full speed.
                ball.Launch(settings, System.Guid.NewGuid(), 1, 1, Vector2.down, Time.time);
                float deadline = Time.time + 12f;
                float previousY = ball.Velocity.y, previousX = ball.Velocity.x;
                while (!returned && Time.time < deadline)
                {
                    if (ball)
                    {
                        topSpeed = Mathf.Max(topSpeed, ball.Velocity.magnitude);
                        var velocity = ball.Velocity;
                        if (previousY < 0f && velocity.y > 0f) floorBounce = true;
                        if (Mathf.Sign(previousX) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 1f) wallBounce = true;
                        previousY = velocity.y; previousX = velocity.x;
                        Assert.That(ball.transform.position.y, Is.GreaterThan(-0.05f));
                        Assert.That(ball.transform.position.x, Is.InRange(-0.05f, 8.05f));
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.IsTrue(returned, "the full-speed ball returns");
                Assert.AreEqual(100f, topSpeed, 1f, "the round really ran at the physics cap");
                Assert.IsTrue(floorBounce, "the floor still bounces at full speed");
                Assert.IsTrue(wallBounce, "the side walls still bounce at full speed");
                Assert.Greater(hits, 0, "the narrow gap is still passable and the block behind it is hit");
            }
            finally { Object.Destroy(settings); }
        }
    }
}
#endif
