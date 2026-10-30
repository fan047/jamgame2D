using System;
using System.Collections.Generic;

namespace Holedown
{
    public enum SessionPhase { Initializing, Aiming, Volley, Recalling, Settling, Won, Lost }

    public sealed class GameSession
    {
        readonly HashSet<int> active = new HashSet<int>();
        int nextBallId;
        public Guid SessionId { get; } = Guid.NewGuid();
        public int VolleyId { get; private set; }
        public int Round => VolleyId;
        public int Pending { get; private set; }
        public int ActiveCount => active.Count;
        public SessionPhase Phase { get; private set; } = SessionPhase.Initializing;
        public bool CanSettle => (Phase == SessionPhase.Volley || Phase == SessionPhase.Recalling) && Pending == 0 && active.Count == 0;

        public void Ready()
        {
            if (Phase != SessionPhase.Initializing) throw new InvalidOperationException("Session already initialized.");
            Phase = SessionPhase.Aiming;
        }
        public bool TryFire(int count)
        {
            if (Phase != SessionPhase.Aiming || count <= 0) return false;
            VolleyId++;
            Pending = count;
            Phase = SessionPhase.Volley;
            return true;
        }
        public int RegisterLaunch()
        {
            if (Phase != SessionPhase.Volley || Pending <= 0) return 0;
            Pending--;
            int id = ++nextBallId;
            active.Add(id);
            return id;
        }
        public bool AcceptHit(Guid session, int volley, int ball)
            => Phase == SessionPhase.Volley && session == SessionId && volley == VolleyId && active.Contains(ball);
        public bool ReturnBall(Guid session, int volley, int ball)
        {
            if ((Phase != SessionPhase.Volley && Phase != SessionPhase.Recalling) || session != SessionId || volley != VolleyId) return false;
            return active.Remove(ball);
        }
        public bool Recall()
        {
            if (Phase != SessionPhase.Volley) return false;
            Phase = SessionPhase.Recalling;
            Pending = 0;
            return true;
        }
        public bool TryBeginSettlement()
        {
            if (!CanSettle) return false;
            Phase = SessionPhase.Settling;
            return true;
        }
        public void Settle(BoardState board)
        {
            if (Phase != SessionPhase.Settling) throw new InvalidOperationException("Settlement not authorized.");
            Phase = board.TargetDestroyed ? SessionPhase.Won : board.Advance() ? SessionPhase.Lost : SessionPhase.Aiming;
        }
    }
}
