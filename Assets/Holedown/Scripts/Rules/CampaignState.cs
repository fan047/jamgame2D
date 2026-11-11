using System;
using System.Collections.Generic;

namespace Holedown
{
    public sealed class CampaignState
    {
        readonly HashSet<int> rewardedBlocks = new HashSet<int>();
        int normalFailures;
        public int StageCount { get; }
        public int BaseBalls { get; }
        public int StageIndex { get; private set; }
        public int EntryBalls { get; private set; }
        public int ConfirmedBonus { get; private set; }
        public int PendingBonus { get; private set; }
        public bool StageCompleted { get; private set; }
        public bool StageFailed { get; private set; }
        public bool OffersEasyRetry { get; private set; }
        public bool IsFinalStage => StageIndex == StageCount - 1;
        public int NextEntryBalls => checked(BaseBalls + ConfirmedBonus);

        public CampaignState(int stageCount, int baseBalls, int stageIndex = 0)
        {
            if(stageCount<=0 || baseBalls<=0 || stageIndex<0 || stageIndex>=stageCount) throw new ArgumentException("Positive stage count and base balls, and a valid stage index required.");
            StageCount=stageCount; BaseBalls=baseBalls; EntryBalls=baseBalls; StageIndex=stageIndex;
        }
        public bool RecordHit(int blockId, HitResult hit)
        {
            if(StageCompleted || StageFailed || IsFinalStage || !hit.Accepted || !hit.Destroyed || hit.RewardBalls<=0 || rewardedBlocks.Contains(blockId)) return false;
            int value=checked(PendingBonus+hit.RewardBalls);
            checked { _ = BaseBalls+ConfirmedBonus+value; }
            rewardedBlocks.Add(blockId); PendingBonus=value; return true;
        }
        public bool CompleteStage()
        {
            if(StageCompleted || StageFailed) return false;
            ConfirmedBonus=checked(ConfirmedBonus+PendingBonus);
            StageCompleted=true; return true;
        }
        public bool FailStage(bool countNormalFailure = false)
        {
            if(StageCompleted || StageFailed) return false;
            StageFailed = true;
            if (countNormalFailure && normalFailures < 3)
                OffersEasyRetry = ++normalFailures == 3;
            return true;
        }
        public void DismissEasyRetry() => OffersEasyRetry = false;
        public bool RetryStage()
        {
            if(!StageFailed) return false;
            return RestartStage();
        }
        public bool RestartStage()
        {
            if (StageCompleted) return false;
            ResetAttempt(); return true;
        }
        public bool NextStage()
        {
            if(!StageCompleted || IsFinalStage) return false;
            normalFailures = 0;
            StageIndex++; EntryBalls=NextEntryBalls; ResetAttempt(); return true;
        }
        public void RestartRun()
        {
            normalFailures = 0;
            StageIndex=0; EntryBalls=BaseBalls; ConfirmedBonus=0; ResetAttempt();
        }
        void ResetAttempt()
        {
            OffersEasyRetry = false;
            PendingBonus=0; StageCompleted=false; StageFailed=false; rewardedBlocks.Clear();
        }
    }
}
