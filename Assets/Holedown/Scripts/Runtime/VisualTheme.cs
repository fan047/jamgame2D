using UnityEngine;

namespace Holedown
{
    [CreateAssetMenu(menuName = "Holedown/Visual Theme")]
    public sealed class VisualTheme : ScriptableObject
    {
        [Tooltip("Enable only for authored color art. The shipped placeholder theme keeps legacy role colors.")]
        public bool originalColors;
        [Tooltip("Keep the complete portrait frame visible; camera and background share this one stage size.")]
        public bool fixedStage;
        public Vector2 stageSize = new Vector2(9.4f, 16.702232f);
        public bool logoContainsTitle = true;
        public Sprite ball, blockSquare, blockWide, blockTall;
        public Sprite rewardSquare, rewardWide, rewardTall;
        public Sprite targetSquare, targetWide, targetTall;
        public Sprite crackLight, crackHeavy, rewardBadge, targetBadge;
        public Sprite background, launcher, titleLogo, fragment, spark;
        [Tooltip("Transparent rock/crystal layer drawn in front of blocks (order 10).")]
        public Sprite foreground;
        [Tooltip("Shared Holedown/IncomingPreview material for the waiting row below the floor.")]
        public Material previewMaterial;
        [Tooltip("Stages 1–3, 4–6 and 7–9. Missing slots fall back to background.")]
        public Sprite backgroundStages1To3, backgroundStages4To6, backgroundStages7To9;
        [Tooltip("Stages 4–6 and 7–9: in-place foreground and the nine authored block sprites.")]
        public StageSet stages4To6, stages7To9;

        [System.Serializable]
        public sealed class StageSet
        {
            [Tooltip("In-place transparent rock/crystal layer of the same group. Empty hides it.")]
            public Sprite foreground;
            public Sprite ordinarySquare, ordinaryWide, ordinaryTall;
            public Sprite rewardSquare, rewardWide, rewardTall;
            public Sprite targetSquare, targetWide, targetTall;
            public Sprite Block(int shape, int role)
            {
                if (role == 0) return shape == 1 ? ordinaryWide : shape == 2 ? ordinaryTall : ordinarySquare;
                if (role == 2) return shape == 1 ? targetWide : shape == 2 ? targetTall : targetSquare;
                return shape == 1 ? rewardWide : shape == 2 ? rewardTall : rewardSquare;
            }
        }

        // A group brings its own cave only when its dedicated background slot is filled; the
        // background slot is the single switch, never a duplicate sprite inside the set.
        Sprite DedicatedBackground(int stageIndex) => stageIndex >= 3 && stageIndex < 6 ? backgroundStages4To6
            : stageIndex >= 6 && stageIndex < 9 ? backgroundStages7To9 : null;
        StageSet SetFor(int stageIndex) => stageIndex >= 3 && stageIndex < 6 ? stages4To6
            : stageIndex >= 6 && stageIndex < 9 ? stages7To9 : null;
        public bool UsesOwnStages(int stageIndex) => DedicatedBackground(stageIndex) != null;

        public Sprite Background(int stageIndex)
        {
            var dedicated = DedicatedBackground(stageIndex);
            if (dedicated) return dedicated;
            // The first cave is whatever the first-three-stage slot says, so a group without
            // its own background falls back to the very same cyan cave, not to a bare default.
            return backgroundStages1To3 ? backgroundStages1To3 : background;
        }
        // Without a dedicated background the whole first cave stays in charge, including its
        // decorations. With one, a missing group foreground hides the layer instead of
        // borrowing another cave's rocks.
        public Sprite Foreground(int stageIndex)
            => DedicatedBackground(stageIndex) ? (SetFor(stageIndex) != null ? SetFor(stageIndex).foreground : null) : foreground;
        public Material flashMaterial;
        public Sprite Block(int width, int height) => width == 2 ? blockWide : height == 2 ? blockTall : blockSquare;
        readonly System.Collections.Generic.HashSet<int> missingRoles = new System.Collections.Generic.HashSet<int>();
        public Sprite Block(int width, int height, bool target, int reward)
        {
            var ordinary = Block(width, height);
            if (!target && reward <= 0) return ordinary;
            int shape = width == 2 ? 1 : height == 2 ? 2 : 0;
            var selected = target
                ? shape == 1 ? targetWide : shape == 2 ? targetTall : targetSquare
                : shape == 1 ? rewardWide : shape == 2 ? rewardTall : rewardSquare;
            if (selected) return selected;
            if (missingRoles.Add(shape + (target ? 3 : 0)))
                Debug.LogWarning($"VisualTheme '{name}': missing {(target ? "target" : "reward")} {width}x{height}; using ordinary sprite.", this);
            return ordinary ? ordinary : blockSquare;
        }
        // Stage-aware lookup: the group's own sprite first, then the first cave's sprite of
        // the same role and shape, so a single missing image never blanks a block. The group
        // only counts when its dedicated background is present.
        public Sprite Block(int stageIndex, int width, int height, bool target, int reward)
        {
            int shape = width == 2 ? 1 : height == 2 ? 2 : 0;
            int role = !target && reward <= 0 ? 0 : target ? 2 : 1;
            var set = UsesOwnStages(stageIndex) ? SetFor(stageIndex) : null;
            var selected = set != null ? set.Block(shape, role) : null;
            if (!selected) selected = FirstGroupBlock(shape, role);
            if (selected) return selected;
            return Block(width, height, target, reward);
        }
        public Sprite FirstGroupBlock(int shape, int role)
        {
            if (role == 0) return shape == 1 ? blockWide : shape == 2 ? blockTall : blockSquare;
            if (role == 2) return shape == 1 ? targetWide : shape == 2 ? targetTall : targetSquare;
            return shape == 1 ? rewardWide : shape == 2 ? rewardTall : rewardSquare;
        }
    }
}
