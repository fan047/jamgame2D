using UnityEngine;
using UnityEngine.UI;

namespace Holedown
{
    // Shared flat, two-cut-corner uGUI surface. Existing Button/Image references
    // remain valid because this is an Image, not a second rendering framework.
    [AddComponentMenu("UI/Mine Cut Corner Image")]
    public sealed class CutCornerImage : Image
    {
        [SerializeField, Min(0)] float cut = 10.44f;
        public float Cut { get => cut; set { cut = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();float c=Mathf.Min(cut,Mathf.Min(r.width,r.height)*.5f);
            var points=new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c)};
            for(int i=0;i<points.Length;i++)mesh.AddVert(points[i],color,Vector2.zero);
            for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(0,i,i+1);
        }
    }
}
