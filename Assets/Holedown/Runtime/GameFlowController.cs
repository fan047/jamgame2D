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
        [SerializeField, Tooltip("Optional JSON folder. Empty: Inspector assets in Editor; executable/Config in Player.")]
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
        public GameSettings Settings => configuration?.Settings ?? settings;
        public string ConfigurationLabel => configuration?.Label ?? "Not loaded";
        public string ConfigurationDirectory { get; private set; }
        void OnDestroy() { configuration?.Dispose(); }

        void OnEnable() { aim.FireRequested += OnFireRequested; }
        void OnDisable()
        {
            aim.FireRequested -= OnFireRequested;
            aim.CanAim = false;
            StopAllCoroutines(); launching = null;
            foreach (var ball in balls.Values) if (ball) { ball.Hit -= OnHit; ball.Returned -= OnReturned; Destroy(ball.gameObject); }
            balls.Clear();
        }
        void Start()
        {
            Session = new GameSession();
            try
            {
                ConfigurationDirectory = !string.IsNullOrWhiteSpace(externalConfigDirectory)
                    ? System.IO.Path.GetFullPath(externalConfigDirectory)
                    : Application.isEditor ? null : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),"Config");
                configuration = ExternalConfiguration.Load(ConfigurationDirectory,settings,level);
                Board = configuration.Board;
                aim.Configure(Settings);
                boardView.Sync(Board); Session.Ready(); aim.CanAim = true;
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
            if (Session == null || direction.y >= 0 || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude < 0.0001f) return false;
            float angle = Mathf.Clamp(Mathf.Atan2(direction.x,-direction.y)*Mathf.Rad2Deg,-Settings.maxAimAngle,Settings.maxAimAngle)*Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));
            if (!Session.TryFire(Settings.ballsPerRound)) return false;
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
            var view = boardView.ApplyHit(blockId,hit);
            BlockHit?.Invoke(view,hit);
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
        public void Restart()
        {
            aim.CanAim = false;
            StopAllCoroutines(); launching = null;
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
                    Session.Settle(Board); boardView.Sync(Board);
                    aim.CanAim = Session.Phase == SessionPhase.Aiming;
                    Changed?.Invoke();
                    break;
            }
        }
    }
}
