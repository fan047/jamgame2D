#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class ThemePresentationTests
    {
        [Test]
        public void NativeSpriteSizesFitWithoutChangingPhysics()
        {
            var texture = new Texture2D(128,64);
            var sprite = Sprite.Create(texture,new Rect(0,0,128,64),Vector2.one*0.5f,64);
            var go = new GameObject("Sizing test"); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            try
            {
                SpriteSizing.Fit(renderer,new Vector2(0.9f,1.9f));
                Assert.AreEqual(new Vector3(0.45f,1.9f,1),go.transform.localScale);
                SpriteSizing.Cover(renderer,new Vector2(8,12));
                Assert.AreEqual(Vector3.one*12,go.transform.localScale);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
        [UnityTest]
        public IEnumerator ColoredThemeUsesIndependentCracksAndPerRendererFlash()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<BlockView>("Assets/Holedown/Prefabs/Block.prefab");
            var view = Object.Instantiate(prefab); var other = Object.Instantiate(prefab);
            var theme = Object.Instantiate(AssetDatabase.LoadAssetAtPath<VisualTheme>("Assets/Holedown/Data/VisualTheme.asset"));
            var texture = new Texture2D(128,64); var sprite = Sprite.Create(texture,new Rect(0,0,128,64),Vector2.one*0.5f,64);
            theme.originalColors = true; theme.blockTall = sprite;
            try
            {
                var board = new BoardState(new[]{new BlockDefinition(3,2,1,6,false,0,2)},new[]{new RowDefinition{blocks=new System.Collections.Generic.List<BlockDefinition>{new BlockDefinition(0,0,1,1,true)}}});
                view.ConfigureTheme(theme); view.Bind(board.Blocks[1]); other.Bind(board.Blocks[1]);
                Assert.AreEqual(Color.white,view.BaseColor);
                Assert.AreEqual(new Vector3(0.45f,1.9f,1),view.Visual.transform.localScale);
                view.SetHealth(2,false);
                Assert.AreEqual(view.Visual.transform.parent,view.Cracks.transform.parent);
                Assert.AreEqual(new Vector3(0.9f,1.9f,1),view.Cracks.transform.localScale);
                var size = view.GetComponent<BoxCollider2D>().size;
                view.PlayImpact();
                var block = new MaterialPropertyBlock();view.Visual.GetPropertyBlock(block);Assert.AreEqual(1,block.GetFloat("_FlashAmount"));
                other.Visual.GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_FlashAmount"));
                Assert.AreEqual(0,theme.flashMaterial.GetFloat("_FlashAmount"));
                yield return new WaitForSeconds(0.2f);
                view.Visual.GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_FlashAmount"));
                Assert.AreEqual(Vector3.one,view.ImpactRoot.localScale); Assert.AreEqual(size,view.GetComponent<BoxCollider2D>().size);
                view.PlayImpact(); view.gameObject.SetActive(false);
                view.Visual.GetPropertyBlock(block);Assert.AreEqual(0,block.GetFloat("_FlashAmount"));
            }
            finally { Object.DestroyImmediate(view.gameObject); Object.DestroyImmediate(other.gameObject); Object.DestroyImmediate(theme); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
        [Test]
        public void FlashShaderCompilesForCurrentEditor()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Holedown/Art/SpriteFlash.shader");
            Assert.IsNotNull(shader); Assert.IsTrue(shader.isSupported);
            foreach(var message in ShaderUtil.GetShaderMessages(shader)) Assert.AreNotEqual(UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,message.severity,message.message);
        }
    }
}
#endif
