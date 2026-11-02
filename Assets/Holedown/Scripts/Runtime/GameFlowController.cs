using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    public enum AppPhase { Loading, Title, Playing, Paused, ConfigError }
    public enum FlowCommand { Start, Continue, Recall, NextStage, RetryStage, RestartRun, Reload }

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
        public AppPhase Phase { get; private set; } = AppPhase.Loading;
        public bool HasFocus { get; private set; } = true;
        public bool CanSimulate => Phase == AppPhase.Playing && HasFocus;
        public int InputGeneration { get; private set; }
        public event Action<bool> PauseChanged;
        bool hasStarted, ownsPause;
        float savedTimeScale;
        PlayerInputController input;
        FlowCommand? pendingCommand;
        Vector2? pendingFire;
        int pendingGeneration;
        public bool UserInputAllowed => HasFocus && (input == null || input.AcceptsCommands);
        void OnDestroy() { ReleasePause(); configuration?.Dispose(); }

        void OnEnable() { aim.FireRequested += OnFireRequested; input = GetComponent<PlayerInputController>(); }
        void OnDisable()
        {
            aim.FireRequested -= OnFireRequested;
            ReleasePause();
            ClearAttempt();
        }
        void InvalidateInput()
        {
            InputGeneration++; pendingCommand = null; pendingFire = null;
            if(input) input.ResetGestures();
            if(UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        void SetPhase(AppPhase phase)
        {
            Phase = phase; InvalidateInput();
            aim.CanAim = CanSimulate && Session?.Phase == SessionPhase.Aiming;
            if(!aim.CanAim) aim.HidePreview();
            Changed?.Invoke();
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
            aim.Configure(Settings);boardView.Sync(Board);Session.Ready();
            SetPhase(AppPhase.Playing);
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
                if(hasStarted) BeginAttempt(); else SetPhase(AppPhase.Title);
            }
            catch (Exception exception)
            {
                InitializationError = exception.Message; aim.CanAim = false;
                SetPhase(AppPhase.ConfigError);
                Debug.LogError("Initialization failed: " + exception.Message, this);
            }
            Changed?.Invoke();
        }
        void OnFireRequested(Vector2 direction)
        {
            if(!UserInputAllowed || pendingCommand.HasValue) return;
            pendingFire = direction; pendingGeneration = InputGeneration;
        }
        public void QueueCommand(FlowCommand command)
        {
            if(!UserInputAllowed) return;
            // UI commands outrank gameplay requests, but never overwrite another UI command.
            if(pendingCommand.HasValue && pendingCommand != FlowCommand.Recall) return;
            pendingCommand = command; pendingFire = null; pendingGeneration = InputGeneration;
        }
        void DispatchInput()
        {
            var command = pendingCommand; var fire = pendingFire; int generation = pendingGeneration;
            pendingCommand = null; pendingFire = null;
            if(generation != InputGeneration || !UserInputAllowed) return;
            if(command.HasValue) switch(command.Value)
            {
                case FlowCommand.Start: StartGame(); break;
                case FlowCommand.Continue: ContinueGame(); break;
                case FlowCommand.Recall: RequestRecall(); break;
                case FlowCommand.NextStage: NextStage(); break;
                case FlowCommand.RetryStage: RetryStage(); break;
                case FlowCommand.RestartRun: RestartRun(); break;
                case FlowCommand.Reload: ReloadAndRestart(); break;
            }
            else if(fire.HasValue) TryFire(fire.Value);
        }
        public void StartGame()
        {
            if(Phase != AppPhase.Title || configuration == null || !UserInputAllowed || !EnterTransition()) return;
            try { hasStarted = true; BeginAttempt(); } finally { transitioning = false; }
        }
        public bool TryFire(Vector2 direction)
        {
            if (!CanSimulate || !UserInputAllowed || transitionFrame == Time.frameCount || transitioning || Campaign == null || Session == null || direction.y >= 0 || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude < 0.0001f) return false;
            float angle = Mathf.Clamp(Mathf.Atan2(direction.x,-direction.y)*Mathf.Rad2Deg,-Settings.maxAimAngle,Settings.maxAimAngle)*Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));
            if (!Session.TryFire(Campaign.EntryBalls)) return false;
            aim.CanAim = false; InvalidateInput(); volleyStarted = Time.time;
            launching = StartCoroutine(LaunchVolley(Session.SessionId,Session.VolleyId,direction));
            Changed?.Invoke(); return true;
        }
        IEnumerator LaunchVolley(Guid session, int volley, Vector2 direction)
        {
            var interval = new WaitForSeconds(Settings.launchInterval);
            while (Session.SessionId == session && Session.VolleyId == volley && Session.Phase == SessionPhase.Volley && Session.Pending > 0)
            {
                while(!CanSimulate) yield return null;
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
            if (!CanSimulate || Session == null || !Session.AcceptHit(ball.SessionId,ball.VolleyId,ball.BallId)) return;
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
            if (!CanSimulate || !UserInputAllowed || transitionFrame == Time.frameCount || Session == null || !Session.Recall()) return;
            InvalidateInput();
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
            if(!CanSimulate || !UserInputAllowed || Session?.Phase!=SessionPhase.Won || Campaign==null || Campaign.IsFinalStage || !EnterTransition())return;
            try{if(Campaign.NextStage())BeginAttempt();}finally{transitioning=false;}
        }
        public void RetryStage()
        {
            if(!CanSimulate || !UserInputAllowed || Session?.Phase!=SessionPhase.Lost || Campaign==null || !EnterTransition())return;
            try{if(Campaign.RetryStage())BeginAttempt();}finally{transitioning=false;}
        }
        public void RestartRun()
        {
            if(!CanSimulate || !UserInputAllowed || Campaign==null || !EnterTransition())return;
            try{Campaign.RestartRun();BeginAttempt();}finally{transitioning=false;}
        }
        public void Restart() => ReloadAndRestart();
        public void ReloadAndRestart()
        {
            if(!UserInputAllowed || Phase == AppPhase.Loading || !EnterTransition())return;
            try
            {
                SetPhase(AppPhase.Loading); ClearAttempt(); ReleasePause();
                configuration?.Dispose(); configuration = null; Campaign = null; Board = null;
                Session = new GameSession(); InitializationError = null;
                configuration = ExternalConfiguration.Load(ConfigurationDirectory,settings,level);
                Campaign = new CampaignState(configuration.Campaign.Stages.Count, Settings.ballsPerRound);
                if(hasStarted) BeginAttempt(); else SetPhase(AppPhase.Title);
            }
            catch(Exception exception)
            {
                InitializationError = exception.Message; SetPhase(AppPhase.ConfigError);
                Debug.LogError("Initialization failed: " + exception.Message, this);
            }
            finally { transitioning = false; Changed?.Invoke(); }
        }
        public void RequestPause()
        {
            if(!CanSimulate || (Session?.Phase != SessionPhase.Aiming && Session?.Phase != SessionPhase.Volley && Session?.Phase != SessionPhase.Recalling)) return;
            savedTimeScale = Time.timeScale; ownsPause = true; Time.timeScale = 0;
            SetPhase(AppPhase.Paused); PauseChanged?.Invoke(true);
        }
        public void ContinueGame()
        {
            if(Phase != AppPhase.Paused || !UserInputAllowed || !EnterTransition()) return;
            try { ReleasePause(); SetPhase(AppPhase.Playing); } finally { transitioning = false; }
        }
        void ReleasePause()
        {
            if(!ownsPause) return;
            ownsPause = false; Time.timeScale = savedTimeScale; PauseChanged?.Invoke(false);
        }
        void OnApplicationFocus(bool focused)
        {
            if(!focused) RequestPause();
            HasFocus = focused; InvalidateInput();
            if(input) input.FocusChanged(focused);
            aim.CanAim = CanSimulate && Session?.Phase == SessionPhase.Aiming;
            Changed?.Invoke();
        }
        void LateUpdate()
        {
            DispatchInput();
            if(!CanSimulate || Session == null) return;
            if(Session.Phase == SessionPhase.Volley && Time.time-volleyStarted >= Settings.recallTimeout) RequestRecall();
            switch (Session.Phase)
            {
                case SessionPhase.Volley:
                case SessionPhase.Recalling:
                    if (!Session.TryBeginSettlement()) break;
                    Session.Settle(Board);
                    if(Session.Phase==SessionPhase.Won)Campaign.CompleteStage();
                    else if(Session.Phase==SessionPhase.Lost)Campaign.FailStage();
                    boardView.Sync(Board);
                    InvalidateInput();
                    aim.CanAim = Session.Phase == SessionPhase.Aiming;
                    Changed?.Invoke();
                    break;
            }
        }
    }
}
