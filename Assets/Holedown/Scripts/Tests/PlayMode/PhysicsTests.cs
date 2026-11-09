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
        static IEnumerator LoadPhysicsOnlyScene()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
            yield return null;
            // The integrated scene now auto-initializes gameplay. Isolate fixture-owned blocks.
            UnityEngine.Object.FindFirstObjectByType<GameFlowController>().enabled = false;
            UnityEngine.Object.Destroy(UnityEngine.Object.FindFirstObjectByType<BoardView>().gameObject);
            yield return null;
            var view = new GameObject("PhysicsTestBoard").AddComponent<BoardView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("blockPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BlockView>("Assets/Holedown/Prefabs/Block.prefab");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [UnityTest] public IEnumerator BallsBounceHitAndReturnAtBothSpeeds()
        {
            yield return LoadPhysicsOnlyScene();
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
        [UnityTest] public IEnumerator RadiusSettingUpdatesVisualAndCollider()
        {
            yield return LoadPhysicsOnlyScene();
            var settings = ScriptableObject.CreateInstance<GameSettings>(); settings.ballRadius = 0.2f;
            var ball = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab"),AimInput.Origin,Quaternion.identity);
            ball.Launch(settings,Guid.NewGuid(),1,1,Vector2.down);
            Assert.AreEqual(0.2f,ball.GetComponent<CircleCollider2D>().radius,0.001f);
            // Authored sprites have native dimensions/PPU; verify world diameter, not placeholder local scale.
            var renderer=ball.GetComponentInChildren<SpriteRenderer>();
            Assert.AreEqual(0.4f,renderer.bounds.size.x,0.001f);
            Assert.AreEqual(0.4f,renderer.bounds.size.y,0.001f);
            Assert.AreEqual(Vector3.one,ball.transform.localScale);
            ball.Return(); UnityEngine.Object.Destroy(settings); yield return null;
        }
        [UnityTest] public IEnumerator PreviewMatchesBallCenterAtFirstContact()
        {
            yield return LoadPhysicsOnlyScene();
            var aim=UnityEngine.Object.FindFirstObjectByType<AimInput>(); Physics2D.SyncTransforms();
            Assert.AreEqual(0.12f,aim.PreviewEnd(Vector2.down).y,0.015f);
            var rb=AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab").GetComponent<Rigidbody2D>();
            Assert.AreEqual(8,rb.gameObject.layer); Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(8,8));
            yield return null;
        }
        // The floor line alone must reverse the ball: the play field starts empty, so the
        // reported bounce cannot come from a block hit.
        [UnityTest] public IEnumerator BallsBounceOffTheFloorLineWithoutTouchingABlock()
        {
            yield return LoadPhysicsOnlyScene();
            var board = new BoardState(System.Array.Empty<BlockDefinition>(), new[] { new RowDefinition { blocks = new System.Collections.Generic.List<BlockDefinition> { new BlockDefinition(0, 0, 1, 1, true) } } });
            var view = UnityEngine.Object.FindFirstObjectByType<BoardView>(); view.Sync(board);
            Assert.AreEqual(0, view.Views.Count, "the field is empty, so nothing can be hit on the way down");
            var prefab = AssetDatabase.LoadAssetAtPath<BallActor>("Assets/Holedown/Prefabs/Ball.prefab");
            var floor = GameObject.Find("Game/Bottom");
            Assert.IsNotNull(floor, "the authored floor collider must stay in the scene");
            Assert.AreEqual(new Vector3(4, -0.25f, 0), floor.transform.position);
            Assert.AreEqual(new Vector2(9, 0.5f), floor.GetComponent<BoxCollider2D>().size);
            foreach (float speed in new[] { 12f, 24f })
            foreach (var direction in new[] { Vector2.down, new Vector2(0.35f, -1).normalized, new Vector2(-0.35f, -1).normalized })
            {
                var settings = ScriptableObject.CreateInstance<GameSettings>(); settings.ballSpeed = speed;
                var ball = UnityEngine.Object.Instantiate(prefab, AimInput.Origin, Quaternion.identity);
                int hits = 0; bool returned = false, descending = false, ascending = false;
                float lowest = float.MaxValue;
                ball.Hit += (b, id) => hits++;
                ball.Returned += b => returned = true;
                ball.Launch(settings, Guid.NewGuid(), 1, 1, direction);
                float deadline = Time.time + 12;
                while (!returned && Time.time < deadline)
                {
                    if (ball)
                    {
                        var position = ball.transform.position; var velocity = ball.Velocity;
                        lowest = Mathf.Min(lowest, position.y);
                        Assert.Greater(position.y, -0.01f, "the ball never enters the waiting area below the floor line");
                        if (velocity.y < 0) descending = true;
                        else if (velocity.y > 0 && descending) ascending = true;
                    }
                    yield return new WaitForFixedUpdate();
                }
                Assert.IsTrue(returned, $"ball speed {speed} did not return");
                Assert.IsTrue(descending && ascending, "the floor itself reversed the vertical speed");
                // A fixed step cannot sample the exact contact depth, so bound it: the lowest
                // sampled centre stays above the floor line and within one step of one radius.
                Assert.Greater(lowest, 0f, "the ball centre never crosses the floor line");
                Assert.Less(lowest, settings.ballRadius + speed * Time.fixedDeltaTime + 0.01f, "the ball turns around at the floor line, not at a block");
                Assert.AreEqual(0, hits, "no block was touched, so this is a floor bounce only");
                UnityEngine.Object.Destroy(settings);
            }
        }
#endif
    }
}
