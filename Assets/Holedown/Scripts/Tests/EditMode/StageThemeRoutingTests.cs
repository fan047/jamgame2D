#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace Holedown.Tests
{
    // Group routing rules for the two late-stage caves: the dedicated background slot is the
    // single switch, a missing foreground hides the layer, and a single missing block only
    // falls back for that one role and shape.
    public sealed class StageThemeRoutingTests
    {
        VisualTheme theme;
        Sprite first, secondBg, secondFg, secondSquare, secondWide, secondTall, secondRewardWide;

        [SetUp] public void Setup()
        {
            theme = ScriptableObject.CreateInstance<VisualTheme>();
            var texture = new Texture2D(4, 4);
            first = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            secondBg = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            secondFg = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            secondSquare = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            secondWide = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            secondTall = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            secondRewardWide = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            theme.background = first;
            theme.foreground = first;
            theme.blockSquare = first; theme.blockWide = first; theme.blockTall = first;
            theme.stages4To6 = new VisualTheme.StageSet
            {
                foreground = secondFg,
                ordinarySquare = secondSquare, ordinaryWide = secondWide, ordinaryTall = secondTall,
                rewardWide = secondRewardWide
            };
            theme.backgroundStages4To6 = secondBg;
        }
        [TearDown] public void Cleanup()
        {
            foreach (var sprite in new[] { first, secondBg, secondFg, secondSquare, secondWide, secondTall, secondRewardWide })
                if (sprite) { var texture = sprite.texture; Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
            Object.DestroyImmediate(theme);
        }
        [Test] public void DedicatedBackgroundSwitchesTheWholeGroup()
        {
            Assert.IsFalse(theme.UsesOwnStages(0));
            Assert.IsTrue(theme.UsesOwnStages(3));
            Assert.AreSame(secondBg, theme.Background(3));
            Assert.AreSame(secondFg, theme.Foreground(3));
            Assert.AreSame(secondSquare, theme.Block(3, 1, 1, false, 0));
            Assert.AreSame(secondWide, theme.Block(3, 2, 1, false, 0));
            Assert.AreSame(secondTall, theme.Block(3, 1, 2, false, 0));
            Assert.AreSame(secondRewardWide, theme.Block(3, 2, 1, false, 3));
        }
        [Test] public void MissingDedicatedBackgroundFallsBackToTheWholeFirstCave()
        {
            theme.backgroundStages4To6 = null;
            Assert.IsFalse(theme.UsesOwnStages(3));
            Assert.AreSame(first, theme.Background(3));
            Assert.AreSame(first, theme.Foreground(3), "the first cave keeps its own decorations");
            Assert.AreSame(first, theme.Block(3, 1, 1, false, 0));
            Assert.AreSame(first, theme.Block(3, 2, 1, false, 3));
        }
        [Test] public void MissingGroupForegroundHidesTheLayerInsteadOfBorrowingRocks()
        {
            theme.stages4To6.foreground = null;
            Assert.AreSame(secondBg, theme.Background(3));
            Assert.IsNull(theme.Foreground(3));
        }
        [Test] public void OneMissingBlockOnlyFallsBackForThatRoleAndShape()
        {
            theme.stages4To6.ordinarySquare = null;
            theme.stages4To6.rewardWide = null;
            Assert.AreSame(secondBg, theme.Background(3), "the group itself stays selected");
            Assert.AreSame(first, theme.Block(3, 1, 1, false, 0), "only the square falls back");
            Assert.AreSame(secondWide, theme.Block(3, 2, 1, false, 0), "the wide block stays in the group");
            Assert.AreSame(secondTall, theme.Block(3, 1, 2, false, 0));
            Assert.AreSame(first, theme.Block(3, 2, 1, false, 3), "only the reward wide falls back");
        }
        [Test] public void GroupWithoutASetStillUsesItsBackgroundAndHidesDecorations()
        {
            theme.stages4To6 = null;
            Assert.IsTrue(theme.UsesOwnStages(3));
            Assert.AreSame(secondBg, theme.Background(3));
            Assert.IsNull(theme.Foreground(3));
            Assert.AreSame(first, theme.Block(3, 1, 1, false, 0));
        }
        [Test] public void StageFourAndStageSevenAreTheGroupBoundaries()
        {
            var bg7 = Sprite.Create(secondBg.texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            theme.backgroundStages7To9 = bg7;
            try
            {
                for (int index = 0; index < 9; index++)
                {
                    var expected = index < 3 ? theme.background : index < 6 ? secondBg : bg7;
                    Assert.AreSame(expected, theme.Background(index), "background for stage " + (index + 1));
                    Assert.AreEqual(index >= 3, theme.UsesOwnStages(index), "group selection for stage " + (index + 1));
                }
            }
            finally { theme.backgroundStages7To9 = null; Object.DestroyImmediate(bg7); }
        }
        [Test] public void FirstThreeStagesKeepTheShippedCave()
        {
            Assert.AreSame(first, theme.Background(2));
            Assert.AreSame(first, theme.Foreground(2));
            Assert.AreSame(theme.blockSquare, theme.Block(2, 1, 1, false, 0));
            Assert.AreSame(first, theme.Block(2, 1, 1, false, 0));
        }
    }
}
#endif
