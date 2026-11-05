using System;

namespace Holedown
{
    public enum PlannerBallMode { BaseOnly, AllPreviousRewards, Manual }

    // Immutable test intent. Resolve by stable ID against each newly validated snapshot.
    public sealed class PlannerTestContext
    {
        public string StageId { get; }
        public PlannerBallMode Mode { get; }
        public int ManualBalls { get; }
        public PlannerTestContext(string stageId, PlannerBallMode mode, int manualBalls)
        {
            if(string.IsNullOrWhiteSpace(stageId)) throw new ArgumentException("stage.id: select a valid stage ID.");
            if(!Enum.IsDefined(typeof(PlannerBallMode),mode)) throw new ArgumentException("ball mode: unknown value.");
            if(mode==PlannerBallMode.Manual && manualBalls<=0) throw new ArgumentException("manual balls: expected positive Int32 integer.");
            StageId=stageId; Mode=mode; ManualBalls=manualBalls;
        }
        public (int index,int balls) Resolve(CampaignDefinition campaign,int baseBalls)
        {
            int index=-1;
            for(int i=0;i<campaign.Stages.Count;i++) if(campaign.Stages[i].Id==StageId){index=i;break;}
            if(index<0) throw new ArgumentException($"level.json.levels.id: tested stage '{StageId}' no longer exists.");
            long balls=Mode==PlannerBallMode.Manual?ManualBalls:baseBalls;
            if(Mode==PlannerBallMode.AllPreviousRewards) for(int i=0;i<index;i++) balls+=campaign.Stages[i].PossibleReward;
            if(balls<=0 || balls>int.MaxValue) throw new ArgumentException("entry balls: outside positive Int32 range.");
            return (index,(int)balls);
        }
    }
}
