using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Holedown.Tests
{
    public sealed class VerticalBlockRulesTests
    {
        static RowDefinition[] Rows()=>new[]{new RowDefinition{blocks=new List<BlockDefinition>{new BlockDefinition(6,0,1,4,true)}}};
        [Test] public void VerticalShapeRetainsFullGeometryAndMaximumHealth()
        {
            var definition=new BlockDefinition(2,4,1,6,false,0,2);
            var stage=new StageDefinition("vertical","Vertical",new[]{definition},Rows());
            var board=stage.CreateBoard();var block=board.Blocks[1];
            Assert.AreEqual(1,block.Width);Assert.AreEqual(2,block.Height);Assert.AreEqual(6,block.MaxHealth);
            board.ApplyHit(1);board.ApplyHit(1);board.Advance();
            Assert.AreEqual(4,block.Health);Assert.AreEqual(6,block.MaxHealth);Assert.AreEqual(5,block.Row);
            definition.height=1;definition.health=99;
            var retry=stage.CreateBoard().Blocks[1];Assert.AreEqual(2,retry.Height);Assert.AreEqual(6,retry.Health);Assert.AreEqual(6,retry.MaxHealth);
        }
        [TestCase(0,0)] [TestCase(0,1)] [TestCase(1,0)] [TestCase(1,1)]
        public void CompleteRectanglesAreCheckedRegardlessOfInputOrder(int top,int reverse)
        {
            var vertical=new BlockDefinition(3,2,1,4,false,0,2);var other=new BlockDefinition(2,2+top,2,1);
            var list=reverse==0?new[]{vertical,other}:new[]{other,vertical};
            StringAssert.Contains("overlaps",Assert.Throws<ArgumentException>(()=>new BoardState(list,Rows())).Message);
        }
        [Test] public void AdjacentRectanglesDoNotOverlap()
        {
            Assert.DoesNotThrow(()=>new BoardState(new[]{new BlockDefinition(3,2,1,4,false,0,2),new BlockDefinition(4,3,2,4),new BlockDefinition(3,4,1,4)},Rows()));
        }
        [Test] public void HighestOccupiedCellTriggersDangerNotAnchor()
        {
            var board=new BoardState(new[]{new BlockDefinition(3,5,1,4,false,0,2)},Rows());
            Assert.IsFalse(board.Advance());Assert.AreEqual(6,board.Blocks[1].Row);
            Assert.IsTrue(board.Advance());Assert.AreEqual(7,board.Blocks[1].Row);
        }
        [TestCase(1,3,0)] [TestCase(2,2,0)] [TestCase(1,2,7)] [TestCase(1,0,0)]
        public void InvalidShapesAndTopBoundaryAreRejected(int width,int height,int row)
        {
            Assert.Throws<ArgumentException>(()=>new BoardState(new[]{new BlockDefinition(0,row,width,3,false,0,height)},Rows()));
        }
        [Test] public void IncomingVerticalIsRejectedEvenWithoutOverlap()
        {
            var rows=Rows();rows[0].blocks[0]=new BlockDefinition(6,0,1,4,true,0,2);
            StringAssert.Contains("height",Assert.Throws<ArgumentException>(()=>new BoardState(Array.Empty<BlockDefinition>(),rows)).Message);
        }
    }
}
