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
        public void RoleSelectionCoversNineSlotsAndFallsBackOncePerMissingSlot()
        {
            var theme = ScriptableObject.CreateInstance<VisualTheme>();
            var texture = new Texture2D(3,3);
            var sprites = new Sprite[9];
            for (int i=0;i<9;i++) sprites[i]=Sprite.Create(texture,new Rect(i%3,i/3,1,1),Vector2.one*.5f);
            try
            {
                theme.blockSquare=sprites[0]; theme.blockWide=sprites[1]; theme.blockTall=sprites[2];
                theme.rewardSquare=sprites[3]; theme.rewardWide=sprites[4]; theme.rewardTall=sprites[5];
                theme.targetSquare=sprites[6]; theme.targetWide=sprites[7]; theme.targetTall=sprites[8];
                for(int role=0;role<3;role++) for(int shape=0;shape<3;shape++)
                    Assert.AreSame(sprites[role*3+shape],theme.Block(shape==1?2:1,shape==2?2:1,role==2,role==1?3:0));
                Assert.AreSame(sprites[8],theme.Block(1,2,true,5),"Target identity takes precedence");
                theme.targetTall=null;
                LogAssert.Expect(LogType.Warning,"VisualTheme '': missing target 1x2; using ordinary sprite.");
                Assert.AreSame(sprites[2],theme.Block(1,2,true,0));
                Assert.AreSame(sprites[2],theme.Block(1,2,true,0));
                theme.rewardWide=null;
                LogAssert.Expect(LogType.Warning,"VisualTheme '': missing reward 2x1; using ordinary sprite.");
                Assert.AreSame(sprites[1],theme.Block(2,1,false,5));
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.DestroyImmediate(theme); foreach(var sprite in sprites) Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
        [Test]
        public void ShippedThemeBindsAllRolesWithoutChangingColliders()
        {
            var theme=AssetDatabase.LoadAssetAtPath<VisualTheme>("Assets/Holedown/Data/VisualTheme.asset");
            var prefab=AssetDatabase.LoadAssetAtPath<BlockView>("Assets/Holedown/Prefabs/Block.prefab");
            // The supplied key art already carries the wordmark, so the drawn title text stays hidden.
            Assert.IsTrue(theme.originalColors); Assert.IsTrue(theme.logoContainsTitle);
            foreach(var sprite in new[]{theme.ball,theme.background,theme.launcher,theme.crackLight,theme.crackHeavy,theme.rewardBadge,theme.targetBadge,theme.titleLogo,theme.fragment,theme.spark}) Assert.IsNotNull(sprite);
            for(int role=0;role<3;role++) for(int shape=0;shape<3;shape++)
            {
                int width=shape==1?2:1,height=shape==2?2:1;
                var definition=new BlockDefinition(0,0,width,12,role==2,role==1?5:0,height);
                // Rendering fixture deliberately covers target-tall too; production rules prohibit tall incoming rows.
                var state=(BlockState)System.Activator.CreateInstance(typeof(BlockState),
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{1,definition,0,definition.health},null);
                var view=Object.Instantiate(prefab);
                try
                {
                    view.Bind(state);
                    Assert.AreSame(theme.Block(width,height,role==2,role==1?5:0),view.Visual.sprite);
                    Assert.AreEqual(Color.white,view.Visual.color);
                    Assert.AreEqual(new Vector2(width-.1f,height-.1f),view.GetComponent<BoxCollider2D>().size);
                    var badge=view.transform.Find("RoleBadge").GetComponent<SpriteRenderer>();
                    Assert.AreEqual(role>0,badge.enabled);
                    if(role>0) Assert.AreSame(role==2?theme.targetBadge:theme.rewardBadge,badge.sprite);
                    view.SetHealth(8,role==2); Assert.AreSame(theme.crackLight,view.Cracks.sprite);
                    view.SetHealth(4,role==2); Assert.AreSame(theme.crackHeavy,view.Cracks.sprite);
                    view.Bind(state); Assert.IsFalse(view.Cracks.enabled);
                }
                finally { Object.DestroyImmediate(view.gameObject); }
            }
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Holedown/Art/Theme"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(SpriteImportMode.Single,importer.spriteImportMode,path);
                Assert.AreEqual(FilterMode.Point,importer.filterMode,path);Assert.IsFalse(importer.mipmapEnabled,path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression,path);
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.AreEqual(new Rect(0,0,sprite.texture.width,sprite.texture.height),sprite.rect,path);
            }
        }
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
