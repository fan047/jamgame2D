using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    internal static class PlaySceneEntry
    {
        // Explicitly enters through the production StartGame command; never disables title/input gates.
        public static IEnumerator StartIfReady()
        {
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            flow.SendMessage("OnApplicationFocus", true);
            yield return null; yield return null;
            if(flow.Phase == AppPhase.Title)
            {
                flow.StartGame();
                Assert.AreEqual(AppPhase.Playing, flow.Phase);
                yield return null;
            }
        }
        // This workstation can deliver an OS focus loss to the Editor during long PlayMode
        // runs; the game then pauses by design (covered as a product rule by the UI suite).
        // Physical rule tests resume through the same Continue command a player uses and
        // keep asserting the pause really was a focus loss, never a logic-driven stop.
        public static IEnumerator ResumeAfterFocusLoss(GameFlowController flow)
        {
            Assert.AreEqual(AppPhase.Paused, flow.Phase); Assert.IsFalse(flow.HasFocus, "pause was not caused by focus loss");
            flow.SendMessage("OnApplicationFocus", true);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!flow.UserInputAllowed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(flow.UserInputAllowed, "explicit continue must be available after focus returns");
            flow.ContinueGame(); Assert.AreEqual(AppPhase.Playing, flow.Phase);
        }
    }
}
