using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace Holedown.Tests
{
    public sealed class MineUIStyleTests
    {
        [Test] public void PrimaryAndSecondaryUseSharedColorsAndNonBlockingText()
        {
            var go=new GameObject("Style",typeof(RectTransform),typeof(CutCornerImage),typeof(Button));
            var child=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));child.transform.SetParent(go.transform,false);
            try {var b=go.GetComponent<Button>();b.targetGraphic=go.GetComponent<CutCornerImage>();MineUIStyle.Button(b,true);Assert.AreEqual(MineUIStyle.Ivory,b.targetGraphic.color);Assert.AreEqual(MineUIStyle.Ink,child.GetComponent<TMPro.TMP_Text>().color);Assert.IsFalse(child.GetComponent<TMPro.TMP_Text>().raycastTarget);MineUIStyle.Button(b);Assert.AreEqual(MineUIStyle.Secondary,b.targetGraphic.color);Assert.Greater(b.colors.highlightedColor.r,b.colors.normalColor.r);}
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void SoftScrimFadesUpwardWithoutAnyBorder()
        {
            var go=new GameObject("Scrim",typeof(RectTransform),typeof(SoftScrim));
            try {
                var scrim=go.GetComponent<SoftScrim>();scrim.TopAlpha=0f;scrim.BottomAlpha=.74f;scrim.color=new Color(.04f,.05f,.1f,1f);
                var helper=new VertexHelper();
                typeof(SoftScrim).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(scrim,new object[]{helper});
                Assert.AreEqual(4,helper.currentVertCount,"one quad only: a soft scrim cannot draw a frame");
                var bottom=new UIVertex();var top=new UIVertex();
                helper.PopulateUIVertex(ref bottom,0);helper.PopulateUIVertex(ref top,2);
                Assert.AreEqual(.74f,bottom.color.a/255f,.01f,"opaque edge sits at the bottom");
                Assert.AreEqual(0,top.color.a,"the upper edge is fully transparent");
            } finally { Object.DestroyImmediate(go); }
        }
        [Test] public void DesignUnitsFollowStageNotScreen()
        {Assert.AreEqual(940f,540*MineUIStyle.Units,.001f);Assert.AreEqual(12*MineUIStyle.Units,MineUIStyle.PanelCut);Assert.AreEqual(1f,MineUIStyle.Ivory.a);}
    }
}
