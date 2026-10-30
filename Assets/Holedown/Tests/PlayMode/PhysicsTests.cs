using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Holedown.Tests
{
    public sealed class PhysicsTests
    {
        [TestCase(4, 9)] [TestCase(4, 10)] [TestCase(-1, 4)] [TestCase(9, 4)]
        public void InvalidAimRejected(float x, float y) => Assert.IsFalse(AimInput.TryDirection(new Vector2(x, y), 75, out _));
        [Test] public void AimClampedDownward()
        {
            Assert.IsTrue(AimInput.TryDirection(new Vector2(8,8.99f),75,out var d));
            Assert.AreEqual(75,Vector2.Angle(Vector2.down,d),0.01f);
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator BallsBounceHitAndReturnAtBothSpeeds()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            var board = new BoardState(new[] { new BlockDefinition(3, 0, 2, 100) }, new[] { new RowDefinition { blocks = new System.Collections.Generic.List<BlockDefinition> { new BlockDefinition(0, 0, 1, 1, true) } } });
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>(); view.Sync(board);
            var prefab = AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab");
            foreach (float speed in new[] { 12f,24f })
            foreach (var direction in new[] { Vector2.down,new Vector2(0.5f,-1).normalized,new Vector2(-3.5f,-1).normalized })
            {
                var settings = ScriptableObject.CreateInstance<GameSettings>(); settings.ballSpeed=speed;
                var ball = UnityEngine.Object.Instantiate(prefab,AimInput.Origin,Quaternion.identity);
                bool returned=false; int hits=0; bool bounced=false;
                ball.Hit += (b,id) => { var result=board.ApplyHit(id); Assert.IsTrue(result.Accepted); view.ApplyHit(id,result); hits++; };
                ball.Returned += b => returned=true;
                ball.Launch(settings,Guid.NewGuid(),1,1,direction);
                float deadline=Time.time+12;
                while (!returned && Time.time<deadline)
                {
                    if(ball && ball.Velocity.y>0) bounced=true;
                    if(ball) {Assert.That(ball.transform.position.x,Is.InRange(-0.01f,8.01f));Assert.Greater(ball.transform.position.y,-0.01f);}
                    yield return new WaitForFixedUpdate();
                }
                Assert.IsTrue(returned,"Ball failed to naturally return"); Assert.IsTrue(bounced);
                if(direction==Vector2.down) Assert.Greater(hits,0);
                UnityEngine.Object.Destroy(settings);
            }
        }
        [UnityTest] public IEnumerator PreviewMatchesBallCenterAtFirstContact()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            var aim=UnityEngine.Object.FindFirstObjectByType<AimInput>(); Physics2D.SyncTransforms();
            Assert.AreEqual(0.12f,aim.PreviewEnd(Vector2.down).y,0.015f);
            var rb=AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab").GetComponent<Rigidbody2D>();
            Assert.AreEqual(8,rb.gameObject.layer); Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(8,8));
            yield return null;
        }
#endif
    }
}
