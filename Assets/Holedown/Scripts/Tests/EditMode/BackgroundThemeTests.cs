using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class BackgroundThemeTests
    {
        [Test]
        public void GroupsUseZeroBasedStageBoundariesAndFallback()
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
                Assert.That(theme.Background(-1), Is.SameAs(sprites[0]));
                Assert.That(theme.Background(9), Is.SameAs(sprites[0]));
                theme.backgroundStages4To6 = null;
                Assert.That(theme.Background(3), Is.SameAs(sprites[0]));
                Assert.That(theme.Background(5), Is.SameAs(sprites[0]));
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
        public void ShippedThemeKeepsOneBackgroundUntilGroupArtArrives()
        {
            // Art for stages 4-6 and 7-9 is not delivered yet: the shipped theme must not switch at 3→4 or 6→7.
            var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTheme>("Assets/Holedown/Data/VisualTheme.asset");
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.background, Is.Not.Null);
            Assert.That(theme.backgroundStages4To6, Is.Null);
            Assert.That(theme.backgroundStages7To9, Is.Null);
            for (int stage = 0; stage < 9; stage++) Assert.That(theme.Background(stage), Is.SameAs(theme.background), "stage " + (stage + 1));
        }
    }
}
