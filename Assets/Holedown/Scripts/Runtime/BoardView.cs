using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] BlockView blockPrefab;
        [SerializeField, Tooltip("Smooth upward move for board advance; 0 disables the animation.")]
        [Min(0f)] float advanceSeconds = 0.2f;
        readonly Dictionary<int, BlockView> views = new Dictionary<int, BlockView>();
        readonly Dictionary<int, Vector3> captured = new Dictionary<int, Vector3>();
        Coroutine advance;
        public IReadOnlyDictionary<int, BlockView> Views => views;
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
        // Called before a settle so the view knows where blocks started.
        public void CapturePositions()
        {
            captured.Clear();
            foreach (var pair in views) if (pair.Value) captured[pair.Key] = pair.Value.transform.position;
        }
        // Plays the advance: existing blocks rise one row while the new bottom row slides in.
        public void PlayAdvance(BoardState board)
        {
            if (advanceSeconds <= 0f || !isActiveAndEnabled || !Application.isPlaying) { Sync(board); AdvanceCompleted?.Invoke(); return; }
            StopAdvance();
            Sync(board);
            var targets = new Dictionary<int, Vector3>();
            foreach (var block in board.Blocks.Values) targets[block.Id] = new Vector3(block.Column + block.Width * 0.5f, block.Row + block.Height * 0.5f, 0);
            var starts = new Dictionary<int, Vector3>();
            var entering = new List<BlockView>();
            foreach (var pair in views)
            {
                if (!targets.TryGetValue(pair.Key, out var target)) continue;
                if (captured.TryGetValue(pair.Key, out var start)) starts[pair.Key] = start;
                else
                {
                    // Blocks of the incoming row are hidden until they reach the bottom row.
                    starts[pair.Key] = target + Vector3.down;
                    pair.Value.SetPresentationVisible(false);
                    entering.Add(pair.Value);
                }
                pair.Value.transform.position = starts[pair.Key];
            }
            Physics2D.SyncTransforms();
            IsAdvancing = true;
            advance = StartCoroutine(Advance(targets, starts, entering));
        }
        System.Collections.IEnumerator Advance(Dictionary<int, Vector3> targets, Dictionary<int, Vector3> starts, List<BlockView> entering)
        {
            float elapsed = 0;
            while (elapsed < advanceSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / advanceSeconds), eased = 1f - Mathf.Pow(1f - t, 3f);
                foreach (var pair in targets)
                    if (views.TryGetValue(pair.Key, out var view) && view) view.transform.position = Vector3.LerpUnclamped(starts[pair.Key], pair.Value, eased);
                Physics2D.SyncTransforms();
                yield return null;
            }
            foreach (var pair in targets) if (views.TryGetValue(pair.Key, out var view) && view) view.transform.position = pair.Value;
            foreach (var view in entering) if (view) view.SetPresentationVisible(true);
            Physics2D.SyncTransforms();
            advance = null; IsAdvancing = false;
            AdvanceCompleted?.Invoke();
        }
        void StopAdvance()
        {
            if (advance != null) { StopCoroutine(advance); advance = null; }
            if (IsAdvancing) foreach (var view in views.Values) if (view) view.SetPresentationVisible(true);
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
