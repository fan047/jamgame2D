using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class BackgroundThemeTests
    {
        [Test]
        public void GroupsUseZeroBasedStageBoundariesAndFallBackToTheFirstCave()
        {
            var theme = ScriptableObject.CreateInstance<VisualTheme>();
            var texture = new Texture2D(4, 4);
            var sprites = new Sprite[4];
            try
            {
                for (int i = 0; i < sprites.Length; i++) sprites[i] = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * .5f);
                theme.background = sprites[0];
                theme.backgroundStages1To3 = sprites[1];
                theme.backgroundStages4To6 = sprites[2];
                theme.backgroundStages7To9 = sprites[3];
                for (int i = 0; i < 9; i++) Assert.That(theme.Background(i), Is.SameAs(sprites[1 + i / 3]));
                Assert.That(theme.Background(-1), Is.SameAs(sprites[1]), "out of range uses the first cave");
                Assert.That(theme.Background(9), Is.SameAs(sprites[1]));
                // A group without its own background falls back to the same first cave, not to a bare default.
                theme.backgroundStages4To6 = null;
                Assert.That(theme.Background(3), Is.SameAs(sprites[1]));
                Assert.That(theme.Background(5), Is.SameAs(sprites[1]));
                // When the first-three-stage slot itself is empty the generic background serves.
                theme.backgroundStages1To3 = null;
                Assert.That(theme.Background(0), Is.SameAs(sprites[0]));
                Assert.That(theme.Background(4), Is.SameAs(sprites[0]));
                theme.background = null;
                Assert.That(theme.Background(4), Is.Null);
            }
            finally
            {
                foreach (var sprite in sprites) Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(theme);
            }
        }
        [Test]
        public void ShippedThemeShipsBothLateGroupsWithTheirOwnArt()
        {
            var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTheme>("Assets/Holedown/Data/VisualTheme.asset");
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.background, Is.Not.Null);
            var first = theme.backgroundStages1To3 ? theme.backgroundStages1To3 : theme.background;
            for (int stage = 0; stage < 3; stage++)
            {
                Assert.That(theme.Background(stage), Is.SameAs(first), "stage " + (stage + 1));
                Assert.That(theme.Foreground(stage), Is.SameAs(theme.foreground), "stage " + (stage + 1));
                Assert.That(theme.UsesOwnStages(stage), Is.False);
            }
            foreach (var (start, set, background) in new[]
            {
                (3, theme.stages4To6, theme.backgroundStages4To6),
                (6, theme.stages7To9, theme.backgroundStages7To9)
            })
            {
                Assert.That(background, Is.Not.Null, "group background for stage " + (start + 1));
                Assert.That(set, Is.Not.Null);
                Assert.That(set.foreground, Is.Not.Null);
                foreach (var shape in new[] { "square", "wide", "tall" })
                {
                    Assert.That(set.Block(shape == "wide" ? 1 : shape == "tall" ? 2 : 0, 0), Is.Not.Null, "ordinary " + shape);
                    Assert.That(set.Block(shape == "wide" ? 1 : shape == "tall" ? 2 : 0, 1), Is.Not.Null, "reward " + shape);
                    Assert.That(set.Block(shape == "wide" ? 1 : shape == "tall" ? 2 : 0, 2), Is.Not.Null, "target " + shape);
                }
                for (int stage = start; stage < start + 3; stage++)
                {
                    Assert.That(theme.UsesOwnStages(stage), Is.True, "stage " + (stage + 1));
                    Assert.That(theme.Background(stage), Is.SameAs(background));
                    Assert.That(theme.Foreground(stage), Is.SameAs(set.foreground));
                }
            }
        }
    }
}
