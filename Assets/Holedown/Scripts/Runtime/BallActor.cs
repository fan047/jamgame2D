using System;
using UnityEngine;

namespace Holedown
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class BallActor : MonoBehaviour
    {
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

        void Awake()
        {
            body = GetComponent<Rigidbody2D>(); circle = GetComponent<CircleCollider2D>();
            visual = GetComponentInChildren<SpriteRenderer>().transform;
        }
        public void Launch(GameSettings settings, Guid session, int volley, int ball, Vector2 direction)
        {
            SessionId = session; VolleyId = volley; BallId = ball;
            speed = settings.ballSpeed;
            circle.radius = settings.ballRadius;
            visual.localScale = Vector3.one * (settings.ballRadius * 2f);
            body.gravityScale = 0; body.linearDamping = 0; body.angularDamping = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.position = transform.position;
            returned = false;
            body.linearVelocity = direction.normalized * speed;
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
            circle.enabled = false; body.simulated = false;
            Returned?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
