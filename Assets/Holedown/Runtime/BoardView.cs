using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] BlockView blockPrefab;
        readonly Dictionary<int, BlockView> views = new Dictionary<int, BlockView>();
        public IReadOnlyDictionary<int, BlockView> Views => views;
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
        public BlockView ApplyHit(int id, HitResult hit)
        {
            if (!hit.Accepted || !views.TryGetValue(id, out var view)) return null;
            if (hit.Destroyed) { views.Remove(id); view.Remove(); }
            else view.SetHealth(hit.Health, hit.IsTarget);
            return view;
        }
    }
}
