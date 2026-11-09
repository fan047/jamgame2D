using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    // The waiting area is display only: real column footprints, no rules, no identity,
    // and it stays blank for an empty row and for an exhausted queue.
    public sealed class IncomingPreviewTests
    {
        [UnityTest] public IEnumerator PreviewShowsOnlyTheNextRowFootprint()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var preview = Object.FindFirstObjectByType<IncomingRowPreview>();
            var theme = preview.Theme;
            Assert.IsNotNull(theme, "the presenter needs the shared visual theme");
            Assert.IsTrue(flow.Board.TryPeekIncomingRow(out var row));
            Assert.AreEqual(row.Count, preview.CellCount, "one silhouette per real block");
            var cells = preview.Cells.Where(cell => cell && cell.gameObject.activeSelf).ToList();
            for (int i = 0; i < row.Count; i++)
            {
                var cell = cells[i];
                Assert.AreEqual(row[i].Column + row[i].Width * 0.5f, cell.transform.position.x, 0.0001f, "same column as the rule row");
                Assert.AreEqual(preview.CentreY, cell.transform.position.y, 0.0001f);
                Assert.AreEqual(row[i].Width - 0.1f, cell.bounds.size.x, 0.05f, "same width as the rule block");
                Assert.AreEqual(theme.Block(row[i].Width, 1), cell.sprite, "ordinary silhouette, never a role sprite");
                Assert.Less(cell.bounds.max.y, 0.05f, "the waiting area never reaches into row 0");
            }
            // Identity and interaction stay hidden: no text, no collider, no body.
            Assert.IsEmpty(preview.GetComponentsInChildren<TMP_Text>(true), "the waiting row never shows numbers");
            Assert.IsEmpty(preview.GetComponentsInChildren<Collider2D>(true), "the waiting row cannot be hit");
            Assert.IsEmpty(preview.GetComponentsInChildren<Rigidbody2D>(true));
            Assert.AreEqual(2, cells[0].sortingOrder, "the rock foreground stays in front of the waiting row");
            // Aiming stays on the real board only: a straight-down shot stops on a block or
            // on the floor line, never in the waiting area below it.
            var aim = Object.FindFirstObjectByType<AimInput>();
            Physics2D.SyncTransforms();
            Assert.Greater(aim.PreviewEnd(Vector2.down).y, 0f, "the prediction never reaches the waiting area");
        }
        [UnityTest] public IEnumerator EmptyRowAndExhaustionLeaveTheWaitingAreaBlank()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            Object.FindFirstObjectByType<GameFlowController>().enabled = false;
            var preview = Object.FindFirstObjectByType<IncomingRowPreview>();
            var board = new BoardState(
                new[] { new BlockDefinition(0, 0, 1, 1) },
                new[]
                {
                    new RowDefinition { blocks = new List<BlockDefinition> { new BlockDefinition(1, 0, 2, 3, false, 2) } },
                    new RowDefinition(),
                    new RowDefinition { blocks = new List<BlockDefinition> { new BlockDefinition(7, 0, 1, 1, true) } }
                });
            preview.Present(board, false);
            Assert.AreEqual(1, preview.CellCount);
            Assert.AreEqual(2f, preview.Cells[0].transform.position.x, 0.0001f, "a two-wide block covers both columns");
            board.Advance();
            preview.Present(board, false);
            Assert.AreEqual(0, preview.CellCount, "an empty next row stays blank instead of showing a later row");
            board.Advance();
            preview.Present(board, false);
            Assert.AreEqual(1, preview.CellCount);
            Assert.AreEqual(7.5f, preview.Cells[0].transform.position.x, 0.0001f, "the last row is still previewed");
            board.Advance();
            Assert.IsFalse(board.TryPeekIncomingRow(out _));
            preview.Present(board, false);
            Assert.AreEqual(0, preview.CellCount, "an exhausted queue creates no placeholder");
        }
        [UnityTest] public IEnumerator FinishedRunKeepsNoWaitingRow()
        {
            var directory = System.IO.Path.GetFullPath("Library/Locus/tmp/preview-end/" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.Copy("Config/settings.json", System.IO.Path.Combine(directory, "settings.json"));
            string Block(int c, int r, int hp, bool target) => $"{{\"column\":{c},\"row\":{r},\"width\":1,\"height\":1,\"health\":{hp},\"target\":{(target ? "true" : "false")},\"rewardBalls\":0}}";
            // A block already on the last row trips the danger line on the first advance.
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "level.json"),
                "{\"schemaVersion\":2,\"version\":\"preview-end\",\"levels\":[{\"id\":\"end\",\"name\":\"End\",\"initialBlocks\":[" + Block(1, 7, 6, false) +
                "],\"incomingRows\":[{\"blocks\":[" + Block(7, 0, 3, false) + "]},{\"blocks\":[" + Block(4, 0, 1, true) + "]}]}]}");
            SceneManager.sceneLoaded += Configure;
            void Configure(Scene scene, LoadSceneMode mode)
            {
                var controller = Object.FindFirstObjectByType<GameFlowController>();
                if (!controller) return;
                var serialized = new UnityEditor.SerializedObject(controller);
                serialized.FindProperty("externalConfigDirectory").stringValue = directory;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            try
            {
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
                yield return PlaySceneEntry.StartIfReady();
                var flow = Object.FindFirstObjectByType<GameFlowController>();
                var preview = Object.FindFirstObjectByType<IncomingRowPreview>();
                Assert.Greater(preview.CellCount, 0, "a running stage previews its next row");
                Assert.IsTrue(flow.TryFire(new Vector2(0.02f, -1f))); flow.RequestRecall();
                float deadline = Time.realtimeSinceStartup + 5f;
                while (flow.Session.Phase != SessionPhase.Lost && Time.realtimeSinceStartup < deadline)
                {
                    if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                    yield return null;
                }
                Assert.AreEqual(SessionPhase.Lost, flow.Session.Phase);
                yield return null;
                Assert.AreEqual(0, preview.CellCount, "a finished run keeps no waiting row");
            }
            finally
            {
                SceneManager.sceneLoaded -= Configure;
                if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
            }
        }
        [UnityTest] public IEnumerator WaitingRowStaysStableThroughAPausedVolley()        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var preview = Object.FindFirstObjectByType<IncomingRowPreview>();
            var before = preview.Cells.Where(cell => cell && cell.gameObject.activeSelf).Select(cell => cell.transform.position).ToArray();
            Assert.IsTrue(flow.Board.TryPeekIncomingRow(out var row));
            Assert.IsTrue(flow.TryFire(Vector2.down));
            flow.RequestPause(); yield return null;
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            CollectionAssert.AreEqual(before, preview.Cells.Where(cell => cell && cell.gameObject.activeSelf).Select(cell => cell.transform.position).ToArray(), "the waiting row does not drift while paused");
            Assert.AreEqual(row.Count, preview.CellCount, "aiming never changes what is previewed");
            flow.ContinueGame(); yield return null;
            flow.RequestRecall(); yield return null;
        }
    }
}
