using System;
using System.Collections.Generic;

namespace Holedown
{
    [Serializable]
    public sealed class BlockDefinition
    {
        public int column;
        public int row;
        public int width = 1;
        public int health = 3;
        public bool target;
        public BlockDefinition() { }
        public BlockDefinition(int column, int row, int width, int health, bool target = false)
        { this.column = column; this.row = row; this.width = width; this.health = health; this.target = target; }
    }

    [Serializable]
    public sealed class RowDefinition
    {
        public List<BlockDefinition> blocks = new List<BlockDefinition>();
    }

    public sealed class BlockState
    {
        public int Id { get; }
        public int Column { get; }
        public int Row { get; internal set; }
        public int Width { get; }
        public int Health { get; internal set; }
        public bool IsTarget { get; }
        internal BlockState(int id, BlockDefinition definition, int row)
        { Id = id; Column = definition.column; Row = row; Width = definition.width; Health = definition.health; IsTarget = definition.target; }
    }

    public readonly struct HitResult
    {
        public readonly bool Accepted;
        public readonly int Health;
        public readonly bool Destroyed;
        public readonly bool IsTarget;
        public HitResult(bool accepted, int health, bool destroyed, bool target)
        { Accepted = accepted; Health = health; Destroyed = destroyed; IsTarget = target; }
    }

    public sealed class BoardState
    {
        readonly Dictionary<int, BlockState> blocks = new Dictionary<int, BlockState>();
        readonly List<RowDefinition> incoming = new List<RowDefinition>();
        int nextId;
        public IReadOnlyDictionary<int, BlockState> Blocks => blocks;
        public int IncomingCursor { get; private set; }
        public int IncomingCount => incoming.Count;
        public bool TargetDestroyed { get; private set; }

        public BoardState(IReadOnlyList<BlockDefinition> initial, IReadOnlyList<RowDefinition> rows)
        {
            Validate(initial, rows);
            foreach (var d in initial) Add(d, d.row);
            foreach (var row in rows)
            {
                var copy = new RowDefinition();
                foreach (var d in row.blocks) copy.blocks.Add(new BlockDefinition(d.column, 0, d.width, d.health, d.target));
                incoming.Add(copy);
            }
        }

        void Add(BlockDefinition d, int row) { var block = new BlockState(++nextId, d, row); blocks.Add(block.Id, block); }

        public HitResult ApplyHit(int id)
        {
            if (!blocks.TryGetValue(id, out var block)) return default;
            block.Health--;
            bool destroyed = block.Health == 0;
            if (destroyed)
            {
                if (block.IsTarget) TargetDestroyed = true;
                blocks.Remove(id);
            }
            return new HitResult(true, block.Health, destroyed, block.IsTarget);
        }

        public bool Advance()
        {
            foreach (var block in blocks.Values) block.Row++;
            if (IncomingCursor < incoming.Count)
            {
                foreach (var d in incoming[IncomingCursor].blocks) Add(d, 0);
                IncomingCursor++;
            }
            foreach (var block in blocks.Values) if (block.Row >= 8) return true;
            return false;
        }

        public static void Validate(IReadOnlyList<BlockDefinition> initial, IReadOnlyList<RowDefinition> rows)
        {
            if (initial == null || rows == null || rows.Count == 0) throw new ArgumentException("level.json: initialBlocks and at least one incomingRows entry are required.");
            int targets = 0;
            var occupied = new HashSet<int>();
            for(int i=0;i<initial.Count;i++)
            {
                var d=initial[i]; string path=$"level.json.initialBlocks[{i}]";
                ValidateBlock(d, false, occupied,path);
                if (d.target) throw new ArgumentException(path+".target: target must be in the last incoming row.");
            }
            for (int i = 0; i < rows.Count; i++)
            {
                string path=$"level.json.incomingRows[{i}]";
                if (rows[i]?.blocks == null) throw new ArgumentException(path+": missing blocks array.");
                occupied.Clear();
                for(int j=0;j<rows[i].blocks.Count;j++)
                {
                    var d=rows[i].blocks[j]; string blockPath=$"{path}.blocks[{j}]";
                    ValidateBlock(d, true, occupied,blockPath);
                    if (!d.target) continue;
                    targets++;
                    if (i != rows.Count - 1) throw new ArgumentException(blockPath+".target: target must be in the last incoming row.");
                }
            }
            if (targets != 1) throw new ArgumentException($"level.json: expected exactly one target in the last incoming row; found {targets}.");
        }

        static void ValidateBlock(BlockDefinition d, bool incoming, HashSet<int> occupied,string path)
        {
            if(d==null) throw new ArgumentException(path+": missing block.");
            if(d.column<0 || d.column>7) throw new ArgumentException(path+".column: expected 0-7.");
            if(d.width!=1 && d.width!=2) throw new ArgumentException(path+".width: expected 1 or 2.");
            if(d.column+d.width>8) throw new ArgumentException(path+": column + width exceeds 8 columns.");
            if(d.health<=0) throw new ArgumentException(path+".health: expected positive integer.");
            if(d.row<0 || d.row>=8 || (incoming && d.row!=0)) throw new ArgumentException(path+".row: "+(incoming?"incoming blocks must use row 0.":"expected 0-7."));
            for (int c = d.column; c < d.column + d.width; c++)
                if (!occupied.Add(d.row * 8 + c)) throw new ArgumentException(path+$": overlaps another block at column {c}, row {d.row}.");
        }
    }
}
