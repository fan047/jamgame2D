using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] GameSettings settings;
        [SerializeField] LevelDefinition level;
        [SerializeField] AimInput aim;
        [SerializeField] BoardView boardView;
        [SerializeField] BallActor ballPrefab;
        [SerializeField, Tooltip("Optional JSON folder. Empty: project/Config in Editor; executable/Config in Player.")]
        string externalConfigDirectory;
        LoadedConfiguration configuration;
        readonly Dictionary<int, BallActor> balls = new Dictionary<int, BallActor>();
        Coroutine launching;
        float volleyStarted;
        public GameSession Session { get; private set; }
        public BoardState Board { get; private set; }
        public string InitializationError { get; private set; }
        public event Action<BlockView, HitResult> BlockHit;
        public event Action Changed;
        public event Action AttemptReset;
        public CampaignState Campaign { get; private set; }
        bool transitioning;
        int transitionFrame = -1;
        public bool IsTransitioning => transitioning;
        public GameSettings Settings => configuration?.Settings ?? settings;
        public string ConfigurationLabel => configuration?.Label ?? "Not loaded";
        public string ConfigurationDirectory { get; private set; }
        void OnDestroy() { configuration?.Dispose(); }

        void OnEnable() { aim.FireRequested += OnFireRequested; }
        void OnDisable()
        {
            aim.FireRequested -= OnFireRequested;
            ClearAttempt();
        }
        void ClearAttempt()
        {
            aim.CanAim=false;
            StopAllCoroutines(); launching=null;
            foreach(var ball in balls.Values) if(ball){ball.Hit-=OnHit;ball.Returned-=OnReturned;ball.gameObject.SetActive(false);Destroy(ball.gameObject);}
            balls.Clear();
            AttemptReset?.Invoke();
            if(boardView) boardView.Clear();
        }
        void BeginAttempt()
        {
            ClearAttempt();
            Session=new GameSession(); Board=configuration.CreateBoard(Campaign.StageIndex);
            aim.Configure(Settings);boardView.Sync(Board);Session.Ready();aim.CanAim=true;
            Changed?.Invoke();
        }
        void Start()
        {
            Session = new GameSession();
            try
            {
                ConfigurationDirectory = !string.IsNullOrWhiteSpace(externalConfigDirectory)
                    ? System.IO.Path.GetFullPath(externalConfigDirectory)
                    : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),"Config");
                configuration = ExternalConfiguration.Load(ConfigurationDirectory,settings,level);
                Campaign=new CampaignState(configuration.Campaign.Stages.Count,Settings.ballsPerRound);
                BeginAttempt();
            }
            catch (Exception exception)
            {
                InitializationError = exception.Message; aim.CanAim = false;
                Debug.LogError("Initialization failed: " + exception.Message, this);
            }
            Changed?.Invoke();
        }
        void OnFireRequested(Vector2 direction) => TryFire(direction);
        public bool TryFire(Vector2 direction)
        {
            if (transitioning || Campaign == null || Session == null || direction.y >= 0 || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude < 0.0001f) return false;
            float angle = Mathf.Clamp(Mathf.Atan2(direction.x,-direction.y)*Mathf.Rad2Deg,-Settings.maxAimAngle,Settings.maxAimAngle)*Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));
            if (!Session.TryFire(Campaign.EntryBalls)) return false;
            aim.CanAim = false; volleyStarted = Time.time;
            launching = StartCoroutine(LaunchVolley(Session.SessionId,Session.VolleyId,direction));
            Changed?.Invoke(); return true;
        }
        IEnumerator LaunchVolley(Guid session, int volley, Vector2 direction)
        {
            var interval = new WaitForSeconds(Settings.launchInterval);
            while (Session.SessionId == session && Session.VolleyId == volley && Session.Phase == SessionPhase.Volley && Session.Pending > 0)
            {
                int id = Session.RegisterLaunch();
                var ball = Instantiate(ballPrefab, AimInput.Origin, Quaternion.identity);
                balls.Add(id,ball); ball.Hit += OnHit; ball.Returned += OnReturned;
                ball.Launch(Settings,session,volley,id,direction);
                Changed?.Invoke();
                yield return interval;
            }
            launching = null;
        }
        void OnHit(BallActor ball, int blockId)
        {
            if (!Session.AcceptHit(ball.SessionId,ball.VolleyId,ball.BallId)) return;
            var hit = Board.ApplyHit(blockId);
            if (!hit.Accepted) return;
            Campaign.RecordHit(blockId,hit);
            var view = boardView.ApplyHit(blockId,hit);
            BlockHit?.Invoke(view,hit);
            Changed?.Invoke();
        }
        void OnReturned(BallActor ball)
        {
            if (!Session.ReturnBall(ball.SessionId,ball.VolleyId,ball.BallId)) return;
            balls.Remove(ball.BallId); ball.Hit -= OnHit; ball.Returned -= OnReturned;
            Changed?.Invoke();
        }
        public void RequestRecall()
        {
            if (Session == null || !Session.Recall()) return;
            if (launching != null) { StopCoroutine(launching); launching = null; }
            var returning = new List<BallActor>(balls.Values);
            foreach (var ball in returning) ball.Return();
            Changed?.Invoke();
        }
        bool EnterTransition()
        {
            if(transitioning || transitionFrame==Time.frameCount)return false;
            transitioning=true;transitionFrame=Time.frameCount;return true;
        }
        public void NextStage()
        {
            if(Session?.Phase!=SessionPhase.Won || Campaign==null || Campaign.IsFinalStage || !EnterTransition())return;
            try{if(Campaign.NextStage())BeginAttempt();}finally{transitioning=false;}
        }
        public void RetryStage()
        {
            if(Session?.Phase!=SessionPhase.Lost || Campaign==null || !EnterTransition())return;
            try{if(Campaign.RetryStage())BeginAttempt();}finally{transitioning=false;}
        }
        public void RestartRun()
        {
            if(Campaign==null || !EnterTransition())return;
            try{Campaign.RestartRun();BeginAttempt();}finally{transitioning=false;}
        }
        public void Restart() => ReloadAndRestart();
        public void ReloadAndRestart()
        {
            if(!EnterTransition())return;
            ClearAttempt();
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.path);
        }
        void Update()
        {
            if (Session != null && Session.Phase == SessionPhase.Volley && Time.time-volleyStarted >= Settings.recallTimeout) RequestRecall();
        }
        void LateUpdate()
        {
            if (Session == null) return;
            switch (Session.Phase)
            {
                case SessionPhase.Volley:
                case SessionPhase.Recalling:
                    if (!Session.TryBeginSettlement()) break;
                    Session.Settle(Board);
                    if(Session.Phase==SessionPhase.Won)Campaign.CompleteStage();
                    else if(Session.Phase==SessionPhase.Lost)Campaign.FailStage();
                    boardView.Sync(Board);
                    aim.CanAim = Session.Phase == SessionPhase.Aiming;
                    Changed?.Invoke();
                    break;
            }
        }
    }
}
