#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Holedown.Tests
{
    public sealed class AudioFeedbackTests
    {
        string directory;

        [SetUp]
        public void Setup()
        {
            BoardView.SkipAdvanceForTests = true;
            directory = Path.GetFullPath("Library/AudioFeedbackTests/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json", Path.Combine(directory, "settings.json"));
            File.WriteAllText(Path.Combine(directory, "level.json"), @"{
                ""schemaVersion"": 2, ""version"": ""audio-tests"", ""levels"": [
                    {""id"": ""first"", ""name"": ""First"", ""initialBlocks"": [
                        {""column"": 0, ""row"": 6, ""width"": 1, ""height"": 1, ""health"": 2, ""target"": false, ""rewardBalls"": 0},
                        {""column"": 1, ""row"": 2, ""width"": 1, ""height"": 1, ""health"": 2, ""target"": false, ""rewardBalls"": 0},
                        {""column"": 2, ""row"": 2, ""width"": 1, ""height"": 1, ""health"": 1, ""target"": false, ""rewardBalls"": 0},
                        {""column"": 3, ""row"": 2, ""width"": 1, ""height"": 1, ""health"": 1, ""target"": false, ""rewardBalls"": 3}
                    ], ""incomingRows"": [{""blocks"": [{""column"": 3, ""row"": 0, ""width"": 1, ""height"": 1, ""health"": 1, ""target"": true, ""rewardBalls"": 0}]}]},
                    {""id"": ""second"", ""name"": ""Second"", ""initialBlocks"": [], ""incomingRows"": [
                        {""blocks"": [{""column"": 3, ""row"": 0, ""width"": 1, ""height"": 1, ""health"": 1, ""target"": true, ""rewardBalls"": 0}]}]}
                ]
            }");
            SceneManager.sceneLoaded += ConfigureScene;
        }

        void ConfigureScene(Scene scene, LoadSceneMode mode)
        {
            var flow = new SerializedObject(Object.FindFirstObjectByType<GameFlowController>());
            flow.FindProperty("externalConfigDirectory").stringValue = directory;
            flow.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Cleanup()
        {
            BoardView.SkipAdvanceForTests = false;
            SceneManager.sceneLoaded -= ConfigureScene;
            Directory.Delete(directory, true);
        }

        [UnityTest]
        public IEnumerator DenseImpactsKeepIndependentCooldownsAndTargetWaitsForSettlement()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            yield return PlaySceneEntry.StartIfReady();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var feedback = Object.FindFirstObjectByType<FeedbackPlayer>();
            var clipNames = new[] { "launchSound", "hitSound", "breakSound", "rewardSound", "winSound", "lossSound", "recallSound", "confirmSound" };
            var serializedFeedback = new SerializedObject(feedback);
            foreach (string name in clipNames)
            {
                var clip = serializedFeedback.FindProperty(name).objectReferenceValue as AudioClip;
                Assert.IsNotNull(clip, name);
                Assert.AreEqual(1, clip.channels, name);
            }
            var hits = typeof(GameFlowController).GetMethod("OnHit", BindingFlags.Instance | BindingFlags.NonPublic);
            int wins = 0;
            flow.RoundSettled += phase => { if (phase == SessionPhase.Won) wins++; };

            Assert.IsTrue(flow.TryFire(Vector2.down));
            var ball = Object.FindFirstObjectByType<BallActor>();
            void Hit(int column)
            {
                int id = flow.Board.Blocks.Values.Single(block => block.Column == column).Id;
                hits.Invoke(flow, new object[] { ball, id });
            }

            Hit(0);
            Hit(1);
            Assert.AreEqual(1, feedback.SoundsPlayed, "Ordinary impacts share the 50 ms cooldown.");
            Hit(1);
            Hit(2);
            Assert.AreEqual(2, feedback.SoundsPlayed, "Breaks use their own cooldown.");
            Hit(3);
            Assert.AreEqual(3, feedback.SoundsPlayed, "The reward bypasses both cooldowns.");
            Assert.AreEqual(3, flow.Campaign.PendingBonus);
            flow.RequestRecall();
            yield return null;
            yield return null;

            Assert.IsTrue(flow.TryFire(Vector2.down));
            ball = Object.FindFirstObjectByType<BallActor>();
            yield return new WaitForSeconds(0.09f);
            Hit(0);
            Assert.AreEqual(4, feedback.SoundsPlayed);
            Hit(3);
            Assert.IsTrue(flow.Board.TargetDestroyed);
            Assert.AreEqual(5, feedback.SoundsPlayed, "The target can be heard over an ordinary break in the same frame.");
            Assert.AreEqual(0, wins);
            flow.RequestRecall();
            yield return null;
            yield return null;
            Assert.AreEqual(SessionPhase.Won, flow.Session.Phase);
            Assert.AreEqual(1, wins);
            yield return null;
            Assert.AreEqual(1, wins);
        }

        [UnityTest]
        public IEnumerator VolleyLaunchRecallFailureAndConfirmationFollowAcceptedActions()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            int launches = 0, recalls = 0, losses = 0, confirmations = 0;
            flow.VolleyLaunched += () => launches++;
            flow.ManualRecallAccepted += () => recalls++;
            flow.RoundSettled += phase => { if (phase == SessionPhase.Lost) losses++; };
            flow.CommandConfirmed += () => confirmations++;
            yield return PlaySceneEntry.StartIfReady();
            Assert.AreEqual(1, confirmations);

            flow.RequestRecall();
            Assert.AreEqual(0, recalls);
            Assert.IsFalse(flow.TryFire(Vector2.up));
            Assert.AreEqual(0, launches);
            flow.Settings.launchInterval = 0.01f;
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return new WaitForSeconds(0.07f);
            Assert.Greater(flow.Session.ActiveCount, 1);
            Assert.AreEqual(1, launches);

            flow.QueueCommand(FlowCommand.Recall);
            yield return null;
            yield return null;
            flow.RequestRecall();
            Assert.AreEqual(1, recalls);
            Assert.AreEqual(1, confirmations);

            flow.RequestPause();
            flow.RequestPause();
            Assert.AreEqual(2, confirmations);
            flow.ContinueGame();
            Assert.AreEqual(3, confirmations);
            yield return null;

            flow.Settings.recallTimeout = 0.03f;
            Assert.IsTrue(flow.TryFire(Vector2.down));
            yield return new WaitForSeconds(0.12f);
            Assert.AreEqual(SessionPhase.Lost, flow.Session.Phase);
            Assert.AreEqual(1, losses);
            Assert.AreEqual(2, launches);
            Assert.AreEqual(1, recalls, "Timeout does not play the manual recall cue.");
            flow.RetryStage();
            flow.RetryStage();
            Assert.AreEqual(4, confirmations);
        }
    }
}
#endif
