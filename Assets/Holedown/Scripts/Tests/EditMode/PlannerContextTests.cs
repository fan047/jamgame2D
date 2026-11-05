using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Holedown.Tests
{
    public sealed class PlannerContextTests
    {
        static CampaignDefinition Campaign()
        {
            var stages=new List<StageDefinition>();
            for(int i=0;i<9;i++)stages.Add(new StageDefinition("s"+i,"Stage "+i,
                i<8?new[]{new BlockDefinition(0,0,1,1,false,i+1)}:new BlockDefinition[0],
                new[]{new RowDefinition{blocks=new List<BlockDefinition>{new BlockDefinition(7,0,1,1,true)}}}));
            return new CampaignDefinition(stages,12);
        }
        [Test] public void ThreeEntryModesUseRealOrderAndKeepManualIndependent()
        {
            var c=Campaign();
            Assert.AreEqual((6,12),new PlannerTestContext("s6",PlannerBallMode.BaseOnly,0).Resolve(c,12));
            Assert.AreEqual((6,33),new PlannerTestContext("s6",PlannerBallMode.AllPreviousRewards,0).Resolve(c,12));
            Assert.AreEqual((6,123),new PlannerTestContext("s6",PlannerBallMode.Manual,123).Resolve(c,7));
            Assert.Throws<ArgumentException>(()=>new PlannerTestContext("missing",PlannerBallMode.BaseOnly,0).Resolve(c,12));
            Assert.Throws<ArgumentException>(()=>new PlannerTestContext("s6",PlannerBallMode.Manual,0));
            Assert.Throws<ArgumentException>(()=>new PlannerTestContext("s6",(PlannerBallMode)9,1));
            var state=new CampaignState(9,12,6);Assert.AreEqual(6,state.StageIndex);Assert.IsFalse(state.IsFinalStage);Assert.AreEqual(12,state.EntryBalls);
        }
    }
}
