using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Holedown
{
    public enum AppPhase { Loading, Title, Playing, Paused, ConfigError, PlannerSelection }
    // Where the packaged configuration comes from. Automatic picks the bundled StreamingAssets
    // copy on Android and the external directory everywhere else (Editor and Windows).
    public enum ConfigSource { Automatic, ExternalDirectory, BundledStreamingAssets }
    public enum FlowCommand { Start, Continue, Recall, NextStage, RetryStage, RestartRun, Reload, BeginPlannerTest, PlannerSelection }

    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] GameSettings settings;
        [SerializeField] LevelDefinition level;
        [SerializeField] AimInput aim;
        [SerializeField] BoardView boardView;
        [SerializeField] BallActor ballPrefab;
        [SerializeField, Tooltip("Optional JSON folder. Empty: project/Config in Editor; executable/Config in Player.")]
        string externalConfigDirectory;
        [SerializeField, Tooltip("Automatic: bundled StreamingAssets on Android, external directory elsewhere.")]
        ConfigSource configSource = ConfigSource.Automatic;
        public bool UsesBundledConfiguration => configSource == ConfigSource.BundledStreamingAssets
            || (configSource == ConfigSource.Automatic && Application.platform == RuntimePlatform.Android);
        // The packaged build always ships the full campaign; a short list is a broken bundle,
        // never a cue to fall back to a single stage.
        public const int BundledStageCount = 9;
        static void RequireBundledStages(LoadedConfiguration candidate)
        {
            if (candidate.Campaign.Stages.Count != BundledStageCount)
                throw new ArgumentException($"Config/level.json: expected {BundledStageCount} stages; found {candidate.Campaign.Stages.Count}.");
        }
        [SerializeField] bool plannerTestMode;
        PlannerTestContext plannerContext, requestedPlannerContext;
        int plannerEntryBalls;
        public bool IsPlannerTest => plannerTestMode;
        const string DifficultyPreference = "Holedown.Difficulty";
        public GameDifficulty Difficulty { get; private set; }
        public bool OffersEasyRetry => !plannerTestMode && Phase == AppPhase.Playing
            && Session?.Phase == SessionPhase.Lost && Campaign?.OffersEasyRetry == true;
        public PlannerTestContext PlannerContext => plannerContext;
        public string PlannerError { get; private set; }
        public int EntryBalls => plannerContext != null ? plannerEntryBalls : Campaign?.EntryBalls ?? Settings.ballsPerRound;
        public IReadOnlyList<StageDefinition> Stages => configuration?.Campaign.Stages;
        public string StageId => configuration != null && Campaign != null ? configuration.Campaign.Stages[Campaign.StageIndex].Id : "";
        LoadedConfiguration configuration;
        readonly Dictionary<int, BallActor> balls = new Dictionary<int, BallActor>();
        Coroutine launching;
        float volleyStarted;
        public GameSession Session { get; private set; }
        public BoardState Board { get; private set; }
        public string InitializationError { get; private set; }
        public event Action<BlockView, HitResult> BlockHit;
        public event Action VolleyLaunched;
        public event Action ManualRecallAccepted;
        public event Action<SessionPhase> RoundSettled;
        public event Action CommandConfirmed;
        public event Action Changed;
        public event Action AttemptReset;
        public CampaignState Campaign { get; private set; }
        bool transitioning, stageTransitionInputBlocked;
        int transitionFrame = -1;
        public bool IsTransitioning => transitioning || stageTransitionInputBlocked;
        public GameSettings Settings => configuration?.Settings ?? settings;
        public string ConfigurationLabel => configuration?.Label ?? "Not loaded";
        public string ConfigurationDirectory { get; private set; }
        public AppPhase Phase { get; private set; } = AppPhase.Loading;
        public bool HasFocus { get; private set; } = true;
        public bool CanSimulate => isActiveAndEnabled && Phase == AppPhase.Playing && HasFocus && !advancing && !stageTransitionInputBlocked;
        public bool IsAdvancing => advancing;
        bool advancing;
        public int InputGeneration { get; private set; }
        public event Action<bool> PauseChanged;
        bool hasStarted, ownsPause;
        float savedTimeScale;
        PlayerInputController input;
        FlowCommand? pendingCommand;
        Vector2? pendingFire;
        int pendingGeneration;
        public bool UserInputAllowed => isActiveAndEnabled && HasFocus && !stageTransitionInputBlocked && (input == null || input.AcceptsCommands);
        internal void SetStageTransitionInputBlocked(bool blocked, bool rearmInput = true)
        {
            if (stageTransitionInputBlocked == blocked) return;
            stageTransitionInputBlocked = blocked;
            InvalidateInput();
            if (!blocked && rearmInput && input) input.FocusChanged(HasFocus);
            aim.CanAim = CanSimulate && Session?.Phase == SessionPhase.Aiming;
            if (!aim.CanAim) aim.HidePreview();
            Changed?.Invoke();
        }
        void OnDestroy() { ReleasePause(); configuration?.Dispose(); if(boardView)boardView.AdvanceCompleted-=OnAdvanceCompleted; }

        void OnEnable()
        {
            // Unity leaves the mobile frame rate at the platform default (30 fps); this game runs on a
            // 60 Hz target, so ask for it before the first frame and let the platform pace it.
            if (Application.isMobilePlatform) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60; }
            aim.FireRequested += OnFireRequested; input = GetComponent<PlayerInputController>(); if(boardView)boardView.AdvanceCompleted+=OnAdvanceCompleted;
        }
        void OnDisable()
        {
            aim.FireRequested -= OnFireRequested;
            if(boardView)boardView.AdvanceCompleted-=OnAdvanceCompleted;
            advancing=false;
            stageTransitionInputBlocked=false;
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
        void OnAdvanceCompleted()
        {
            advancing = false;
            aim.CanAim = CanSimulate && Session?.Phase == SessionPhase.Aiming;
            Changed?.Invoke();
        }
        void ClearAttempt()
        {
            advancing=false;
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
            Session=new GameSession();
            Board=configuration.CreateBoard(Campaign.StageIndex, plannerTestMode ? GameDifficulty.Normal : Difficulty);
            aim.Configure(Settings);
            Session.Ready();
            SetPhase(AppPhase.Playing);
            // The opening board is shown at its rule positions; only the row waiting below
            // the floor line fades in, so nothing rises across the whole field.
            boardView.ConfigureStage(Campaign.StageIndex);
            boardView.Sync(Board);
            boardView.PresentIncoming(Board, Application.isPlaying);
        }
        void Start()
        {
            Session = new GameSession();
            plannerTestMode |= Array.IndexOf(Environment.GetCommandLineArgs(),"--planner-test")>=0;
            Difficulty = PlayerPrefs.GetInt(DifficultyPreference) == (int)GameDifficulty.Easy
                ? GameDifficulty.Easy : GameDifficulty.Normal;
            if(plannerTestMode) gameObject.AddComponent<PlannerTestView>().Configure(this);
            if(UsesBundledConfiguration)
            {
                // The phone reads the copy that shipped inside the build; there is no external
                // directory to fall back to.
                SetPhase(AppPhase.Loading);
                StartCoroutine(LoadBundledConfiguration());
                return;
            }
            LoadFromDirectory();
            Changed?.Invoke();
        }
        void LoadFromDirectory()
        {
            try
            {
                ConfigurationDirectory = !string.IsNullOrWhiteSpace(externalConfigDirectory)
                    ? System.IO.Path.GetFullPath(externalConfigDirectory)
                    : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),"Config");
                configuration = ExternalConfiguration.Load(ConfigurationDirectory,settings,level);
                Campaign=new CampaignState(configuration.Campaign.Stages.Count,Settings.ballsPerRound);
                if(plannerTestMode) SetPhase(AppPhase.PlannerSelection);
                else if(hasStarted) BeginAttempt(); else SetPhase(AppPhase.Title);
            }
            catch (Exception exception)
            {
                InitializationError = exception.Message; aim.CanAim = false;
                SetPhase(AppPhase.ConfigError);
                Debug.LogError("Initialization failed: " + exception.Message, this);
            }
        }
        IEnumerator LoadBundledConfiguration()
        {
            string settingsText = null, levelText = null, error = null;
            yield return BundledConfiguration.Read(text => settingsText = text, text => levelText = text, message => error = message);
            try
            {
                if (error != null) throw new ArgumentException(error);
                configuration = ExternalConfiguration.Parse(settingsText, levelText, settings, level, "Bundled");
                RequireBundledStages(configuration);
                Campaign = new CampaignState(configuration.Campaign.Stages.Count, Settings.ballsPerRound);
                if(plannerTestMode) SetPhase(AppPhase.PlannerSelection);
                else if(hasStarted) BeginAttempt(); else SetPhase(AppPhase.Title);
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
                case FlowCommand.BeginPlannerTest: BeginPlannerTest(requestedPlannerContext); break;
                case FlowCommand.PlannerSelection: ReturnToPlannerSelection(); break;
            }
            else if(fire.HasValue) TryFire(fire.Value);
        }
        public void StartGame()
        {
            if(Phase != AppPhase.Title || configuration == null || !UserInputAllowed || !EnterTransition()) return;
            try { hasStarted = true; BeginAttempt(); CommandConfirmed?.Invoke(); } finally { transitioning = false; }
        }
        public bool TryFire(Vector2 direction)
        {
            if (!CanSimulate || !UserInputAllowed || transitionFrame == Time.frameCount || transitioning || Campaign == null || Session == null || direction.y >= 0 || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude < 0.0001f) return false;
            float angle = Mathf.Clamp(Mathf.Atan2(direction.x,-direction.y)*Mathf.Rad2Deg,-Settings.maxAimAngle,Settings.maxAimAngle)*Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));
            if (!Session.TryFire(EntryBalls)) return false;
            // The beam disappears on the launch frame, not one frame later.
            aim.CanAim = false; aim.HidePreview(); InvalidateInput(); volleyStarted = Time.time;
            launching = StartCoroutine(LaunchVolley(Session.SessionId,Session.VolleyId,direction));
            Changed?.Invoke(); return true;
        }
        IEnumerator LaunchVolley(Guid session, int volley, Vector2 direction)
        {
            var interval = new WaitForSeconds(Settings.launchInterval);
            bool firstBall = true;
            // The round's ramp starts when the first ball actually leaves the launcher, on
            // shared scaled time, so pause freezes it and later balls continue from here.
            float rampStart = float.NaN;
            while (Session.SessionId == session && Session.VolleyId == volley && Session.Phase == SessionPhase.Volley && Session.Pending > 0)
            {
                while(!CanSimulate) yield return null;
                if (firstBall) rampStart = Time.time;
                int id = Session.RegisterLaunch();
                var ball = Instantiate(ballPrefab, AimInput.Origin, Quaternion.identity);
                balls.Add(id,ball); ball.Hit += OnHit; ball.Returned += OnReturned;
                ball.Launch(Settings,session,volley,id,direction,rampStart);
                if (firstBall)
                {
                    firstBall = false;
                    VolleyLaunched?.Invoke();
                }
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
        public void RequestRecall() => Recall(manual: true);

        void Recall(bool manual)
        {
            if (!CanSimulate || !UserInputAllowed || transitionFrame == Time.frameCount || Session == null || !Session.Recall()) return;
            InvalidateInput();
            if (launching != null) { StopCoroutine(launching); launching = null; }
            var returning = new List<BallActor>(balls.Values);
            foreach (var ball in returning) ball.Return();
            if (manual) ManualRecallAccepted?.Invoke();
            Changed?.Invoke();
        }
        bool EnterTransition()
        {
            if(transitioning || transitionFrame==Time.frameCount)return false;
            transitioning=true;transitionFrame=Time.frameCount;return true;
        }
        public void NextStage()
        {
            if(plannerTestMode)return;
            if(!CanSimulate || !UserInputAllowed || Session?.Phase!=SessionPhase.Won || Campaign==null || Campaign.IsFinalStage || !EnterTransition())return;
            try { if (Campaign.NextStage()) { BeginAttempt(); CommandConfirmed?.Invoke(); } } finally { transitioning = false; }
        }
        public void RetryStage()
        {
            if(plannerTestMode){BeginPlannerTest(plannerContext);return;}
            if(!CanSimulate || !UserInputAllowed || Session?.Phase!=SessionPhase.Lost || Campaign==null || !EnterTransition())return;
            try { if (Campaign.RetryStage()) { BeginAttempt(); CommandConfirmed?.Invoke(); } } finally { transitioning = false; }
        }
        public bool ChangeDifficulty(GameDifficulty difficulty)
        {
            if (difficulty == Difficulty || plannerTestMode || !UserInputAllowed) return false;
            if (Phase == AppPhase.Paused) return RestartCurrentStage(difficulty);
            if (Phase != AppPhase.Title) return false;
            SaveDifficulty(difficulty);
            Changed?.Invoke();
            CommandConfirmed?.Invoke();
            return true;
        }

        public bool RestartCurrentStage() => RestartCurrentStage(Difficulty);

        public bool RetryOnEasy() => OffersEasyRetry && RestartCurrentStage(GameDifficulty.Easy);

        public void DismissEasyRetry()
        {
            if (!OffersEasyRetry || !UserInputAllowed) return;
            Campaign.DismissEasyRetry();
            Changed?.Invoke();
        }

        bool RestartCurrentStage(GameDifficulty difficulty)
        {
            bool allowed = Phase == AppPhase.Paused || (OffersEasyRetry && difficulty == GameDifficulty.Easy);
            if (!allowed || plannerTestMode || !UserInputAllowed || Campaign == null || !EnterTransition()) return false;
            try
            {
                if (!Campaign.RestartStage()) return false;
                if (Difficulty != difficulty) SaveDifficulty(difficulty);
                ReleasePause();
                BeginAttempt();
                CommandConfirmed?.Invoke();
                return true;
            }
            finally { transitioning = false; }
        }

        void SaveDifficulty(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
            PlayerPrefs.SetInt(DifficultyPreference, (int)difficulty);
            PlayerPrefs.Save();
        }

        public void RestartRun()
        {
            if(plannerTestMode){ReturnToPlannerSelection();return;}
            // Allowed during an advance animation: restarting cancels it instead of being ignored.
            if(Phase!=AppPhase.Playing || !UserInputAllowed || Campaign==null || !EnterTransition())return;
            try { Campaign.RestartRun(); BeginAttempt(); CommandConfirmed?.Invoke(); } finally { transitioning = false; }
        }
        public void Restart() => ReloadAndRestart();
        public void ReloadAndRestart()
        {
            if(plannerTestMode){ReloadPlannerTest();return;}
            if(!UserInputAllowed || Phase == AppPhase.Loading || !EnterTransition())return;
            if(UsesBundledConfiguration)
            {
                // ClearAttempt stops every coroutine on this object, so the cleanup happens here
                // and only the read plus commit runs in the coroutine that follows.
                SetPhase(AppPhase.Loading); ClearAttempt(); ReleasePause();
                ConfigurationDirectory = null;
                configuration?.Dispose(); configuration = null; Campaign = null; Board = null;
                Session = new GameSession(); InitializationError = null;
                StartCoroutine(ReloadBundledConfiguration());
                return;
            }
            try
            {
                SetPhase(AppPhase.Loading); ClearAttempt(); ReleasePause();
                configuration?.Dispose(); configuration = null; Campaign = null; Board = null;
                Session = new GameSession(); InitializationError = null;
                configuration = ExternalConfiguration.Load(ConfigurationDirectory,settings,level);
                Campaign = new CampaignState(configuration.Campaign.Stages.Count, Settings.ballsPerRound);
                if(plannerTestMode) SetPhase(AppPhase.PlannerSelection);
                else if(hasStarted) BeginAttempt(); else SetPhase(AppPhase.Title);
                CommandConfirmed?.Invoke();
            }
            catch(Exception exception)
            {
                InitializationError = exception.Message; SetPhase(AppPhase.ConfigError);
                Debug.LogError("Initialization failed: " + exception.Message, this);
            }
            finally { transitioning = false; Changed?.Invoke(); }
        }
        // The packaged files cannot change on the phone, but the command keeps its meaning:
        // re-read the bundle and restart from stage 1 without touching an external folder.
        IEnumerator ReloadBundledConfiguration()
        {
            string settingsText = null, levelText = null, error = null;
            yield return BundledConfiguration.Read(text => settingsText = text, text => levelText = text, message => error = message);
            try
            {
                if (error != null) throw new ArgumentException(error);
                configuration = ExternalConfiguration.Parse(settingsText, levelText, settings, level, "Bundled");
                RequireBundledStages(configuration);
                Campaign = new CampaignState(configuration.Campaign.Stages.Count, Settings.ballsPerRound);
                if(hasStarted) BeginAttempt(); else SetPhase(AppPhase.Title);
                CommandConfirmed?.Invoke();
            }
            catch (Exception exception)
            {
                InitializationError = exception.Message; SetPhase(AppPhase.ConfigError);
                Debug.LogError("Initialization failed: " + exception.Message, this);
            }
            finally { transitioning = false; Changed?.Invoke(); }
        }
        public int PreviewPlannerEntryBalls(string stageId,PlannerBallMode mode,int manualBalls)
            => new PlannerTestContext(stageId,mode,manualBalls).Resolve(configuration.Campaign,Settings.ballsPerRound).balls;
        public void QueuePlannerTest(string stageId,PlannerBallMode mode,int manualBalls)
        {
            if(!plannerTestMode || !UserInputAllowed)return;
            try { requestedPlannerContext=new PlannerTestContext(stageId,mode,manualBalls); QueueCommand(FlowCommand.BeginPlannerTest); }
            catch(Exception e) { PlannerError=e.Message;Changed?.Invoke(); }
        }
        void BeginPlannerTest(PlannerTestContext context)
        {
            if(!plannerTestMode || !UserInputAllowed || !EnterTransition())return;
            try
            {
                if(context==null || configuration==null)throw new ArgumentException("Select a valid stage and load configuration first.");
                var resolved=context.Resolve(configuration.Campaign,Settings.ballsPerRound);
                plannerContext=context;plannerEntryBalls=resolved.balls;PlannerError=null;InitializationError=null;
                Campaign=new CampaignState(configuration.Campaign.Stages.Count,Settings.ballsPerRound,resolved.index);
                ReleasePause();hasStarted=true;BeginAttempt();
                CommandConfirmed?.Invoke();
            }
            catch(Exception e) { PlannerError=e.Message;Changed?.Invoke(); }
            finally { transitioning=false; }
        }
        public void ReturnToPlannerSelection()
        {
            if(!plannerTestMode || !UserInputAllowed || !EnterTransition())return;
            try { ClearAttempt();ReleasePause();plannerContext=null;PlannerError=null;Board=null;Session=new GameSession();SetPhase(AppPhase.PlannerSelection); CommandConfirmed?.Invoke(); }
            finally { transitioning=false; }
        }
        void ReloadPlannerTest()
        {
            if(!UserInputAllowed || !EnterTransition())return;
            LoadedConfiguration candidate=null;
            try
            {
                foreach(string name in new[]{"settings.json","level.json"})
                    if(!System.IO.File.Exists(System.IO.Path.Combine(ConfigurationDirectory,name)))throw new ArgumentException($"Config/{name}: required file is missing; current test preserved.");
                candidate=ExternalConfiguration.Load(ConfigurationDirectory,settings,level);
                var resolved=plannerContext?.Resolve(candidate.Campaign,candidate.Settings.ballsPerRound);
                // Commit only after files, IDs and effective ball count are validated.
                SetPhase(AppPhase.Loading);ClearAttempt();ReleasePause();
                var old=configuration;configuration=candidate;candidate=null;old?.Dispose();
                PlannerError=null;InitializationError=null;
                if(resolved.HasValue)
                {
                    plannerEntryBalls=resolved.Value.balls;
                    Campaign=new CampaignState(configuration.Campaign.Stages.Count,Settings.ballsPerRound,resolved.Value.index);
                    BeginAttempt();
                }
                else { Campaign=new CampaignState(configuration.Campaign.Stages.Count,Settings.ballsPerRound);Board=null;Session=new GameSession();SetPhase(AppPhase.PlannerSelection); }
                CommandConfirmed?.Invoke();
            }
            catch(Exception e) { PlannerError=e.Message;Changed?.Invoke(); }
            finally { candidate?.Dispose();transitioning=false; }
        }
        public void RequestPause() => Pause(manual: true);

        void Pause(bool manual)
        {
            // Advancing blocks simulation but must still allow pause and focus-loss pause.
            if(Phase != AppPhase.Playing || !HasFocus || Session == null) return;
            if(Session.Phase != SessionPhase.Aiming && Session.Phase != SessionPhase.Volley && Session.Phase != SessionPhase.Recalling) return;
            savedTimeScale = Time.timeScale; ownsPause = true; Time.timeScale = 0;
            SetPhase(AppPhase.Paused); PauseChanged?.Invoke(true);
            if (manual) CommandConfirmed?.Invoke();
        }
        public void ContinueGame()
        {
            if(Phase != AppPhase.Paused || !UserInputAllowed || !EnterTransition()) return;
            try { ReleasePause(); SetPhase(AppPhase.Playing); CommandConfirmed?.Invoke(); } finally { transitioning = false; }
        }
        void ReleasePause()
        {
            if(!ownsPause) return;
            ownsPause = false; Time.timeScale = savedTimeScale; PauseChanged?.Invoke(false);
        }
        // Backgrounding freezes the run and drops every input; coming back only re-enables the
        // UI, the player still has to press continue. Focus and pause events arrive in either
        // order, so both restore through the same path and never leave HasFocus false.
        void OnApplicationPause(bool paused)
        {
            if(paused) LeaveForeground(); else RestoreForeground();
        }
        void OnApplicationFocus(bool focused)
        {
            if(focused) RestoreForeground(); else LeaveForeground();
        }
        void LeaveForeground()
        {
            Pause(manual: false);
            HasFocus = false;
            InvalidateInput();
            if(input) input.FocusChanged(false);
            aim.CanAim = false;
            Changed?.Invoke();
        }
        void RestoreForeground()
        {
            HasFocus = true;
            InvalidateInput();
            if(input) input.FocusChanged(true);
            aim.CanAim = CanSimulate && Session?.Phase == SessionPhase.Aiming;
            Changed?.Invoke();
        }
        void LateUpdate()
        {
            DispatchInput();
            if(!CanSimulate || Session == null) return;
            if(Session.Phase == SessionPhase.Volley && Time.time-volleyStarted >= Settings.recallTimeout) Recall(manual: false);
            switch (Session.Phase)
            {
                case SessionPhase.Volley:
                case SessionPhase.Recalling:
                    if (!Session.TryBeginSettlement()) break;
                    // Capture where blocks were so the advance can rise from those positions.
                    boardView.CapturePositions();
                    Session.Settle(Board);
                    if(Session.Phase==SessionPhase.Won){Campaign.CompleteStage();boardView.Sync(Board);boardView.HideIncoming();}
                    else if(Session.Phase==SessionPhase.Lost)
                    {
                        Campaign.FailStage(!plannerTestMode && Difficulty == GameDifficulty.Normal);
                        boardView.Sync(Board);
                        boardView.HideIncoming();
                    }
                    // PlayAdvance also covers the zero-duration path: it settles at once and
                    // still refreshes the waiting row for the next advance.
                    else { advancing = true; boardView.PlayAdvance(Board); }
                    InvalidateInput();
                    aim.CanAim = !advancing && Session.Phase == SessionPhase.Aiming;
                    Changed?.Invoke();
                    RoundSettled?.Invoke(Session.Phase);
                    break;
            }
        }
    }
}
