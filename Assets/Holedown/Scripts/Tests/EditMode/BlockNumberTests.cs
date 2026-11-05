#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Holedown.Tests
{
    public sealed class BlockNumberTests
    {
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(1, 2)]
        public void Int32HealthAndRewardStayOnTwoLinesInsideBlock(int width, int height)
        {
            var board = new BoardState(new[]{new BlockDefinition(0,0,width,int.MaxValue,false,int.MaxValue,height)},new[]{new RowDefinition { blocks = new System.Collections.Generic.List<BlockDefinition> { new BlockDefinition(7,0,1,1,true) } }});
            var prefab = AssetDatabase.LoadAssetAtPath<BlockView>("Assets/Holedown/Prefabs/Block.prefab");
            var view = Object.Instantiate(prefab);
            try
            {
                view.Bind(board.Blocks.Values.First(b=>!b.IsTarget));
                var text = view.GetComponentInChildren<TMP_Text>();text.ForceMeshUpdate();
                Assert.That(text.text, Does.Contain("2147483647\n<size=65%>+2147483647"));
                Assert.That(text.textInfo.lineCount, Is.EqualTo(2));
                Assert.That(text.textBounds.size.x, Is.LessThanOrEqualTo(text.rectTransform.rect.width + .02f));
                Assert.That(text.textBounds.size.y, Is.LessThanOrEqualTo(text.rectTransform.rect.height + .02f));
            }
            finally {Object.DestroyImmediate(view.gameObject);}
        }
    }
}
#endif
