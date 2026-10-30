using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    [CreateAssetMenu(menuName = "Holedown/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public List<BlockDefinition> initialBlocks = new List<BlockDefinition>();
        public List<RowDefinition> incomingRows = new List<RowDefinition>();
        public BoardState CreateBoard() => new BoardState(initialBlocks, incomingRows);
    }
}
