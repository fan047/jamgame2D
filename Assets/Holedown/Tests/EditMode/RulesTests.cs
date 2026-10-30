using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class RulesTests
    {
        static List<RowDefinition> Rows() => new List<RowDefinition> { new RowDefinition { blocks = new List<BlockDefinition> { new BlockDefinition(3, 0, 2, 1, true) } } };
        static BoardState Board(int row = 0) => new BoardState(new[] { new BlockDefinition(0, row, 2, 2) }, Rows());
        static GameSession Session(int count = 1)
        { var s = new GameSession(); s.Ready(); Assert.IsTrue(s.TryFire(count)); return s; }

        [Test] public void DamageIsIdempotentAfterDestruction()
        {
            var b = Board();
            Assert.AreEqual(1, b.ApplyHit(1).Health);
            Assert.IsTrue(b.ApplyHit(1).Destroyed);
            Assert.IsFalse(b.ApplyHit(1).Accepted);
        }
        [Test] public void AdvanceCopiesDataAndExhaustsIncomingRows()
        {
            var d = new BlockDefinition(0, 0, 2, 2);
            var rows = Rows();
            var b = new BoardState(new[] { d }, rows);
            d.health = 100; rows[0].blocks[0].health = 100;
            b.Advance();
            Assert.AreEqual(1, b.Blocks[1].Row);
            Assert.AreEqual(2, b.Blocks[1].Health);
            Assert.AreEqual(1, b.Blocks[2].Health);
            b.Advance(); Assert.AreEqual(2, b.Blocks.Count); Assert.AreEqual(1, b.IncomingCursor);
        }
        [Test] public void DangerStartsAtRowEight()
        { var b = Board(6); Assert.IsFalse(b.Advance()); Assert.AreEqual(7, b.Blocks[1].Row); Assert.IsTrue(b.Advance()); }
        [Test] public void PendingBallsPreventEarlySettlement()
        {
            var s = Session(2); int id = s.RegisterLaunch();
            Assert.IsTrue(s.ReturnBall(s.SessionId, s.VolleyId, id));
            Assert.IsFalse(s.CanSettle); Assert.IsFalse(s.TryBeginSettlement());
        }
        [Test] public void RecallAndReturnAreIdempotent()
        {
            var s = Session(12); int id = s.RegisterLaunch();
            Assert.IsTrue(s.Recall()); Assert.IsFalse(s.Recall()); Assert.AreEqual(0, s.Pending);
            Assert.AreEqual(0, s.RegisterLaunch());
            Assert.IsTrue(s.ReturnBall(s.SessionId, s.VolleyId, id)); Assert.IsFalse(s.ReturnBall(s.SessionId, s.VolleyId, id));
            Assert.IsTrue(s.TryBeginSettlement()); Assert.IsFalse(s.TryBeginSettlement());
            s.Settle(Board()); Assert.AreEqual(SessionPhase.Aiming, s.Phase);
            Assert.Throws<InvalidOperationException>(() => s.Settle(Board()));
        }
        [Test] public void StaleAndUnknownEventsAreRejected()
        {
            var s = Session(); int id = s.RegisterLaunch();
            Assert.IsFalse(s.AcceptHit(Guid.NewGuid(), s.VolleyId, id));
            Assert.IsFalse(s.AcceptHit(s.SessionId, s.VolleyId - 1, id));
            Assert.IsFalse(s.AcceptHit(s.SessionId, s.VolleyId, id + 1));
            Assert.IsFalse(s.ReturnBall(s.SessionId, s.VolleyId - 1, id));
            Assert.IsTrue(s.AcceptHit(s.SessionId, s.VolleyId, id));
            s.Recall(); Assert.IsFalse(s.AcceptHit(s.SessionId, s.VolleyId, id));
        }
        [Test] public void TargetVictoryPrecedesLosingAdvance()
        {
            var b = Board(6); b.Advance(); b.ApplyHit(2);
            var s = Session(); s.Recall(); Assert.IsTrue(s.TryBeginSettlement()); s.Settle(b);
            Assert.AreEqual(SessionPhase.Won, s.Phase); Assert.AreEqual(7, b.Blocks[1].Row);
        }
        [Test] public void OrdinaryClearDoesNotWin()
        {
            var b = Board(); b.ApplyHit(1); b.ApplyHit(1);
            var s = Session(); s.Recall(); s.TryBeginSettlement(); s.Settle(b);
            Assert.AreEqual(SessionPhase.Aiming, s.Phase);
        }
        [TestCase(-1, 0, 1, 1)] [TestCase(7, 0, 2, 1)] [TestCase(0, 8, 1, 1)]
        [TestCase(0, 0, 3, 1)] [TestCase(0, 0, 1, 0)]
        public void InvalidBlockRejected(int c, int r, int w, int h)
            => Assert.Throws<ArgumentException>(() => new BoardState(new[] { new BlockDefinition(c, r, w, h) }, Rows()));
        [Test] public void OverlapRejected()
            => Assert.Throws<ArgumentException>(() => new BoardState(new[] { new BlockDefinition(0, 0, 2, 1), new BlockDefinition(1, 0, 1, 1) }, Rows()));
        [Test] public void TargetMustBeUniqueAndLast()
        {
            var rows = Rows(); rows.Add(new RowDefinition());
            Assert.Throws<ArgumentException>(() => new BoardState(Array.Empty<BlockDefinition>(), rows));
            rows = Rows(); rows[0].blocks.Add(new BlockDefinition(0, 0, 1, 1, true));
            Assert.Throws<ArgumentException>(() => new BoardState(Array.Empty<BlockDefinition>(), rows));
        }
        [Test] public void SettingsRejectInvalidValues()
        {
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            try { settings.Validate(); settings.ballSpeed = float.NaN; Assert.Throws<ArgumentException>(settings.Validate); }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
    }
}
