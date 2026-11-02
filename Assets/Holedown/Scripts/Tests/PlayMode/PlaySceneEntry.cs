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
    }
}
