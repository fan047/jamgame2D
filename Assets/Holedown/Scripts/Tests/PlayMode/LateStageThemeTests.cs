#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    // Scene-level acceptance for the two late-stage caves: the group follows the stage, the
    // waiting row keeps build 22's shared silhouette and only the landed block wears the
    // group art, and planner jumps, reloads and retries all keep one consistent set.
    public sealed class LateStageThemeTests
    {
        string directory;
        VisualTheme theme;
        // Static so a scene-load handler left over from an earlier test in the same run still
        // applies the mode the current test asked for.
        static bool plannerMode = true;

        [SetUp] public void Setup()
        {
            plannerMode = true;
            BoardView.SkipAdvanceForTests = true;
            directory = Path.GetFullPath("Library/Locus/tmp/late-stage-tests/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            File.Copy("Config/settings.json", Path.Combine(directory, "settings.json"));
            WriteLevel(fastLoss: false);
            SceneManager.sceneLoaded += Configure;
        }
        // A block already on row 7 trips the danger line after a single advance, which is how
        // the retry path gets a lost session without playing a whole stage.
        void WriteLevel(bool fastLoss)
        {
            var levels = new JArray();
            for (int i = 0; i < 9; i++)
            {
                JObject Block(int c, int r) => new JObject { { "column", c }, { "row", r }, { "width", 1 }, { "height", 1 }, { "health", 1 }, { "target", false }, { "rewardBalls", 0 } };
                var initial = fastLoss ? new JArray(Block(6, 7)) : new JArray(Block(0, 4), Block(3, 2), Block(6, 4));
                levels.Add(new JObject
                {
                    { "id", "s" + i }, { "name", "Stage " + i },
                    { "initialBlocks", initial },
                    { "incomingRows", new JArray(new JObject { { "blocks", new JArray(new JObject { { "column", 3 }, { "row", 0 }, { "width", 1 }, { "height", 1 }, { "health", 1 }, { "target", true }, { "rewardBalls", 0 } }) } }) }
                });
            }
            File.WriteAllText(Path.Combine(directory, "level.json"), new JObject { { "schemaVersion", 2 }, { "version", "late-stage-fixture" }, { "levels", levels } }.ToString());
        }
        void Configure(Scene scene, LoadSceneMode mode)
        {
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            if (!flow) return;
            var serialized = new SerializedObject(flow);
            serialized.FindProperty("externalConfigDirectory").stringValue = directory;
            serialized.FindProperty("plannerTestMode").boolValue = plannerMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown] public void Cleanup()
        {
            SceneManager.sceneLoaded -= Configure;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            if (theme) theme = null;
            BoardView.SkipAdvanceForTests = false;
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            flow.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
            Assert.AreEqual(plannerMode ? AppPhase.PlannerSelection : AppPhase.Title, flow.Phase);
            theme = (VisualTheme)new SerializedObject(UnityEngine.Object.FindFirstObjectByType<SceneVisuals>()).FindProperty("theme").objectReferenceValue;
            if (!plannerMode) { flow.StartGame(); yield return null; yield return null; }
        }
        SpriteRenderer Background() => GameObject.Find("Game/Background").GetComponent<SpriteRenderer>();
        SpriteRenderer Foreground() => GameObject.Find("Game/Foreground").GetComponent<SpriteRenderer>();
        [UnityTest] public IEnumerator PlannerJumpReloadAndRetryKeepOneGroupSet()
        {
            yield return Load();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            foreach (var (stageId, index) in new[] { ("s3", 3), ("s6", 6) })
            {
                flow.QueuePlannerTest(stageId, PlannerBallMode.BaseOnly, 1); yield return null; yield return null;
                Assert.AreEqual(index, flow.Campaign.StageIndex, "phase=" + flow.Phase + " plannerError=" + flow.PlannerError + " init=" + flow.InitializationError + " input=" + flow.UserInputAllowed + " planner=" + flow.IsPlannerTest);
                Assert.AreSame(theme.Background(index), Background().sprite, "background follows the stage");
                Assert.IsTrue(Foreground().enabled);
                Assert.AreSame(theme.Foreground(index), Foreground().sprite, "foreground follows the stage");
                Assert.IsTrue(theme.UsesOwnStages(index));
                foreach (var block in view.Views.Values)
                {
                    Assert.AreEqual(index, block.StageIndex);
                    var rule = flow.Board.Blocks[block.BlockId];
                    Assert.AreSame(theme.Block(index, rule.Width, rule.Height, rule.IsTarget, rule.RewardBalls), block.Visual.sprite,
                        "the landed block wears the group art");
                }
                // Reloading the same stage keeps the same set instead of falling back.
                flow.ReloadAndRestart(); yield return null; yield return null;
                Assert.AreEqual(index, flow.Campaign.StageIndex);
                Assert.AreSame(theme.Background(index), Background().sprite);
                Assert.AreSame(theme.Foreground(index), Foreground().sprite);
                // A real retry after losing also stays inside the group.
                WriteLevel(fastLoss: true);
                flow.ReloadAndRestart(); yield return null; yield return null;
                Assert.AreEqual(index, flow.Campaign.StageIndex);
                yield return Volley(flow, view, Vector2.down);
                Assert.AreEqual(SessionPhase.Lost, flow.Session.Phase, "the fast-loss layout must lose");
                flow.RetryStage(); yield return null; yield return null;
                Assert.AreSame(theme.Background(index), Background().sprite, "retry keeps the group background");
                Assert.AreSame(theme.Foreground(index), Foreground().sprite, "retry keeps the group foreground");
                WriteLevel(fastLoss: false);
            }
        }
        [UnityTest] public IEnumerator WaitingRowKeepsTheSharedSilhouetteAndOnlyTheLandedBlockChanges()
        {
            yield return Load();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            var preview = view.Preview;
            // This test needs the real advance animation, so the instant-settle seam is off.
            BoardView.SkipAdvanceForTests = false;
            flow.QueuePlannerTest("s6", PlannerBallMode.BaseOnly, 1); yield return null; yield return null;
            Assert.AreEqual(6, flow.Campaign.StageIndex);
            // The waiting row is build 22's shared grey-blue silhouette: the preview material
            // multiplies the base colour, so it must use the first cave's ordinary sprite.
            Assert.Greater(preview.CellCount, 0);
            Assert.IsTrue(flow.Board.TryPeekIncomingRow(out var row));
            for (int i = 0; i < row.Count; i++)
                Assert.AreSame(theme.Block(row[i].Width, 1), preview.Cells[i].sprite, "the waiting row keeps the shared silhouette");
            Assert.AreNotSame(theme.Block(6, row[0].Width, 1, false, 0), preview.Cells[0].sprite);
            // During the hand-over the arriving block still wears that silhouette; group art
            // only appears once the block has landed on row 0.
            float saved = Time.timeScale;
            Holedown.BlockView[] arriving = null;
            try
            {
                Time.timeScale = 0f;
                Assert.IsTrue(flow.TryFire(new Vector2(0.02f, -1f)));
                flow.RequestRecall();
                yield return null; yield return null;
                Assert.IsTrue(view.IsAdvancing);
                arriving = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(view.Views.Values, v => v && v.IsEntering));
                Assert.Greater(arriving.Length, 0);
                foreach (var block in arriving)
                {
                    var rule = flow.Board.Blocks[block.BlockId];
                    Assert.AreSame(theme.Block(rule.Width, rule.Height), block.Visual.sprite, "the entrance keeps the shared silhouette");
                    Assert.IsFalse(block.GetComponentInChildren<TMP_Text>(true).enabled);
                }
            }
            finally { Time.timeScale = saved; }
            float deadline = Time.realtimeSinceStartup + 3f;
            while (view.IsAdvancing && Time.realtimeSinceStartup < deadline)
            {
                if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                yield return null;
            }
            Assert.IsFalse(view.IsAdvancing);
            foreach (var block in arriving)
            {
                var rule = flow.Board.Blocks[block.BlockId];
                Assert.AreSame(theme.Block(6, rule.Width, rule.Height, rule.IsTarget, rule.RewardBalls), block.Visual.sprite,
                    "the group art arrives with the landing");
                Assert.IsTrue(block.GetComponentInChildren<TMP_Text>(true).enabled);
            }
        }
        IEnumerator Volley(GameFlowController flow, BoardView view, Vector2 direction, bool recallAtOnce = true)
        {
            float wait = Time.realtimeSinceStartup + 3f;
            while (!flow.CanSimulate && Time.realtimeSinceStartup < wait)
            {
                if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                yield return null;
            }
            Assert.IsTrue(flow.TryFire(direction), "the volley must start");
            if (recallAtOnce) flow.RequestRecall();
            else
            {
                // Let the balls actually reach their target before the volley is closed.
                float flight = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < flight && flow.Session.Phase == SessionPhase.Volley && flow.Session.ActiveCount > 0)
                {
                    if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                    yield return null;
                }
                if (flow.Session.Phase == SessionPhase.Volley) flow.RequestRecall();
            }
            wait = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < wait && (flow.Session.Phase == SessionPhase.Volley || flow.Session.Phase == SessionPhase.Recalling || view.IsAdvancing || flow.IsAdvancing))
            {
                if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                yield return null;
            }
            yield return null;
        }
        void AssertGroupSet(int index)
        {
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Assert.AreEqual(index, flow.Campaign.StageIndex);
            Assert.AreSame(theme.Background(index), Background().sprite, "background follows the stage");
            Assert.AreSame(theme.Foreground(index), Foreground().sprite, "foreground follows the stage");
            foreach (var block in view.Views.Values)
            {
                Assert.AreEqual(index, block.StageIndex);
                var rule = flow.Board.Blocks[block.BlockId];
                Assert.AreSame(theme.Block(index, rule.Width, rule.Height, rule.IsTarget, rule.RewardBalls), block.Visual.sprite);
            }
        }
        [UnityTest] public IEnumerator FormalWinThenNextStageSwitchesGroupsAtThreeToFourAndSixToSeven()
        {
            // The real campaign path: no planner, no jump. Only NextStage after a won stage
            // may move forward, and the cave group must switch exactly at 3→4 and 6→7.
            plannerMode = false;
            yield return Load();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            AssertGroupSet(0);
            for (int stage = 0; stage < 6; stage++)
            {
                yield return Volley(flow, view, Vector2.down);
                Assert.AreEqual(SessionPhase.Aiming, flow.Session.Phase, "stage " + (stage + 1) + " advances");
                yield return Volley(flow, view, new Vector2(-0.06f, -1f), recallAtOnce: false);
                Assert.AreEqual(SessionPhase.Won, flow.Session.Phase, "stage " + (stage + 1) + " must be won by a real volley");
                flow.NextStage(); yield return null; yield return null;
                Assert.AreEqual(stage + 1, flow.Campaign.StageIndex, "NextStage moves one stage forward");
                if (stage + 1 == 3) AssertGroupSet(3);
                if (stage + 1 == 6) AssertGroupSet(6);
            }
        }
        [UnityTest] public IEnumerator RetryAfterALossKeepsTheSameGroupSet()
        {
            WriteLevel(fastLoss: true);
            yield return Load();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            flow.QueuePlannerTest("s6", PlannerBallMode.BaseOnly, 1); yield return null; yield return null;
            AssertGroupSet(6);
            yield return Volley(flow, view, Vector2.down);
            Assert.AreEqual(SessionPhase.Lost, flow.Session.Phase, "the row-7 block must trip the danger line");
            var session = flow.Session.SessionId;
            flow.RetryStage(); yield return null; yield return null;
            Assert.AreEqual(AppPhase.Playing, flow.Phase);
            Assert.AreNotEqual(session, flow.Session.SessionId, "retry starts a fresh attempt");
            AssertGroupSet(6);
        }
        [UnityTest] public IEnumerator MissingGroupBackgroundKeepsTheFirstCaveWhole()
        {
            yield return Load();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            var saved = theme.backgroundStages7To9;
            try
            {
                theme.backgroundStages7To9 = null;
                flow.QueuePlannerTest("s6", PlannerBallMode.BaseOnly, 1); yield return null; yield return null;
                Assert.AreEqual(6, flow.Campaign.StageIndex);
                Assert.AreSame(theme.background, Background().sprite);
                Assert.AreSame(theme.foreground, Foreground().sprite, "the first cave keeps its decorations");
                foreach (var block in view.Views.Values)
                {
                    var rule = flow.Board.Blocks[block.BlockId];
                    Assert.AreSame(theme.Block(rule.Width, rule.Height, rule.IsTarget, rule.RewardBalls), block.Visual.sprite);
                }
            }
            finally { theme.backgroundStages7To9 = saved; }
        }
    }
}
#endif
