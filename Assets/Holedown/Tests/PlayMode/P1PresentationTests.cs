#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Holedown.Tests
{
    public sealed class P1PresentationTests
    {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        [TearDown] public void Cleanup(){foreach(var obj in owned)if(obj)UnityEngine.Object.DestroyImmediate(obj);owned.Clear();}
        BlockView View(BlockState block)
        {
            var view=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<BlockView>("Assets/Holedown/Prefabs/Block.prefab"));owned.Add(view.gameObject);view.Bind(block);return view;
        }
        static BoardState Board(int health=6)=>new BoardState(new[]{new BlockDefinition(3,2,1,health,false,0,2)},new[]{new RowDefinition{blocks=new List<BlockDefinition>{new BlockDefinition(0,0,1,1,true)}}});
        [UnityTest] public IEnumerator CrackThresholdsSurviveBindingAndAdvance()
        {
            var board=Board();var view=View(board.Blocks[1]);var cracks=view.transform.Find("Visual/Cracks").GetComponent<SpriteRenderer>();
            Assert.AreEqual(0,view.DamageLevel);Assert.IsFalse(cracks.enabled);
            foreach(int expected in new[]{0,1,1,2,2}) {
                var hit=board.ApplyHit(1);view.SetHealth(hit.Health,false);Assert.AreEqual(expected,view.DamageLevel);
                Assert.AreEqual(expected>0,cracks.enabled);if(expected>0){Assert.IsNotNull(cracks.sprite);Assert.AreEqual(Vector3.one,new Vector3(cracks.sprite.bounds.size.x,cracks.sprite.bounds.size.y,1));}
            }
            var sprite=cracks.sprite;board.Advance();view.Bind(board.Blocks[1]);
            Assert.AreEqual(2,view.DamageLevel);Assert.AreSame(sprite,cracks.sprite);Assert.AreEqual(6,board.Blocks[1].MaxHealth);
            view.Bind(Board().Blocks[1]);Assert.AreEqual(0,view.DamageLevel);Assert.IsFalse(cracks.enabled);
            yield return null;
        }
        [UnityTest] public IEnumerator RepeatedImpactRestartsFromBaselineWithoutMovingColliderOrText()
        {
            var view=View(Board().Blocks[1]);var visual=view.Visual.transform;var label=view.GetComponentInChildren<TMPro.TMP_Text>().transform;
            var baseline=visual.localScale;var labelScale=label.localScale;var collider=view.GetComponent<BoxCollider2D>();var size=collider.size;var position=view.transform.position;
            for(int i=0;i<5;i++) {
                view.PlayImpact();Assert.AreEqual(baseline,visual.localScale);
                yield return new WaitForSeconds(0.035f);
                Assert.That(visual.localScale.x/baseline.x,Is.InRange(0.939f,1f));
                Assert.AreEqual(size,collider.size);Assert.AreEqual(position,view.transform.position);Assert.AreEqual(labelScale,label.localScale);
            }
            yield return new WaitForSeconds(0.2f);Assert.AreEqual(baseline,visual.localScale);Assert.AreEqual(view.BaseColor,view.Visual.color);
            view.PlayImpact();yield return null;view.gameObject.SetActive(false);Assert.AreEqual(baseline,visual.localScale);
        }
        [UnityTest] public IEnumerator VerticalUpperHalfCollidesAtBothSpeedsAndPreviewUsesFullHeight()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");yield return null;
            UnityEngine.Object.FindFirstObjectByType<GameFlowController>().enabled=false;
            UnityEngine.Object.Destroy(UnityEngine.Object.FindFirstObjectByType<BoardView>().gameObject);yield return null;
            var board=Board(100);var view=View(board.Blocks[1]);Physics2D.SyncTransforms();
            Assert.AreEqual(new Vector3(3.5f,3,0),view.transform.position);Assert.AreEqual(new Vector2(0.9f,1.9f),view.GetComponent<BoxCollider2D>().size);
            var aim=UnityEngine.Object.FindFirstObjectByType<AimInput>();var end=aim.PreviewEnd(new Vector2(-0.5f,-5f).normalized);
            Assert.AreEqual(4.07f,end.y,0.03f);
            foreach(float speed in new[]{12f,24f}) {
                var settings=ScriptableObject.CreateInstance<GameSettings>();settings.ballSpeed=speed;owned.Add(settings);
                var ball=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab"),new Vector3(1,3.5f,0),Quaternion.identity);owned.Add(ball.gameObject);
                bool hit=false;ball.Hit+=(b,id)=>{hit=true;Assert.AreEqual(1,id);Assert.AreEqual(3.5f,b.transform.position.y,0.05f);var damage=board.ApplyHit(id);view.SetHealth(damage.Health,false);b.Return();};
                ball.Launch(settings,Guid.NewGuid(),1,1,Vector2.right);float deadline=Time.time+1;
                while(!hit&&Time.time<deadline)yield return new WaitForFixedUpdate();
                Assert.IsTrue(hit,$"Upper half did not collide at speed {speed}");
            }
        }
    }
}
#endif
