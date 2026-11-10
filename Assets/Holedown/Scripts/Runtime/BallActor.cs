using System;
using UnityEngine;

namespace Holedown
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class BallActor : MonoBehaviour
    {
        [SerializeField] VisualTheme theme;
        [SerializeField, Tooltip("Fading trail behind the ball; length is two ball diameters.")]
        [Min(0f)] float trailDiameters = 2f;
        // Unity's own 2D speed cap; the ramp clamps the base to it and never asks for more.
        public const float DefaultPhysicsSpeedLimit = 100f;
        public const float MaxSpeedMultiplier = 2f;
        public static float PhysicsSpeedLimit => Physics2D.maxTranslationSpeed > 0f
            ? Physics2D.maxTranslationSpeed : DefaultPhysicsSpeedLimit;
        Rigidbody2D body;
        CircleCollider2D circle;
        Transform visual;
        float speed;
        float baseSpeed;
        float rampSeconds;
        float rampStartTime = float.NaN;
        float appliedSpeed;
        float trailDiameter;
        bool ramping;
        bool returned = true;
        public Guid SessionId { get; private set; }
        public int VolleyId { get; private set; }
        public int BallId { get; private set; }
        public event Action<BallActor, int> Hit;
        public event Action<BallActor> Returned;
        public Vector2 Velocity => body.linearVelocity;

        TrailRenderer trail;
        SpriteRenderer ballVisual;
        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); circle = GetComponent<CircleCollider2D>();
            ballVisual = GetComponentInChildren<SpriteRenderer>(); visual = ballVisual.transform;
        }
        // The speed curve of one round: the base grows linearly to twice the base, then holds.
        // The base itself is only limited by the physics cap, so a base of 60 really leaves at
        // 60 and reaches the cap later instead of being silently lowered up front.
        public static float SafeBaseSpeed(float requested) => Mathf.Clamp(requested, 0f, PhysicsSpeedLimit);
        public static float SpeedMultiplier(float elapsed, float rampSeconds)
            => rampSeconds <= 0f ? MaxSpeedMultiplier : 1f + Mathf.Clamp01(elapsed / rampSeconds);
        public static float SpeedAt(float baseSpeedValue, float elapsed, float rampSeconds)
            => Mathf.Min(PhysicsSpeedLimit, SafeBaseSpeed(baseSpeedValue) * SpeedMultiplier(elapsed, rampSeconds));
        public float CurrentSpeed => appliedSpeed;
        // Test seam: the fixed-route suites were approved at one constant speed, so they pin
        // the ramp off instead of re-approving every angle for a moving speed.
        public static bool IgnoreRampForTests { get; set; }
        // rampStartTime is the shared scaled time of this round's first launch. Without it the
        // ball keeps the plain base speed, which is what the rule and physics suites expect.
        public void Launch(GameSettings settings, Guid session, int volley, int ball, Vector2 direction, float rampStartTime = float.NaN)
        {
            SessionId = session; VolleyId = volley; BallId = ball;
            baseSpeed = SafeBaseSpeed(settings.ballSpeed);
            rampSeconds = Mathf.Max(0.01f, settings.ballSpeedRampSeconds);
            this.rampStartTime = rampStartTime;
            ramping = !IgnoreRampForTests && !float.IsNaN(rampStartTime);
            speed = ramping ? SpeedAt(settings.ballSpeed, Time.time - rampStartTime, rampSeconds) : baseSpeed;
            appliedSpeed = speed;
            circle.radius = settings.ballRadius;
            var renderer = visual.GetComponent<SpriteRenderer>();
            if(theme && theme.ball) { renderer.sprite = theme.ball; renderer.color = Color.white; }
            SpriteSizing.Fit(renderer, Vector2.one * (settings.ballRadius * 2f));
            body.gravityScale = 0; body.linearDamping = 0; body.angularDamping = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.position = transform.position;
            returned = false;
            body.linearVelocity = direction.normalized * speed;
            ConfigureTrail(settings);
        }
        void ConfigureTrail(GameSettings settings)
        {
            if (trailDiameters <= 0f || speed <= 0f) return;
            if (!trail)
            {
                var shader = Shader.Find("Sprites/Default");
                if (!shader) return;
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.material = new Material(shader);
                trail.autodestruct = false; trail.emitting = false;
            }
            trailDiameter = settings.ballRadius * 2f;
            float diameter = trailDiameter;
            // Trail time is derived from speed so the visible length stays about trailDiameters diameters.
            trail.time = Mathf.Max(0.01f, diameter * trailDiameters / speed);
            trail.widthMultiplier = diameter;
            trail.numCapVertices = 2;
            var sprite = ballVisual ? ballVisual.sprite : null;
            if (sprite && sprite.texture) trail.material.mainTexture = sprite.texture;
            var fade = new Gradient();
            fade.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(Color.white,1f)},
                        new[]{new GradientAlphaKey(0.55f,0f),new GradientAlphaKey(0f,1f)});
            trail.colorGradient = fade; trail.emitting = true; trail.Clear();
        }
        void FixedUpdate()
        {
            if (returned) return;
            var position = body.position;
            var velocity = body.linearVelocity;
            if ((position.y >= 9.6f && velocity.y > 0) || position.y < -1 || position.x < -1 || position.x > 9 || position.y > 11 || velocity.sqrMagnitude < 0.0001f)
            { Return(); return; }
            // One round shares one ramp: every ball alive follows the same multiplier, and a
            // later ball simply starts at the value the round has already reached.
            speed = ramping ? SpeedAt(baseSpeed, Time.time - rampStartTime, rampSeconds) : baseSpeed;
            appliedSpeed = speed;
            body.linearVelocity = velocity.normalized * speed;
            UpdateTrail();
        }
        void UpdateTrail()
        {
            if (!trail || trailDiameters <= 0f || speed <= 0f) return;
            float seconds = Mathf.Max(0.01f, trailDiameter * trailDiameters / speed);
            // Keep the visible length at about trailDiameters diameters as the speed changes.
            if (Mathf.Abs(trail.time - seconds) > 0.005f) trail.time = seconds;
        }
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!returned && collision.collider.TryGetComponent<BlockView>(out var block)) Hit?.Invoke(this, block.BlockId);
        }
        public void Return()
        {
            if (returned) return;
            returned = true;
            circle.enabled = false; body.simulated = false; if(trail)trail.emitting = false;
            Returned?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
