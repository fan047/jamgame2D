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
        [Test] public void DesignUnitsFollowStageNotScreen()
        {Assert.AreEqual(940f,540*MineUIStyle.Units,.001f);Assert.AreEqual(12*MineUIStyle.Units,MineUIStyle.PanelCut);Assert.AreEqual(1f,MineUIStyle.Ivory.a);}
    }
}
