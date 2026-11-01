using System;
using NUnit.Framework;

namespace Holedown.Tests
{
    public sealed class CampaignRulesTests
    {
        static HitResult Reward(int value=3)=>new HitResult(true,0,true,false,value);
        [Test] public void RewardOnlyOnDestructionAndOnlyOnce()
        {
            var board=new BoardState(new[]{new BlockDefinition(0,0,1,2,false,3)},new[]{new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(3,0,1,1,true)}}});
            var c=new CampaignState(3,12);
            var first=board.ApplyHit(1); Assert.AreEqual(0,first.RewardBalls); Assert.IsFalse(c.RecordHit(1,first));
            var hit=board.ApplyHit(1);Assert.AreEqual(3,hit.RewardBalls);Assert.IsTrue(c.RecordHit(1,hit));
            Assert.IsFalse(c.RecordHit(1,hit));Assert.AreEqual(0,board.ApplyHit(1).RewardBalls);
            Assert.AreEqual(3,c.PendingBonus);Assert.AreEqual(12,c.EntryBalls);
        }
        [TestCase(false,false,12,12)] [TestCase(true,false,15,15)] [TestCase(false,true,12,17)] [TestCase(true,true,15,20)]
        public void FourRewardRoutes(bool first,bool second,int stage2,int stage3)
        {
            var c=new CampaignState(3,12);
            if(first)c.RecordHit(1,Reward());Assert.IsTrue(c.CompleteStage());Assert.IsFalse(c.CompleteStage());Assert.IsTrue(c.NextStage());Assert.IsFalse(c.NextStage());Assert.AreEqual(stage2,c.EntryBalls);
            if(second)c.RecordHit(2,Reward(5));Assert.AreEqual(stage2,c.EntryBalls);c.CompleteStage();c.NextStage();Assert.AreEqual(stage3,c.EntryBalls);
            Assert.IsFalse(c.RecordHit(1,Reward()));c.CompleteStage();Assert.IsFalse(c.NextStage());
        }
        [Test] public void FailedAttemptsCannotFarmAndRestartClearsAllGrowth()
        {
            var c=new CampaignState(3,12);c.RecordHit(1,Reward());c.CompleteStage();c.NextStage();
            for(int i=0;i<10;i++){c.RecordHit(2,Reward(5));c.FailStage();Assert.IsFalse(c.CompleteStage());Assert.IsFalse(c.RecordHit(3,Reward()));Assert.IsTrue(c.RetryStage());Assert.IsFalse(c.RetryStage());Assert.AreEqual(15,c.EntryBalls);Assert.AreEqual(0,c.PendingBonus);Assert.AreEqual(3,c.ConfirmedBonus);}
            c.RestartRun();Assert.AreEqual(0,c.StageIndex);Assert.AreEqual(12,c.EntryBalls);Assert.AreEqual(0,c.ConfirmedBonus);
        }
        [Test] public void OverflowRejectedBeforeRewardStateChanges()
        {
            var c=new CampaignState(2,int.MaxValue-1);Assert.Throws<OverflowException>(()=>c.RecordHit(1,Reward()));Assert.AreEqual(0,c.PendingBonus);
        }
        [TestCase(2,0,false)] [TestCase(1,4,false)] [TestCase(1,3,true)]
        public void UnsupportedHeightRewardOrTargetCombinationRejected(int height,int reward,bool target)
        {
            Assert.Throws<ArgumentException>(()=>new BoardState(new[]{new BlockDefinition(0,0,1,1,target,reward,height)},new[]{new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(3,0,1,1,true)}}}));
        }
    }
}
