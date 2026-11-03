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
        public Sprite crackLight, crackHeavy, rewardBadge, targetBadge;
        public Sprite background, launcher, titleLogo, fragment, spark;
        public Material flashMaterial;
        public Sprite Block(int width, int height) => width == 2 ? blockWide : height == 2 ? blockTall : blockSquare;
    }
}
