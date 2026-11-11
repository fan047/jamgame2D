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
        [Test]
        public void MultipleCustomRewardsSumOnlyActualDestructionsAndConfirmNextStage()
        {
            var blocks=new[]{new BlockDefinition(0,0,1,1,false,3),new BlockDefinition(1,0,1,1,false,5),new BlockDefinition(2,0,1,1,false,8),new BlockDefinition(3,0,1,1,false,11)};
            var rows=new[]{new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(7,0,1,1,true)}}};
            var stage=new StageDefinition("custom","Custom",blocks,rows); Assert.AreEqual(27,stage.PossibleReward);
            var board=stage.CreateBoard(); var state=new CampaignState(9,12);
            foreach(int id in new[]{2,4}) {var hit=board.ApplyHit(id);Assert.IsTrue(state.RecordHit(id,hit));Assert.IsFalse(state.RecordHit(id,hit));}
            Assert.AreEqual(16,state.PendingBonus);Assert.AreEqual(12,state.EntryBalls);
            state.FailStage();state.RetryStage();Assert.AreEqual(0,state.PendingBonus);
            board=stage.CreateBoard();for(int id=1;id<=4;id++)state.RecordHit(id,board.ApplyHit(id));
            state.CompleteStage();state.NextStage();Assert.AreEqual(39,state.EntryBalls);
            Assert.Throws<ArgumentException>(()=>new StageDefinition("overflow","Overflow",new[]{new BlockDefinition(0,0,1,1,false,int.MaxValue),new BlockDefinition(1,0,1,1,false,1)},rows));
            var final=new StageDefinition("final","Final",new BlockDefinition[0],rows);
            Assert.Throws<ArgumentException>(()=>new CampaignDefinition(new[]{stage,final},int.MaxValue-26));
            Assert.Throws<ArgumentException>(()=>new CampaignDefinition(new[]{stage},12));
        }
        [Test] public void OverflowRejectedBeforeRewardStateChanges()
        {
            var c=new CampaignState(2,int.MaxValue-1);Assert.Throws<OverflowException>(()=>c.RecordHit(1,Reward()));Assert.AreEqual(0,c.PendingBonus);
        }
        [Test]
        public void EasyDifficultyScalesStartingBlocksAndFutureRowsFromOriginalHealth()
        {
            var initial = new[]
            {
                new BlockDefinition(0, 0, 1, int.MaxValue),
                new BlockDefinition(1, 0, 1, 10, false, 3),
                new BlockDefinition(2, 0, 1, 2)
            };
            var row = new RowDefinition();
            row.blocks.Add(new BlockDefinition(0, 0, 1, 1));
            row.blocks.Add(new BlockDefinition(1, 0, 1, 3, true));
            var stage = new StageDefinition("difficulty", "Difficulty", initial, new[] { row });

            var easy = stage.CreateBoard(GameDifficulty.Easy);
            Assert.AreEqual(1717986918, easy.Blocks[1].MaxHealth);
            Assert.AreEqual(8, easy.Blocks[2].Health);
            Assert.AreEqual(3, easy.Blocks[2].RewardBalls);
            Assert.AreEqual(2, easy.Blocks[3].Health);
            easy.Advance();
            Assert.AreEqual(1, easy.Blocks[4].Health);
            Assert.AreEqual(2, easy.Blocks[5].MaxHealth);
            Assert.IsTrue(easy.Blocks[5].IsTarget);

            var normal = stage.CreateBoard();
            normal.Advance();
            Assert.AreEqual(10, normal.Blocks[2].Health);
            Assert.AreEqual(3, normal.Blocks[5].Health);
        }

        [Test]
        public void RestartKeepsGrowthAndOnlyTheThirdNormalFailureOffersEasyRetry()
        {
            var campaign = new CampaignState(3, 12);
            campaign.RecordHit(1, Reward());
            campaign.CompleteStage();
            campaign.NextStage();
            campaign.RecordHit(1, Reward(5));
            Assert.IsTrue(campaign.RestartStage());
            Assert.AreEqual(1, campaign.StageIndex);
            Assert.AreEqual(15, campaign.EntryBalls);
            Assert.AreEqual(3, campaign.ConfirmedBonus);
            Assert.AreEqual(0, campaign.PendingBonus);

            campaign.FailStage(); // A failure in easy mode does not count.
            campaign.RetryStage();
            for (int count = 1; count <= 4; count++)
            {
                Assert.IsTrue(campaign.FailStage(countNormalFailure: true));
                Assert.IsFalse(campaign.FailStage(countNormalFailure: true));
                Assert.AreEqual(count == 3, campaign.OffersEasyRetry);
                if (count == 3) campaign.DismissEasyRetry();
                Assert.IsTrue(campaign.RetryStage());
                Assert.IsFalse(campaign.OffersEasyRetry);
                if (count == 1) Assert.IsTrue(campaign.RestartStage());
            }
            campaign.CompleteStage();
            campaign.NextStage();
            campaign.FailStage(countNormalFailure: true);
            Assert.IsFalse(campaign.OffersEasyRetry);
        }
        [TestCase(3,0,false)] [TestCase(1,-1,false)] [TestCase(1,3,true)]
        public void UnsupportedHeightRewardOrTargetCombinationRejected(int height,int reward,bool target)
        {
            Assert.Throws<ArgumentException>(()=>new BoardState(new[]{new BlockDefinition(0,0,1,1,target,reward,height)},new[]{new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(3,0,1,1,true)}}}));
        }
    }
}
