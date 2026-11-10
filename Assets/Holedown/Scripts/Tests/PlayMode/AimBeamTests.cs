#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    // The aiming beam in the real scene: it follows AimInput's geometry, uses the design width,
    // travels its highlight, and disappears whenever aiming is not allowed.
    public sealed class AimBeamTests
    {
        GameFlowController flow;
        AimInput aim;
        AimBeam beam;
        [SetUp] public void Setup() { flow = null; aim = null; beam = null; }
        [TearDown] public void Cleanup() { if (flow) flow.enabled = false; Time.timeScale = 1f; }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity"); yield return null;
            flow = Object.FindFirstObjectByType<GameFlowController>();
            aim = Object.FindFirstObjectByType<AimInput>();
            beam = aim.Beam;
            flow.SendMessage("OnApplicationFocus", true); yield return null;
            yield return PlaySceneEntry.StartIfReady();
            Assert.IsNotNull(beam, "the scene ships the stepped beam");
        }
        static Vector2 ScreenPoint(Vector2 world) => Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
        [UnityTest] public IEnumerator BeamFollowsThePredictionAndUsesTheDesignWidth()
        {
            yield return Load();
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
            Assert.IsTrue(aim.PreviewVisible, "aiming shows the beam");
            Assert.IsTrue(beam.IsVisible);
            var straightDown = aim.PreviewEnd(Vector2.down);
            Assert.AreEqual(straightDown.y, beam.End.y, 0.05f);
            Assert.AreEqual(aim.PreviewEnd(Vector2.down).y, beam.End.y, 0.01f, "the beam ends exactly on the prediction");
            Assert.Greater(beam.End.y, 0f, "the beam never reaches into the waiting area");
            // The scene ships a 13 px design width; the plan allows 12–14 px at 540×960.
            Assert.That(beam.Width * 540f / 9.4f, Is.InRange(12f, 14f), "beam width inside the planned band");
            var renderer = beam.GetComponent<MeshRenderer>();
            Assert.AreEqual(2, renderer.sortingOrder, "above blocks and cracks");
            Assert.Greater(beam.transform.position.z, -1f);
            // A diagonal aim turns the whole beam instead of only its end.
            aim.SamplePointer(ScreenPoint(new Vector2(1f, 2f)), false);
            Assert.That(Mathf.Abs(straightDown.y - beam.End.y), Is.GreaterThan(0.05f), "a diagonal aim changes the endpoint");
            Assert.AreEqual(aim.PreviewEnd(AimInput.TryDirection(new Vector2(1f, 2f), 80f, out var diagonal) ? diagonal : Vector2.down).x,
                beam.End.x, 0.05f, "the endpoint follows the same prediction");
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
        }
        [UnityTest] public IEnumerator BeamHidesOnUiMarginFireAndPause()
        {
            yield return Load();
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
            Assert.IsTrue(beam.IsVisible);
            // A point on the decorative margin outside the board cannot aim.
            aim.SamplePointer(ScreenPoint(new Vector2(-0.6f, 5f)), false);
            Assert.IsFalse(beam.IsVisible, "the decorative margin hides the beam");
            // A point on the HUD is UI, not the board.
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 9f)), false);
            Assert.IsFalse(beam.IsVisible, "hovering the UI hides the beam");
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
            Assert.IsTrue(beam.IsVisible);
            Assert.IsTrue(flow.TryFire(Vector2.down));
            Assert.IsFalse(beam.IsVisible, "firing hides the beam");
            flow.RequestRecall(); yield return null; yield return null;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!flow.CanSimulate && Time.realtimeSinceStartup < deadline)
            {
                if (flow.Phase != AppPhase.Playing) { yield return PlaySceneEntry.ResumeAfterFocusLoss(flow); continue; }
                yield return null;
            }
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
            Assert.IsTrue(beam.IsVisible);
            flow.RequestPause(); yield return null;
            Assert.AreEqual(AppPhase.Paused, flow.Phase);
            Assert.IsFalse(beam.IsVisible, "a pause hides the beam");
            flow.ContinueGame(); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator HighlightTravelsAndLoops()
        {
            yield return Load();
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
            float first = beam.Sweep;
            yield return new WaitForSeconds(0.2f);
            Assert.AreNotEqual(first, beam.Sweep, "the highlight keeps moving while aiming");
            yield return new WaitForSeconds(1.2f);
            Assert.That(beam.Sweep, Is.InRange(0f, 1f), "the highlight loops instead of running away");
            aim.SamplePointer(ScreenPoint(new Vector2(4f, 5f)), false);
        }
    }
}
#endif
