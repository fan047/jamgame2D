using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] BlockView blockPrefab;
        [SerializeField, Tooltip("Display-only presenter for the row waiting below the floor line.")]
        IncomingRowPreview preview;
        [SerializeField, Tooltip("Smooth upward move for board advance; 0 disables the animation.")]
        [Min(0f)] float advanceSeconds = 0.2f;
        [SerializeField, Tooltip("Waiting-row centre used only when no preview presenter is wired.")]
        float waitingRowY = -0.55f;
        readonly Dictionary<int, BlockView> views = new Dictionary<int, BlockView>();
        readonly Dictionary<int, Vector3> captured = new Dictionary<int, Vector3>();
        Coroutine advance;
        public IReadOnlyDictionary<int, BlockView> Views => views;
        public IncomingRowPreview Preview => preview;
        public float AdvanceSeconds => SkipAdvanceForTests ? 0f : advanceSeconds;
        // Test seam: rule and physics suites assert on immediate settlement, so they skip the visual advance.
        // The animation itself is covered by AdvanceMotionTests with production defaults.
        public static bool SkipAdvanceForTests { get; set; }
        public bool IsAdvancing { get; private set; }
        public event System.Action AdvanceCompleted;
        public void Clear()
        {
            StopAdvance();
            foreach(var view in views.Values) if(view)view.Remove();
            views.Clear();captured.Clear();
            if (preview) preview.SetVisible(false);
        }
        public void Sync(BoardState board)
        {
            foreach (var block in board.Blocks.Values)
            {
                if (!views.TryGetValue(block.Id, out var view))
                { view = Instantiate(blockPrefab, transform); views.Add(block.Id, view); }
                view.Bind(block);
            }
            Physics2D.SyncTransforms();
        }
        // Shows the row the next advance would add. The opening board is presented at its
        // rule positions; only the waiting row fades in.
        public void PresentIncoming(BoardState board, bool animate)
        {
            if (!preview) return;
            preview.Present(board, animate && !SkipAdvanceForTests);
        }
        // Wins, losses and reloads leave no waiting row behind: nothing is promised for a
        // run that is already over.
        public void HideIncoming()
        {
            if (preview) preview.SetVisible(false);
        }
        // Called before a settle so the view knows where blocks started.
        public void CapturePositions()
        {
            captured.Clear();
            foreach (var pair in views) if (pair.Value) captured[pair.Key] = pair.Value.transform.position;
        }
        // Plays the advance: existing blocks rise one row while the new bottom row rises
        // from exactly where the waiting preview was drawn, so the hand-over is continuous.
        public void PlayAdvance(BoardState board)
        {
            if (AdvanceSeconds <= 0f || !isActiveAndEnabled || !Application.isPlaying)
            { Sync(board); PresentIncoming(board, false); AdvanceCompleted?.Invoke(); return; }
            StopAdvance();
            var entryY = preview ? preview.CentreY : waitingRowY;
            Sync(board);
            var targets = new Dictionary<int, Vector3>();
            foreach (var block in board.Blocks.Values) targets[block.Id] = new Vector3(block.Column + block.Width * 0.5f, block.Row + block.Height * 0.5f, 0);
            var starts = new Dictionary<int, Vector3>();
            var entering = new List<BlockView>();
            foreach (var pair in views)
            {
                if (!targets.TryGetValue(pair.Key, out var target)) continue;
                if (captured.TryGetValue(pair.Key, out var prior)) starts[pair.Key] = prior;
                else
                {
                    // Arriving blocks continue the waiting row: same column, same height, same silhouette.
                    starts[pair.Key] = new Vector3(target.x, entryY, 0);
                    pair.Value.BeginEntrance(preview ? preview.Tint : Color.white);
                    entering.Add(pair.Value);
                }
                pair.Value.transform.position = starts[pair.Key];
            }
            if (preview) preview.SetVisible(false);
            Physics2D.SyncTransforms();
            IsAdvancing = true;
            advance = StartCoroutine(Advance(board, targets, starts, entering, AdvanceSeconds));
        }
        System.Collections.IEnumerator Advance(BoardState board, Dictionary<int, Vector3> targets, Dictionary<int, Vector3> starts, List<BlockView> entering, float duration)
        {
            // Hold the start pose for one frame so the first rendered frame is the fully
            // hidden waiting pose, never part-way.
            yield return null;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration), eased = 1f - Mathf.Pow(1f - t, 3f);
                foreach (var pair in targets)
                    if (views.TryGetValue(pair.Key, out var view) && view) view.transform.position = Vector3.LerpUnclamped(starts[pair.Key], pair.Value, eased);
                foreach (var view in entering) if (view) view.SetEntrance(eased);
                Physics2D.SyncTransforms();
                yield return null;
            }
            foreach (var pair in targets) if (views.TryGetValue(pair.Key, out var view) && view) view.transform.position = pair.Value;
            foreach (var view in entering) if (view) { view.EndEntrance(); view.SetPresentationVisible(true); }
            Physics2D.SyncTransforms();
            PresentIncoming(board, false);
            advance = null; IsAdvancing = false;
            AdvanceCompleted?.Invoke();
        }
        void StopAdvance()
        {
            if (advance != null) { StopCoroutine(advance); advance = null; }
            var wasAdvancing = IsAdvancing;
            foreach (var view in views.Values) if (view) { view.EndEntrance(); if (wasAdvancing) view.SetPresentationVisible(true); }
            IsAdvancing = false;
        }
        public BlockView ApplyHit(int id, HitResult hit)
        {
            if (!hit.Accepted || !views.TryGetValue(id, out var view)) return null;
            if (hit.Destroyed) { views.Remove(id); view.Remove(); }
            else view.SetHealth(hit.Health, hit.IsTarget);
            return view;
        }
    }
}
