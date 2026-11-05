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
        [Tooltip("Stages 1–3, 4–6 and 7–9. Missing slots fall back to background.")]
        public Sprite backgroundStages1To3, backgroundStages4To6, backgroundStages7To9;
        public Sprite Background(int stageIndex)
        {
            var selected = stageIndex >= 0 && stageIndex < 3 ? backgroundStages1To3
                : stageIndex >= 3 && stageIndex < 6 ? backgroundStages4To6
                : stageIndex >= 6 && stageIndex < 9 ? backgroundStages7To9 : null;
            return selected ? selected : background;
        }
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
    }
}
