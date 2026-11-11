using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Holedown
{
    public sealed class StageDefinition
    {
        readonly List<BlockDefinition> initial = new List<BlockDefinition>();
        readonly List<RowDefinition> rows = new List<RowDefinition>();
        public string Id { get; }
        public string Name { get; }
        public int PossibleReward { get; }
        public StageDefinition(string id,string name,IReadOnlyList<BlockDefinition> initialBlocks,IReadOnlyList<RowDefinition> incomingRows)
        {
            if(!Regex.IsMatch(id??"","^[A-Za-z0-9._-]{1,32}$")) throw new ArgumentException("id: expected 1-32 letters, digits, dots, underscores or hyphens.");
            if(string.IsNullOrWhiteSpace(name) || name.Length>40) throw new ArgumentException("name: expected 1-40 characters.");
            BoardState.Validate(initialBlocks,incomingRows);
            Id=id;Name=name;
            long possibleReward = 0;
            foreach(var d in initialBlocks){initial.Add(Copy(d));possibleReward+=d.rewardBalls;}
            foreach(var row in incomingRows)
            {
                var copy=new RowDefinition();foreach(var d in row.blocks){copy.blocks.Add(Copy(d));possibleReward+=d.rewardBalls;}rows.Add(copy);
            }
            if(possibleReward>int.MaxValue) throw new ArgumentException("rewardBalls: stage reward sum exceeds Int32 maximum.");
            PossibleReward=(int)possibleReward;
        }
        static BlockDefinition Copy(BlockDefinition d)=>new BlockDefinition(d.column,d.row,d.width,d.health,d.target,d.rewardBalls,d.height);
        public BoardState CreateBoard(GameDifficulty difficulty = GameDifficulty.Normal) => new BoardState(initial, rows, difficulty);
    }

    public sealed class CampaignDefinition
    {
        public ReadOnlyCollection<StageDefinition> Stages { get; }
        public CampaignDefinition(IList<StageDefinition> stages,int baseBalls)
        {
            if(stages==null || stages.Count==0)throw new ArgumentException("level.json.levels: expected at least one stage.");
            var ids=new HashSet<string>();long maximum=baseBalls;
            for(int i=0;i<stages.Count;i++)
            {
                var stage=stages[i];
                if(stage==null || !ids.Add(stage.Id))throw new ArgumentException($"level.json.levels[{i}].id: missing or duplicate stage id.");
                if(i==stages.Count-1 && stage.PossibleReward>0)throw new ArgumentException($"level.json.levels[{i}]: final stage cannot contain reward blocks.");
                maximum+=stage.PossibleReward;
                if(maximum>int.MaxValue)throw new ArgumentException("level.json.levels: base balls plus possible rewards exceed Int32 maximum.");
            }
            Stages=new List<StageDefinition>(stages).AsReadOnly();
        }
        public BoardState CreateBoard(int index, GameDifficulty difficulty = GameDifficulty.Normal)
        {
            if(index<0 || index>=Stages.Count)throw new ArgumentOutOfRangeException(nameof(index));
            return Stages[index].CreateBoard(difficulty);
        }
    }
}
