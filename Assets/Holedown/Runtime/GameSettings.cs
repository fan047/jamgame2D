using System;
using UnityEngine;

namespace Holedown
{
    [CreateAssetMenu(menuName = "Holedown/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Min(1)] public int ballsPerRound = 12;
        [Min(0.01f)] public float ballRadius = 0.12f;
        [Min(0.01f)] public float ballSpeed = 12f;
        [Min(0.01f)] public float launchInterval = 0.08f;
        [Range(1f, 89f)] public float maxAimAngle = 75f;
        [Min(0.01f)] public float recallTimeout = 30f;

        public void Validate()
        {
            if (ballsPerRound <= 0 || !Positive(ballRadius) || ballRadius >= 0.5f || !Positive(ballSpeed) || !Positive(launchInterval) || !Positive(recallTimeout) || !Positive(maxAimAngle) || maxAimAngle >= 90)
                throw new ArgumentException("Invalid settings: positive finite values required; radius < 0.5, aim angle < 90.");
        }
        static bool Positive(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
    }
}
