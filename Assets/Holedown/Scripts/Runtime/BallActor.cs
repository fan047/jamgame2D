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
        Rigidbody2D body;
        CircleCollider2D circle;
        Transform visual;
        float speed;
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
        public void Launch(GameSettings settings, Guid session, int volley, int ball, Vector2 direction)
        {
            SessionId = session; VolleyId = volley; BallId = ball;
            speed = settings.ballSpeed;
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
            float diameter = settings.ballRadius * 2f;
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
            body.linearVelocity = velocity.normalized * speed;
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
