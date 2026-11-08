using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    // Borderless soft backing: a single quad whose alpha fades from an opaque bottom
    // to a transparent top, so the cave floor stays visible and the UI blends in.
    // Never used as a hit target; buttons keep their own invisible graphic.
    [AddComponentMenu("UI/Mine Soft Scrim")]
    public sealed class SoftScrim : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] float topAlpha;
        [SerializeField, Range(0f, 1f)] float bottomAlpha = .78f;
        public float TopAlpha { get => topAlpha; set { topAlpha = value; SetVerticesDirty(); } }
        public float BottomAlpha { get => bottomAlpha; set { bottomAlpha = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();
            Color bottom=color, top=color;bottom.a*=bottomAlpha;top.a*=topAlpha;
            mesh.AddVert(new Vector2(r.xMin,r.yMin),bottom,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax,r.yMin),bottom,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax,r.yMax),top,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin,r.yMax),top,Vector2.zero);
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
        }
    }
}
