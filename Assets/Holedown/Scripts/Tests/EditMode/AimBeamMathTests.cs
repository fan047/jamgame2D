#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    // The beam presents AimInput's geometry: width comes from the 540-wide design stage and the
    // tip narrows in whole steps so the end reads as pixels instead of a smooth taper.
    public sealed class AimBeamMathTests
    {
        static AimBeam New(float designPixels = 9f, int screen = 540, float stage = 9.4f)
        {
            var go = new GameObject("BeamTest", typeof(MeshFilter), typeof(MeshRenderer));
            var beam = go.AddComponent<AimBeam>();
            var so = new UnityEditor.SerializedObject(beam);
            so.FindProperty("designWidthPixels").floatValue = designPixels;
            so.FindProperty("designScreenWidth").intValue = screen;
            so.FindProperty("designStageWidth").floatValue = stage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return beam;
        }
        [Test] public void WidthScalesFromTheDesignStage()
        {
            var beam = New();
            try
            {
                Assert.AreEqual(9f * 9.4f / 540f, beam.Width, 0.0001f, "nine pixels at 540 wide");
                var wide = New(designPixels: 18f, screen: 720);
                try { Assert.AreEqual(18f * 9.4f / 720f, wide.Width, 0.0001f, "the same relative width at another resolution"); }
                finally { Object.DestroyImmediate(wide.gameObject); }
            }
            finally { Object.DestroyImmediate(beam.gameObject); }
        }
        [Test] public void TipNarrowsInStepsAndEndsAtZero()
        {
            var beam = New();
            try
            {
                float full = beam.Width;
                Assert.AreEqual(full, beam.WidthAt(0f), 0.0001f);
                Assert.AreEqual(full, beam.WidthAt(0.5f), 0.0001f);
                Assert.AreEqual(0f, beam.WidthAt(1f), 0.0001f, "the tip closes completely");
                float previous = full + 1f;
                int distinct = 0;
                for (float t = 0.7f; t <= 1f; t += 0.01f)
                {
                    float w = beam.WidthAt(t);
                    Assert.LessOrEqual(w, previous + 0.0001f, "the tip never widens again");
                    if (!Mathf.Approximately(w, previous)) distinct++;
                    previous = w;
                }
                Assert.Greater(distinct, 1, "the tip is stepped rather than a single cut");
                Assert.AreEqual(2, beam.SortingOrder, "above blocks and cracks, below labels, badges, balls and rocks");
            }
            finally { Object.DestroyImmediate(beam.gameObject); }
        }
    }
}
#endif
