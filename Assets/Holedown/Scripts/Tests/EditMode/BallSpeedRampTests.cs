#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    // The per-round speed curve: base → twice the base over the configured seconds, then
    // held, and never above the 2D physics speed cap.
    public sealed class BallSpeedRampTests
    {
        [Test] public void CurveReachesDoubleAtTheConfiguredSeconds()
        {
            Assert.AreEqual(1f, BallActor.SpeedMultiplier(0f, 10f), 0.0001f);
            Assert.AreEqual(1.5f, BallActor.SpeedMultiplier(5f, 10f), 0.0001f);
            Assert.AreEqual(2f, BallActor.SpeedMultiplier(10f, 10f), 0.0001f);
            Assert.AreEqual(2f, BallActor.SpeedMultiplier(25f, 10f), 0.0001f, "the multiplier never passes two");
            Assert.AreEqual(12f, BallActor.SpeedAt(12f, 0f, 10f), 0.0001f);
            Assert.AreEqual(18f, BallActor.SpeedAt(12f, 5f, 10f), 0.0001f);
            Assert.AreEqual(24f, BallActor.SpeedAt(12f, 10f, 10f), 0.0001f);
            Assert.AreEqual(24f, BallActor.SpeedAt(12f, 40f, 10f), 0.0001f, "the speed holds at twice the base");
        }
        [Test] public void ShortRampStillEndsAtDoubleInsteadOfPassingIt()
        {
            Assert.AreEqual(18f, BallActor.SpeedAt(12f, 0.25f, 0.5f), 0.0001f);
            Assert.AreEqual(24f, BallActor.SpeedAt(12f, 0.5f, 0.5f), 0.0001f);
            Assert.AreEqual(24f, BallActor.SpeedAt(12f, 9f, 0.5f), 0.0001f);
        }
        [Test] public void BaseSpeedIsOnlyCappedByThePhysicsLimit()
        {
            Assert.AreEqual(100f, BallActor.PhysicsSpeedLimit, 0.0001f, "the project's 2D translation speed cap");
            Assert.AreEqual(2f, BallActor.MaxSpeedMultiplier);
            // A high but legal base really leaves at that speed; the cap only binds later.
            Assert.AreEqual(60f, BallActor.SafeBaseSpeed(60f), 0.0001f);
            Assert.AreEqual(60f, BallActor.SpeedAt(60f, 0f, 10f), 0.0001f, "first ball leaves at 60");
            Assert.AreEqual(90f, BallActor.SpeedAt(60f, 5f, 10f), 0.0001f, "five seconds later it is 90");
            Assert.AreEqual(100f, BallActor.SpeedAt(60f, 10f, 10f), 0.0001f, "the limit clamps the curve");
            Assert.AreEqual(100f, BallActor.SpeedAt(60f, 6.6667f, 10f), 0.01f,
                "base 60 reaches the 100 cap at about 6.67 seconds");
            // Extreme input is capped at the limit itself, first ball included, without overflow.
            Assert.AreEqual(100f, BallActor.SafeBaseSpeed(500f), 0.0001f);
            Assert.AreEqual(100f, BallActor.SpeedAt(500f, 0f, 10f), 0.0001f);
            Assert.AreEqual(100f, BallActor.SpeedAt(float.MaxValue, 3f, 10f), 0.0001f);
            Assert.AreEqual(100f, BallActor.SpeedAt(500f, -3f, 10f), 0.0001f, "negative elapsed is clamped, so an extreme base is already at the cap");
            Assert.AreEqual(0f, BallActor.SafeBaseSpeed(-5f), 0.0001f, "a negative input collapses to zero; settings validation rejects it first");
        }
        [Test] public void SettingsCarryTheRampDurationAndValidateIt()
        {
            var settings = ScriptableObject.CreateInstance<GameSettings>();
            try
            {
                Assert.AreEqual(10f, settings.ballSpeedRampSeconds, 0.0001f, "ten seconds by default");
                settings.Validate();
                settings.ballSpeedRampSeconds = 0f;
                Assert.Throws<System.ArgumentException>(settings.Validate);
                settings.ballSpeedRampSeconds = float.NaN;
                Assert.Throws<System.ArgumentException>(settings.Validate);
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
#endif
